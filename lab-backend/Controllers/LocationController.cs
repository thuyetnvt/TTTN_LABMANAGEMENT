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
[Authorize]
public class LocationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public LocationController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public sealed class LocationDto
    {
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetLocations(CancellationToken cancellationToken)
    {
        var locations = await _context.LocationNodes
            .AsNoTracking()
            .Where(location => location.Type != "BUILDING")
            .OrderBy(location => location.Code)
            .Select(location => new
            {
                location.Id,
                location.Code,
                location.Name,
                location.Type,
                location.Description,
                location.IsActive,
                equipmentCount = _context.Equipments.Count(equipment => equipment.LocationNodeId == location.Id)
            })
            .ToListAsync(cancellationToken);

        return Ok(locations);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult> CreateLocation(
        [FromBody] LocationDto dto,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateLocationAsync(dto, null, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        var location = new LocationNode
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Type = dto.Type.Trim().ToUpperInvariant(),
            Description = dto.Description.Trim(),
            IsActive = dto.IsActive
        };
        _context.LocationNodes.Add(location);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Create", nameof(LocationNode), location.Id,
            new { location.Code }, cancellationToken);
        return Ok(location);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> UpdateLocation(
        int id,
        [FromBody] LocationDto dto,
        CancellationToken cancellationToken)
    {
        var location = await _context.LocationNodes.FindAsync(new object[] { id }, cancellationToken);
        if (location is null)
        {
            return NotFound(new { message = "Không tìm thấy vị trí." });
        }

        var validation = await ValidateLocationAsync(dto, id, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(new { message = validation });
        }

        location.Code = dto.Code.Trim().ToUpperInvariant();
        location.Name = dto.Name.Trim();
        location.Type = dto.Type.Trim().ToUpperInvariant();
        location.Description = dto.Description.Trim();
        location.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Update", nameof(LocationNode), id,
            new { location.Code }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> DeleteLocation(int id, CancellationToken cancellationToken)
    {
        var hasEquipment = await _context.Equipments.AnyAsync(equipment => equipment.LocationNodeId == id, cancellationToken);
        if (hasEquipment)
        {
            return Conflict(new { message = "Không thể xóa vị trí còn tài sản. Hãy chuyển tài sản hoặc ngừng sử dụng vị trí." });
        }

        var location = await _context.LocationNodes.FindAsync(new object[] { id }, cancellationToken);
        if (location is null)
        {
            return NotFound();
        }

        _context.LocationNodes.Remove(location);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Delete", nameof(LocationNode), id,
            new { location.Code }, cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateLocationAsync(LocationDto dto, int? currentId, CancellationToken cancellationToken)
    {
        dto.Code = dto.Code.Trim();
        dto.Name = dto.Name.Trim();
        dto.Type = dto.Type.Trim();
        if (string.IsNullOrWhiteSpace(dto.Code)
            || string.IsNullOrWhiteSpace(dto.Name)
            || string.IsNullOrWhiteSpace(dto.Type))
        {
            return "Mã, tên và loại vị trí là bắt buộc.";
        }

        if (await _context.LocationNodes.AnyAsync(
                location => location.Id != currentId && location.Code == dto.Code.ToUpper(),
                cancellationToken))
        {
            return "Mã vị trí đã tồn tại.";
        }

        return null;
    }
}
