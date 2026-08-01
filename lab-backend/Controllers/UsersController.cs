using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public UsersController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public sealed class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public sealed class CreateUserDto
    {
        [Required, MinLength(3), MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

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
        public string Email { get; set; } = string.Empty;

        [MinLength(8), MaxLength(200)]
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

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.Username)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            })
            .ToListAsync(cancellationToken);
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
                user.Username
            })
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> CreateUser(
        CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim();
        if (!Roles.All.Contains(dto.Role))
        {
            return BadRequest("Vai trò không hợp lệ.");
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

        var user = new User
        {
            Username = username,
            Email = email,
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
            Role = user.Role
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
        var email = dto.Email.Trim();
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

        user.Username = username;
        user.Email = email;
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

    [HttpPut("me/password")]
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
}
