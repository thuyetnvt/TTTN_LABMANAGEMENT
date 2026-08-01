using System.ComponentModel.DataAnnotations;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class MaintenanceController : ControllerBase
{
    private const string InProgress = "Đang xử lý";
    private const string Completing = "Đang hoàn tất";
    private const string Completed = "Hoàn thành";

    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public MaintenanceController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public sealed class CreateMaintenanceDto
    {
        [Range(1, int.MaxValue)]
        public int EquipmentId { get; set; }

        public DateTime MaintenanceDate { get; set; }

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; }

        [Required, MaxLength(255)]
        public string PerformedBy { get; set; } = string.Empty;
    }

    public sealed class CompleteMaintenanceDto
    {
        [Required, MaxLength(2000)]
        public string Result { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMaintenanceRecords(
        CancellationToken cancellationToken)
    {
        var records = await _context.MaintenanceRecords
            .AsNoTracking()
            .Include(record => record.Equipment)
            .OrderByDescending(record => record.MaintenanceDate)
            .Select(record => new
            {
                id = record.Id,
                equipmentId = record.EquipmentId,
                device = record.Equipment!.Name,
                maintenanceDate = record.MaintenanceDate,
                description = record.Description,
                cost = record.Cost,
                performedBy = record.PerformedBy,
                status = record.Status,
                completedAt = record.CompletedAt,
                result = record.Result
            })
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceRecord>> CreateMaintenance(
        [FromBody] CreateMaintenanceDto dto,
        CancellationToken cancellationToken)
    {
        dto.Description = dto.Description.Trim();
        dto.PerformedBy = dto.PerformedBy.Trim();
        if (string.IsNullOrWhiteSpace(dto.Description)
            || string.IsNullOrWhiteSpace(dto.PerformedBy))
        {
            return BadRequest(new { message = "Nội dung và người thực hiện là bắt buộc." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimedEquipment = await _context.Equipments
            .Where(equipment => equipment.Id == dto.EquipmentId
                && equipment.Status != EquipmentStatuses.Borrowed)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    equipment => equipment.Status,
                    EquipmentStatuses.Warranty),
                cancellationToken);
        if (claimedEquipment == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            var exists = await _context.Equipments.AnyAsync(
                equipment => equipment.Id == dto.EquipmentId,
                cancellationToken);
            if (exists)
            {
                return Conflict(new { message = "Không thể tạo bảo trì khi thiết bị đang được mượn." });
            }

            return BadRequest(new { message = "Thiết bị không tồn tại." });
        }

        var hasActiveMaintenance = await _context.MaintenanceRecords.AnyAsync(
            record => record.EquipmentId == dto.EquipmentId
                && record.Status == InProgress,
            cancellationToken);
        if (hasActiveMaintenance)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Thiết bị đã có một phiếu bảo trì đang xử lý." });
        }

        var record = new MaintenanceRecord
        {
            EquipmentId = dto.EquipmentId,
            MaintenanceDate = dto.MaintenanceDate == default
                ? DateTime.UtcNow
                : dto.MaintenanceDate,
            Description = dto.Description,
            Cost = dto.Cost,
            PerformedBy = dto.PerformedBy,
            Status = InProgress
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(MaintenanceRecord),
            record.Id,
            new { record.EquipmentId, record.Cost },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(record);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> CompleteMaintenance(
        int id,
        [FromBody] CompleteMaintenanceDto dto,
        CancellationToken cancellationToken)
    {
        dto.Result = dto.Result.Trim();
        if (string.IsNullOrWhiteSpace(dto.Result))
        {
            return BadRequest(new { message = "Kết quả bảo trì là bắt buộc." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await _context.MaintenanceRecords
            .Where(record => record.Id == id && record.Status == InProgress)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(record => record.Status, Completing),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Phiếu bảo trì không tồn tại hoặc đã được xử lý." });
        }

        var record = await _context.MaintenanceRecords
            .Include(item => item.Equipment)
            .FirstAsync(item => item.Id == id, cancellationToken);
        record.Status = Completed;
        record.Result = dto.Result;
        record.CompletedAt = DateTime.UtcNow;
        if (record.Equipment is not null)
        {
            record.Equipment.Status = EquipmentStatuses.Available;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Complete",
            nameof(MaintenanceRecord),
            id,
            new { record.EquipmentId },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "Đã hoàn tất bảo trì; thiết bị được chuyển về trạng thái Rảnh." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteMaintenance(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.MaintenanceRecords.FindAsync(
            new object[] { id },
            cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        if (record.Status == InProgress)
        {
            return BadRequest(new { message = "Hãy hoàn tất phiếu bảo trì trước khi xóa." });
        }

        _context.MaintenanceRecords.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Delete",
            nameof(MaintenanceRecord),
            id,
            new { record.EquipmentId },
            cancellationToken);
        return NoContent();
    }
}
