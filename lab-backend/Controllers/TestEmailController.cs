using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestEmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TestEmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> TestSendEmail([FromQuery] string toEmail)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return BadRequest(new { message = "Vui lòng cung cấp địa chỉ email nhận (toEmail)." });
        }

        try
        {
            var testSubject = "[LabManagement] Kiểm tra cấu hình hệ thống Email";
            var testBody = $"<h3>Xin chào!</h3><p>Đây là email tự động gửi từ hệ thống LabManagement để kiểm tra tính năng gửi thư qua SMTP.</p><p>Thời gian gửi: <strong>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</strong></p>";

            await _emailService.SendEmailAsync(toEmail, testSubject, testBody, HttpContext.RequestAborted);
            
            return Ok(new { message = $"Email thử nghiệm đã được gửi thành công tới {toEmail}." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                message = "Không thể gửi email. Có lỗi xảy ra với cấu hình SMTP.", 
                error = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }
}
