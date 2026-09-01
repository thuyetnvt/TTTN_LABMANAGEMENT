using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Dtos;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/maintenance-schedules")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class MaintenanceScheduleController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public MaintenanceScheduleController(
        AppDbContext context,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public sealed class ScheduleDto
    {
        [Range(1, int.MaxValue)] public int EquipmentId { get; set; }
        [Required, MaxLength(255)] public string Name { get; set; } = string.Empty;
        [Range(1, 3650)] public int IntervalDays { get; set; }
        [Required, MaxLength(20)] public string IntervalUnit { get; set; } = "DAY";
        public DateTime NextDueAt { get; set; }
        [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
        [MaxLength(4000)] public string Checklist { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var schedules = await _context.MaintenanceSchedules.AsNoTracking()
            .Include(schedule => schedule.Equipment)
            .OrderBy(schedule => schedule.NextDueAt)
            .Select(schedule => new
            {
                id = schedule.Id,
                equipmentId = schedule.EquipmentId,
                device = schedule.Equipment!.Name,
                serial = schedule.Equipment.Serial,
                name = schedule.Name,
                intervalDays = schedule.IntervalDays,
                intervalUnit = schedule.IntervalUnit,
                nextDueAt = schedule.NextDueAt,
                lastGeneratedAt = schedule.LastGeneratedAt,
                isActive = schedule.IsActive,
                isDue = schedule.IsActive && schedule.NextDueAt <= now,
                notes = schedule.Notes,
                checklist = schedule.Checklist
            })
            .ToListAsync(cancellationToken);
        return Ok(schedules);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetAllPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _context.MaintenanceSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Equipment)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(schedule =>
                schedule.Name.Contains(search)
                || schedule.Equipment!.Name.Contains(search)
                || schedule.Equipment.Serial.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim().ToUpperInvariant();
            if (status == "DUE") query = query.Where(schedule => schedule.IsActive && schedule.NextDueAt <= now);
            else if (status == "ACTIVE") query = query.Where(schedule => schedule.IsActive);
            else if (status == "INACTIVE") query = query.Where(schedule => !schedule.IsActive);
        }

        var page = await query
            .OrderBy(schedule => schedule.NextDueAt)
            .ThenBy(schedule => schedule.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(schedule => (object)new
        {
            id = schedule.Id,
            equipmentId = schedule.EquipmentId,
            device = schedule.Equipment!.Name,
            serial = schedule.Equipment.Serial,
            name = schedule.Name,
            intervalDays = schedule.IntervalDays,
            intervalUnit = schedule.IntervalUnit,
            nextDueAt = schedule.NextDueAt,
            lastGeneratedAt = schedule.LastGeneratedAt,
            isActive = schedule.IsActive,
            isDue = schedule.IsActive && schedule.NextDueAt <= now,
            notes = schedule.Notes,
            checklist = schedule.Checklist
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScheduleDto dto, CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        dto.Notes = dto.Notes.Trim();
        dto.Checklist = dto.Checklist.Trim();
        dto.IntervalUnit = dto.IntervalUnit.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên kế hoạch bảo trì là bắt buộc." });
        }
        if (!IsValidIntervalUnit(dto.IntervalUnit))
            return BadRequest(new { message = "Đơn vị chu kỳ phải là ngày, tuần, tháng, quý hoặc năm." });

        var equipmentExists = await _context.Equipments.AnyAsync(
            equipment => equipment.Id == dto.EquipmentId, cancellationToken);
        if (!equipmentExists)
        {
            return BadRequest(new { message = "Thiết bị không tồn tại." });
        }

        var userId = GetUserId();
        var schedule = new MaintenanceSchedule
        {
            EquipmentId = dto.EquipmentId,
            Name = dto.Name,
            IntervalDays = dto.IntervalDays,
            IntervalUnit = dto.IntervalUnit,
            NextDueAt = dto.NextDueAt == default ? AddInterval(DateTime.UtcNow, dto.IntervalDays, dto.IntervalUnit) : dto.NextDueAt,
            IsActive = dto.IsActive,
            Notes = dto.Notes,
            Checklist = dto.Checklist,
            CreatedByUserId = userId
        };
        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Create", nameof(MaintenanceSchedule), schedule.Id,
            new { schedule.EquipmentId, schedule.IntervalDays }, cancellationToken);
        return Created($"/api/maintenance-schedules/{schedule.Id}", schedule);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ScheduleDto dto, CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        dto.Notes = dto.Notes.Trim();
        dto.Checklist = dto.Checklist.Trim();
        dto.IntervalUnit = dto.IntervalUnit.Trim().ToUpperInvariant();
        var schedule = await _context.MaintenanceSchedules.FindAsync([id], cancellationToken);
        if (schedule is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Tên kế hoạch bảo trì là bắt buộc." });
        if (!IsValidIntervalUnit(dto.IntervalUnit)) return BadRequest(new { message = "Đơn vị chu kỳ không hợp lệ." });
        if (!await _context.Equipments.AnyAsync(equipment => equipment.Id == dto.EquipmentId, cancellationToken))
            return BadRequest(new { message = "Thiết bị không tồn tại." });

        schedule.EquipmentId = dto.EquipmentId;
        schedule.Name = dto.Name;
        schedule.IntervalDays = dto.IntervalDays;
        schedule.IntervalUnit = dto.IntervalUnit;
        schedule.NextDueAt = dto.NextDueAt == default ? schedule.NextDueAt : dto.NextDueAt;
        schedule.IsActive = dto.IsActive;
        schedule.Notes = dto.Notes;
        schedule.Checklist = dto.Checklist;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(schedule);
    }

    [HttpPost("{id:int}/generate")]
    public async Task<IActionResult> GenerateMaintenanceRecord(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var schedule = await _context.MaintenanceSchedules
            .Include(item => item.Equipment)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (schedule is null) return NotFound();
        if (!schedule.IsActive) return Conflict(new { message = "Kế hoạch bảo trì đang tắt." });
        if (schedule.Equipment is null) return BadRequest(new { message = "Thiết bị của kế hoạch không tồn tại." });
        var hasActiveBorrow = await _context.BorrowRecords.AsNoTracking()
            .AnyAsync(record => (record.EquipmentId == schedule.EquipmentId
                    || record.Details.Any(detail => detail.EquipmentId == schedule.EquipmentId))
                && (record.Status == BorrowStatuses.Approved || record.Status == BorrowStatuses.Borrowed),
                cancellationToken);
        if (hasActiveBorrow
            || schedule.Equipment.Status is EquipmentStatuses.Borrowed or EquipmentStatuses.BorrowPending)
            return Conflict(new { message = "Không thể tạo bảo trì cho thiết bị đang được mượn." });
        if (await _context.MaintenanceRecords.AnyAsync(record => record.EquipmentId == schedule.EquipmentId
            && record.Status == MaintenanceStatuses.InProgress, cancellationToken))
            return Conflict(new { message = "Thiết bị đã có phiếu bảo trì đang xử lý." });

        var record = new MaintenanceRecord
        {
            EquipmentId = schedule.EquipmentId,
            MaintenanceDate = DateTime.UtcNow,
            Description = $"Theo kế hoạch: {schedule.Name}",
            Cost = 0,
            PerformedBy = "Theo kế hoạch",
            Status = MaintenanceStatuses.InProgress,
            Checklist = schedule.Checklist,
            ActiveEquipmentKey = $"EQ:{schedule.EquipmentId}"
        };
        schedule.Equipment.Status = EquipmentStatuses.MaintenanceInProgress;
        schedule.LastGeneratedAt = DateTime.UtcNow;
        schedule.NextDueAt = OperationalAutomationRunner.GetNextFutureDue(
            schedule.NextDueAt,
            schedule.IntervalDays,
            schedule.IntervalUnit,
            DateTime.UtcNow);
        schedule.UpdatedAt = DateTime.UtcNow;
        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "MAINTENANCE_SCHEDULE_GENERATED",
            "Đã tạo nhiệm vụ bảo trì định kỳ",
            $"Kế hoạch {schedule.Name} đã sinh phiếu #{record.Id}.",
            "/dashboard/maintenance",
            cancellationToken);
        await _auditService.WriteAsync(HttpContext, "GenerateMaintenance", nameof(MaintenanceSchedule), id,
            new { record.Id, schedule.NextDueAt }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "Đã tạo phiếu bảo trì theo kế hoạch.", maintenanceId = record.Id, nextDueAt = schedule.NextDueAt });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var schedule = await _context.MaintenanceSchedules.FindAsync([id], cancellationToken);
        if (schedule is null) return NotFound();
        _context.MaintenanceSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static bool IsValidIntervalUnit(string value)
        => value is "DAY" or "WEEK" or "MONTH" or "QUARTER" or "YEAR";

    private static DateTime AddInterval(DateTime value, int amount, string unit)
        => unit switch
        {
            "WEEK" => value.AddDays(amount * 7),
            "MONTH" => value.AddMonths(amount),
            "QUARTER" => value.AddMonths(amount * 3),
            "YEAR" => value.AddYears(amount),
            _ => value.AddDays(amount)
        };
}
