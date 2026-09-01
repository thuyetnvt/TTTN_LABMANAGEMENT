using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Dtos;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private const long MaxAvatarRequestBytes = MaxAvatarBytes + 64 * 1024;
    private static readonly IReadOnlySet<string> AvatarExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly IReadOnlyDictionary<string, string> AvatarContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        AppDbContext context,
        IAuditService auditService,
        IFileStorage fileStorage,
        ILogger<UsersController> logger)
    {
        _context = context;
        _auditService = auditService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public sealed class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? UniversityCode { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool HasAvatar { get; set; }
        public DateTime? AvatarUpdatedAt { get; set; }
    }

    public sealed class AvatarStateDto
    {
        public bool HasAvatar { get; set; }
        public DateTime? AvatarUpdatedAt { get; set; }
    }

    public sealed class CreateUserDto
    {
        [Required, MinLength(3), MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(255)] public string FullName { get; set; } = string.Empty;
        [MaxLength(100)] public string? UniversityCode { get; set; }
        [MaxLength(30)] public string Phone { get; set; } = string.Empty;
        [MaxLength(255)] public string Department { get; set; } = string.Empty;
        [MaxLength(100)] public string? ClassName { get; set; }

        [Required, MinLength(8), MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public sealed class UpdateUserDto
    {
        [Required, MinLength(3), MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(255)] public string FullName { get; set; } = string.Empty;
        [MaxLength(100)] public string? UniversityCode { get; set; }
        [MaxLength(30)] public string Phone { get; set; } = string.Empty;
        [MaxLength(255)] public string Department { get; set; } = string.Empty;
        [MaxLength(100)] public string? ClassName { get; set; }

        [MaxLength(200)]
        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public sealed class ChangePasswordDto
    {
        [Required, MaxLength(200)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8), MaxLength(200)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class UpdateProfileDto
    {
        [EmailAddress, MaxLength(256)] public string? Email { get; set; }
        [MaxLength(255)] public string FullName { get; set; } = string.Empty;
        [MaxLength(100)] public string? UniversityCode { get; set; }
        [MaxLength(30)] public string Phone { get; set; } = string.Empty;
        [MaxLength(255)] public string Department { get; set; } = string.Empty;
        [MaxLength(100)] public string? ClassName { get; set; }
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                UniversityCode = user.UniversityCode,
                Phone = user.Phone,
                Department = user.Department,
                ClassName = user.ClassName,
                Role = user.Role,
                IsActive = user.IsActive,
                HasAvatar = user.AvatarStorageKey != null,
                AvatarUpdatedAt = user.AvatarUpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("paged")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsersPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(user =>
                user.Username.Contains(search)
                || user.FullName.Contains(search)
                || (user.Email != null && user.Email.Contains(search))
                || (user.UniversityCode != null && user.UniversityCode.Contains(search))
                || user.Department.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            if (string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(user => user.IsActive);
            }
            else if (string.Equals(status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(user => !user.IsActive);
            }
            else
            {
                query = query.Where(user => user.Role == status);
            }
        }

        return await query
            .OrderBy(user => user.Username)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                UniversityCode = user.UniversityCode,
                Phone = user.Phone,
                Department = user.Department,
                ClassName = user.ClassName,
                Role = user.Role,
                IsActive = user.IsActive,
                HasAvatar = user.AvatarStorageKey != null,
                AvatarUpdatedAt = user.AvatarUpdatedAt
            })
            .ToPagedResultAsync(paging, cancellationToken);
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<IEnumerable<object>>> GetTeachers(
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive && user.Role == Roles.Teacher)
            .OrderBy(user => user.Username)
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.UniversityCode
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetOwnProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _context.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                UniversityCode = user.UniversityCode,
                Phone = user.Phone,
                Department = user.Department,
                ClassName = user.ClassName,
                Role = user.Role,
                IsActive = user.IsActive,
                HasAvatar = user.AvatarStorageKey != null,
                AvatarUpdatedAt = user.AvatarUpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    [HttpPut("me/profile")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> UpdateOwnProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive) return Unauthorized();

        var email = NormalizeEmail(dto.Email);
        var universityCode = dto.UniversityCode?.Trim();
        if (!string.IsNullOrWhiteSpace(email)
            && await _context.Users.AnyAsync(item => item.Id != userId && item.Email == email, cancellationToken))
            return Conflict(new { message = "Email đã được sử dụng." });
        if (!string.IsNullOrWhiteSpace(universityCode)
            && await _context.Users.AnyAsync(item => item.Id != userId && item.UniversityCode == universityCode, cancellationToken))
            return Conflict(new { message = "Mã sinh viên/mã cán bộ đã tồn tại." });

        user.Email = email;
        user.FullName = dto.FullName.Trim();
        user.UniversityCode = universityCode;
        user.Phone = dto.Phone.Trim();
        user.Department = dto.Department.Trim();
        user.ClassName = dto.ClassName?.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UpdateProfile", nameof(User), userId, cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAvatarRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarRequestBytes)]
    [EnableRateLimiting("sensitive")]
    public async Task<ActionResult<AvatarStateDto>> UploadOwnAvatar(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive) return Unauthorized();
        if (file is null) return BadRequest(new { message = "Vui lòng chọn ảnh đại diện." });

        try
        {
            ValidateAvatar(file);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        var previousKey = user.AvatarStorageKey;
        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                file,
                $"avatars/users/{user.Id}",
                AvatarExtensions,
                MaxAvatarBytes,
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        user.AvatarStorageKey = _fileStorage.GetStorageKey(stored.StoredPath);
        user.AvatarUpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteStorageFileAsync(user.AvatarStorageKey);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousKey)
            && !string.Equals(previousKey, user.AvatarStorageKey, StringComparison.Ordinal))
        {
            await TryDeleteStorageFileAsync(previousKey);
        }

        await _auditService.WriteAsync(
            HttpContext,
            "UpdateAvatar",
            nameof(User),
            user.Id,
            cancellationToken: cancellationToken);

        return Ok(new AvatarStateDto
        {
            HasAvatar = true,
            AvatarUpdatedAt = user.AvatarUpdatedAt
        });
    }

    [HttpDelete("me/avatar")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> DeleteOwnAvatar(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive) return Unauthorized();

        var previousKey = user.AvatarStorageKey;
        user.AvatarStorageKey = null;
        user.AvatarUpdatedAt = null;
        await _context.SaveChangesAsync(cancellationToken);
        await TryDeleteStorageFileAsync(previousKey);
        await _auditService.WriteAsync(
            HttpContext,
            "DeleteAvatar",
            nameof(User),
            user.Id,
            cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpGet("me/avatar")]
    public async Task<IActionResult> DownloadOwnAvatar(CancellationToken cancellationToken)
        => await DownloadAvatar(GetCurrentUserId(), cancellationToken);

    [HttpGet("{id:int}/avatar")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DownloadUserAvatar(int id, CancellationToken cancellationToken)
        => await DownloadAvatar(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> CreateUser(
        CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var username = dto.Username.Trim();
        var email = NormalizeEmail(dto.Email);
        dto.FullName = dto.FullName.Trim(); dto.UniversityCode = dto.UniversityCode?.Trim();
        dto.Phone = dto.Phone.Trim(); dto.Department = dto.Department.Trim(); dto.ClassName = dto.ClassName?.Trim();
        dto.Password = dto.Password.Trim();
        if (!Roles.All.Contains(dto.Role))
        {
            return BadRequest("Vai trò không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 8)
        {
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 8 ký tự." });
        }

        if (await _context.Users.AnyAsync(
                user => user.Username == username,
                cancellationToken))
        {
            return BadRequest("Tên đăng nhập đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await _context.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return BadRequest("Email đã được sử dụng.");
        }
        if (!string.IsNullOrWhiteSpace(dto.UniversityCode)
            && await _context.Users.AnyAsync(user => user.UniversityCode == dto.UniversityCode, cancellationToken))
            return Conflict(new { message = "Mã sinh viên/mã cán bộ đã tồn tại." });

        var user = new User
        {
            Username = username,
            Email = email,
            FullName = dto.FullName,
            UniversityCode = dto.UniversityCode,
            Phone = dto.Phone,
            Department = dto.Department,
            ClassName = dto.ClassName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            "User",
            user.Id,
            new { user.Username, user.Email, user.Role },
            cancellationToken);

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            UniversityCode = user.UniversityCode,
            Phone = user.Phone,
            Department = user.Department,
            ClassName = user.ClassName,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateUser(
        int id,
        UpdateUserDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return NotFound();
        }

        var username = dto.Username.Trim();
        var email = NormalizeEmail(dto.Email);
        dto.FullName = dto.FullName.Trim(); dto.UniversityCode = dto.UniversityCode?.Trim();
        dto.Phone = dto.Phone.Trim(); dto.Department = dto.Department.Trim(); dto.ClassName = dto.ClassName?.Trim();
        if (user.Username == "admin")
        {
            username = "admin";
            dto.Role = Roles.Admin;
        }

        if (!Roles.All.Contains(dto.Role))
        {
            return BadRequest("Vai trò không hợp lệ.");
        }

        if (await _context.Users.AnyAsync(
                item => item.Id != id && item.Username == username,
                cancellationToken))
        {
            return BadRequest("Tên đăng nhập đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && await _context.Users.AnyAsync(
                item => item.Id != id && item.Email == email,
                cancellationToken))
        {
            return BadRequest("Email đã được sử dụng.");
        }
        if (!string.IsNullOrWhiteSpace(dto.UniversityCode)
            && await _context.Users.AnyAsync(item => item.Id != id && item.UniversityCode == dto.UniversityCode, cancellationToken))
            return Conflict(new { message = "Mã sinh viên/mã cán bộ đã tồn tại." });

        user.Username = username;
        user.Email = email;
        user.FullName = dto.FullName;
        user.UniversityCode = dto.UniversityCode;
        user.Phone = dto.Phone;
        user.Department = dto.Department;
        user.ClassName = dto.ClassName;
        user.Role = dto.Role;
        user.TokenVersion += 1;
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Update",
            "User",
            user.Id,
            new { user.Username, user.Email, user.Role },
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteUser(
        int id,
        CancellationToken cancellationToken)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (currentUserId == id)
        {
            return BadRequest("Không thể tự khóa chính mình.");
        }

        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return NotFound();
        }

        if (user.Username == "admin")
        {
            return BadRequest("Tài khoản admin hệ thống không được khóa.");
        }

        user.IsActive = false;
        user.TokenVersion += 1;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Deactivate",
            "User",
            user.Id,
            new { user.Username },
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:int}/activate")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ActivateUser(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        if (user.IsActive) return NoContent();
        user.IsActive = true;
        user.TokenVersion += 1;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Activate", "User", id,
            new { user.Username }, cancellationToken);
        return NoContent();
    }

    [HttpPut("me/password")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> ChangeOwnPassword(
        [FromBody] ChangePasswordDto dto,
        CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(
            new object[] { userId },
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });
        }

        if (VerifyPassword(dto.NewPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.TokenVersion += 1;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "ChangePassword",
            "User",
            user.Id,
            cancellationToken: cancellationToken);

        return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
    }

    private async Task<IActionResult> DownloadAvatar(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(item => item.Id == userId && item.IsActive)
            .Select(item => new { item.AvatarStorageKey })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null) return NotFound();
        if (string.IsNullOrWhiteSpace(user.AvatarStorageKey)
            || !_fileStorage.IsSafePath(user.AvatarStorageKey))
            return NotFound();

        var stream = await _fileStorage.OpenReadAsync(user.AvatarStorageKey, cancellationToken);
        if (stream is null) return NotFound();

        var extension = Path.GetExtension(user.AvatarStorageKey);
        return File(stream, AvatarContentTypes.GetValueOrDefault(extension, "application/octet-stream"));
    }

    private static void ValidateAvatar(IFormFile file)
    {
        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > MaxAvatarBytes)
            throw new InvalidDataException("Ảnh đại diện phải có dung lượng tối đa 2 MB.");
        if (!AvatarExtensions.Contains(extension))
            throw new InvalidDataException("Chỉ hỗ trợ ảnh JPG, PNG hoặc WebP.");
        if (!AvatarContentTypes.TryGetValue(extension, out var expectedMime)
            || !string.Equals(file.ContentType, expectedMime, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("MIME type của ảnh không hợp lệ.");
    }

    private async Task TryDeleteStorageFileAsync(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return;
        try
        {
            await _fileStorage.DeleteAsync(storageKey);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Không thể xóa file avatar cũ {StorageKey}.", storageKey);
        }
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static string? NormalizeEmail(string? value)
    {
        var email = value?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    private int GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token không có định danh người dùng.");
}
