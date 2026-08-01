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
public class AssetCategoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public AssetCategoryController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public sealed class CategoryDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetCategory>>> GetCategories(
        CancellationToken cancellationToken)
    {
        return await _context.AssetCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<AssetCategory>> CreateCategory(
        [FromBody] CategoryDto dto,
        CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        dto.Description = dto.Description.Trim();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên danh mục là bắt buộc." });
        }

        if (await _context.AssetCategories.AnyAsync(
                category => category.Name == dto.Name,
                cancellationToken))
        {
            return Conflict(new { message = "Tên danh mục đã tồn tại." });
        }

        var category = new AssetCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };
        _context.AssetCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(AssetCategory),
            category.Id,
            new { category.Name },
            cancellationToken);
        return Ok(category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] CategoryDto dto,
        CancellationToken cancellationToken)
    {
        var existing = await _context.AssetCategories.FindAsync(
            new object[] { id },
            cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        dto.Name = dto.Name.Trim();
        dto.Description = dto.Description.Trim();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên danh mục là bắt buộc." });
        }

        if (await _context.AssetCategories.AnyAsync(
                category => category.Id != id && category.Name == dto.Name,
                cancellationToken))
        {
            return Conflict(new { message = "Tên danh mục đã tồn tại." });
        }

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Update",
            nameof(AssetCategory),
            id,
            new { existing.Name },
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var isUsed = await _context.Equipments.AnyAsync(
                equipment => equipment.AssetCategoryId == id,
                cancellationToken)
            || await _context.Consumables.AnyAsync(
                consumable => consumable.AssetCategoryId == id,
                cancellationToken);
        if (isUsed)
        {
            return BadRequest(new { message = "Không thể xóa danh mục đang được sử dụng." });
        }

        var category = await _context.AssetCategories.FindAsync(
            new object[] { id },
            cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        _context.AssetCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Delete",
            nameof(AssetCategory),
            id,
            new { category.Name },
            cancellationToken);
        return NoContent();
    }
}
