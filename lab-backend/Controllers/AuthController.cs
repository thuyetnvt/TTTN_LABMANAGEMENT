using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LabManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public AuthController(
        AppDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _auditService = auditService;
    }

    public sealed class LoginRequest
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class ForgotPasswordRequest
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8), MaxLength(200)]
        public string NewPassword { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim();
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Username == normalizedUsername,
                cancellationToken);

        if (user == null
            || !user.IsActive
            || !VerifyPassword(request.Password, user.PasswordHash))
        {
            await _auditService.WriteAsync(
                HttpContext,
                "LoginFailed",
                "User",
                details: new { Username = normalizedUsername },
                cancellationToken: cancellationToken);
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "LoginSucceeded",
            "User",
            user.Id,
            cancellationToken: cancellationToken);

        return Ok(CreateLoginResponse(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        await _context.PasswordResetTokens
            .Where(item => item.ExpiresAt < DateTime.UtcNow.AddDays(-7))
            .ExecuteDeleteAsync(cancellationToken);

        var user = await _context.Users
            .SingleOrDefaultAsync(
                item => item.Email == normalizedEmail && item.IsActive,
                cancellationToken);

        // Luôn trả cùng một kết quả để tránh dò tìm tài khoản theo email.
        if (user == null)
        {
            return Ok(new
            {
                message = "Nếu email tồn tại, hệ thống sẽ gửi hướng dẫn đặt lại mật khẩu."
            });
        }

        await _context.PasswordResetTokens
            .Where(item => item.UserId == user.Id && item.UsedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    item => item.UsedAt,
                    (DateTime?)DateTime.UtcNow),
                cancellationToken);

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var token = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException("Thiếu cấu hình App__FrontendBaseUrl.");
        }

        var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var safeUsername = WebUtility.HtmlEncode(user.Username);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "[LabManagement] Đặt lại mật khẩu",
                $"""
                 <p>Chào {safeUsername},</p>
                 <p>Bạn vừa yêu cầu đặt lại mật khẩu LabManagement.</p>
                 <p><a href="{safeUrl}">Đặt lại mật khẩu</a></p>
                 <p>Liên kết có hiệu lực trong 30 phút và chỉ sử dụng được một lần.</p>
                 """,
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            token.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            message = "Nếu email tồn tại, hệ thống sẽ gửi hướng dẫn đặt lại mật khẩu."
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.Token);
        var resetToken = await _context.PasswordResetTokens
            .AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);

        if (resetToken == null
            || resetToken.UsedAt.HasValue
            || resetToken.ExpiresAt <= DateTime.UtcNow
            || resetToken.User == null
            || !resetToken.User.IsActive)
        {
            return BadRequest(new { message = "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var usedAt = DateTime.UtcNow;
        var claimed = await _context.PasswordResetTokens
            .Where(item => item.Id == resetToken.Id
                && item.UsedAt == null
                && item.ExpiresAt > usedAt)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.UsedAt, (DateTime?)usedAt),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(new { message = "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." });
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var updatedUsers = await _context.Users
            .Where(item => item.Id == resetToken.UserId && item.IsActive)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.PasswordHash, newPasswordHash)
                    .SetProperty(item => item.TokenVersion, item => item.TokenVersion + 1),
                cancellationToken);
        if (updatedUsers == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(new { message = "Tài khoản không còn hoạt động." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "PasswordReset",
            "User",
            resetToken.UserId,
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new { message = "Đặt lại mật khẩu thành công." });
    }

    private object CreateLoginResponse(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
        var accessTokenMinutes = Math.Clamp(
            jwtSection.GetValue("AccessTokenMinutes", 30),
            5,
            120);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("token_version", user.TokenVersion.ToString())
            ]),
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSection["Issuer"],
            Audience = jwtSection["Audience"]
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return new
        {
            token = handler.WriteToken(token),
            role = user.Role,
            username = user.Username
        };
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
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
