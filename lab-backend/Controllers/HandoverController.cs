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
public class HandoverController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public HandoverController(AppDbContext context, IAuditService auditService, INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public sealed class HandoverItemDto
    {
        public int EquipmentId { get; set; }
        [Required, MaxLength(50)] public string Condition { get; set; } = EquipmentStatuses.Available;
        [MaxLength(1000)] public string Accessories { get; set; } = string.Empty;
        [MaxLength(2000)] public string Note { get; set; } = string.Empty;
    }

    public sealed class CreateHandoverDto
    {
        [Range(1, int.MaxValue)] public int BorrowRecordId { get; set; }
        [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
        [MinLength(1)] public List<HandoverItemDto> Items { get; set; } = new();
    }

    [HttpGet("{borrowRecordId:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<object>> Get(int borrowRecordId, CancellationToken cancellationToken)
    {
        var handover = await _context.HandoverRecords.AsNoTracking()
            .Include(item => item.Items).ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.BorrowRecordId == borrowRecordId, cancellationToken);
        if (handover is null) return NotFound(new { message = "Phiếu chưa có biên bản bàn giao." });
        return Ok(new
        {
            handover.Id, handover.Code, handover.BorrowRecordId, handover.HandoverAt, handover.Notes, handover.ConfirmedAt,
            items = handover.Items.Select(item => new
            {
                item.EquipmentId, equipmentName = item.Equipment!.Name, serial = item.Equipment.Serial,
                item.Condition, item.Accessories, item.Note
            })
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<object>> Create([FromBody] CreateHandoverDto dto, CancellationToken cancellationToken)
    {
        dto.Notes = dto.Notes.Trim();
        foreach (var item in dto.Items)
        {
            item.Condition = item.Condition.Trim(); item.Accessories = item.Accessories.Trim(); item.Note = item.Note.Trim();
            if (item.Condition is not (EquipmentStatuses.Available or EquipmentStatuses.Broken))
                return BadRequest(new { message = "Tình trạng bàn giao không hợp lệ." });
        }

        var record = await _context.BorrowRecords.Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.Id == dto.BorrowRecordId, cancellationToken);
        if (record is null) return NotFound(new { message = "Không tìm thấy phiếu mượn." });
        if (record.Status != BorrowStatuses.Borrowed) return Conflict(new { message = "Chỉ được lập biên bản cho phiếu đang mượn." });
        if (await _context.HandoverRecords.AnyAsync(item => item.BorrowRecordId == record.Id, cancellationToken))
            return Conflict(new { message = "Phiếu đã có biên bản bàn giao." });

        var detailIds = record.Details.Select(item => item.EquipmentId).Distinct().ToHashSet();
        var submittedIds = dto.Items.Select(item => item.EquipmentId).Distinct().ToHashSet();
        if (!detailIds.SetEquals(submittedIds)) return BadRequest(new { message = "Biên bản phải ghi nhận đủ từng tài sản trong phiếu." });

        var handover = new HandoverRecord
        {
            Code = $"BH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28],
            BorrowRecordId = record.Id, HandedOverByUserId = GetCurrentUserId(), ReceivedByUserId = record.UserId,
            Notes = dto.Notes, ConfirmedAt = DateTime.UtcNow,
            Items = dto.Items.Select(item => new HandoverItem
            {
                EquipmentId = item.EquipmentId, Condition = item.Condition, Accessories = item.Accessories, Note = item.Note
            }).ToList()
        };
        _context.HandoverRecords.Add(handover);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Create", nameof(HandoverRecord), handover.Id, new { handover.Code, handover.BorrowRecordId }, cancellationToken);
        await _notificationService.NotifyUserAsync(record.UserId, "HANDOVER_CREATED", "Đã lập biên bản bàn giao", $"Biên bản {handover.Code} đã được lập cho phiếu mượn.", "/dashboard/borrow-history", cancellationToken);
        return Ok(new { handover.Id, handover.Code, message = "Đã lập biên bản bàn giao." });
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
