using System.ComponentModel.DataAnnotations;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EquipmentController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
        };

    private static readonly Dictionary<string, string> ContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public EquipmentController(
        AppDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _auditService = auditService;
    }

    public sealed class EquipmentFormDto
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Model { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Serial { get; set; } = string.Empty;

        [MaxLength(255)]
        public string SerialName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(255)]
        public string ResponsiblePerson { get; set; } = string.Empty;

        public DateTime? EntryDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }

        [MaxLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public string Status { get; set; } = EquipmentStatuses.Available;
        public int? AssetCategoryId { get; set; }
        public IFormFile? DecisionFile { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEquipments(
        CancellationToken cancellationToken)
    {
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .OrderByDescending(equipment => equipment.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(equipments.Select(equipment => new
        {
            equipment.Id,
            equipment.Name,
            equipment.Model,
            equipment.Serial,
            equipment.SerialName,
            equipment.Location,
            equipment.ResponsiblePerson,
            equipment.DecisionFileName,
            HasDecisionFile = !string.IsNullOrEmpty(equipment.DecisionFilePath),
            equipment.EntryDate,
            equipment.WarrantyExpiry,
            equipment.InvoiceNumber,
            equipment.Status,
            equipment.BorrowCount,
            equipment.AssetCategoryId,
            CategoryName = equipment.AssetCategory?.Name,
            equipment.CreatedAt
        }));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    [RequestSizeLimit(11_000_000)]
    public async Task<ActionResult<Equipment>> PostEquipment(
        [FromForm] EquipmentFormDto dto,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredEquipmentFields(dto))
        {
            return BadRequest(new { message = "Tên, model, số seri và vị trí là bắt buộc." });
        }

        if (dto.DecisionFile == null)
        {
            return BadRequest(new { message = "Vui lòng tải lên file quyết định mua/thêm thiết bị." });
        }

        var fileValidationMessage = ValidateDecisionFile(dto.DecisionFile);
        if (fileValidationMessage is not null)
        {
            return BadRequest(new { message = fileValidationMessage });
        }

        var serial = dto.Serial.Trim();
        if (await _context.Equipments.AnyAsync(
                equipment => equipment.Serial == serial,
                cancellationToken))
        {
            return BadRequest(new { message = "Số seri đã tồn tại." });
        }

        if (dto.AssetCategoryId.HasValue
            && !await _context.AssetCategories.AnyAsync(
                category => category.Id == dto.AssetCategoryId,
                cancellationToken))
        {
            return BadRequest(new { message = "Danh mục không tồn tại." });
        }

        var equipment = new Equipment
        {
            Name = dto.Name.Trim(),
            Model = dto.Model.Trim(),
            Serial = serial,
            SerialName = dto.SerialName.Trim(),
            Location = dto.Location.Trim(),
            ResponsiblePerson = dto.ResponsiblePerson.Trim(),
            EntryDate = dto.EntryDate,
            WarrantyExpiry = dto.WarrantyExpiry,
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            Status = EquipmentStatuses.Available,
            AssetCategoryId = dto.AssetCategoryId,
            CreatedAt = DateTime.UtcNow
        };

        var storedPath = await SaveDecisionFileAsync(
            equipment,
            dto.DecisionFile,
            cancellationToken);
        try
        {
            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteFileIfExists(storedPath);
            throw;
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            "Equipment",
            equipment.Id,
            new { equipment.Name, equipment.Serial },
            cancellationToken);

        return Ok(equipment);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Managers)]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> PutEquipment(
        int id,
        [FromForm] EquipmentFormDto dto,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Equipments.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (!HasRequiredEquipmentFields(dto))
        {
            return BadRequest(new { message = "Tên, model, số seri và vị trí là bắt buộc." });
        }

        if (dto.DecisionFile is not null)
        {
            var fileValidationMessage = ValidateDecisionFile(dto.DecisionFile);
            if (fileValidationMessage is not null)
            {
                return BadRequest(new { message = fileValidationMessage });
            }
        }

        var serial = dto.Serial.Trim();
        if (await _context.Equipments.AnyAsync(
                equipment => equipment.Id != id && equipment.Serial == serial,
                cancellationToken))
        {
            return BadRequest(new { message = "Số seri đã tồn tại." });
        }

        if (!EquipmentStatuses.All.Contains(dto.Status))
        {
            return BadRequest(new { message = "Trạng thái thiết bị không hợp lệ." });
        }

        if (existing.Status == EquipmentStatuses.Borrowed
            && dto.Status != EquipmentStatuses.Borrowed)
        {
            return BadRequest(new
            {
                message = "Thiết bị đang mượn chỉ được cập nhật trạng thái qua quy trình trả."
            });
        }

        var hasActiveMaintenance = await _context.MaintenanceRecords.AnyAsync(
            record => record.EquipmentId == id && record.Status == "Đang xử lý",
            cancellationToken);
        if (hasActiveMaintenance && dto.Status != existing.Status)
        {
            return BadRequest(new
            {
                message = "Thiết bị đang bảo trì; hãy hoàn tất phiếu bảo trì để cập nhật trạng thái."
            });
        }

        if (dto.AssetCategoryId.HasValue
            && !await _context.AssetCategories.AnyAsync(
                category => category.Id == dto.AssetCategoryId,
                cancellationToken))
        {
            return BadRequest(new { message = "Danh mục không tồn tại." });
        }

        var before = SnapshotEquipment(existing);

        existing.Name = dto.Name.Trim();
        existing.Model = dto.Model.Trim();
        existing.Serial = serial;
        existing.SerialName = dto.SerialName.Trim();
        existing.Location = dto.Location.Trim();
        existing.ResponsiblePerson = dto.ResponsiblePerson.Trim();
        existing.EntryDate = dto.EntryDate;
        existing.WarrantyExpiry = dto.WarrantyExpiry;
        existing.InvoiceNumber = dto.InvoiceNumber.Trim();
        existing.Status = dto.Status;
        existing.AssetCategoryId = dto.AssetCategoryId;

        string? previousPath = null;
        string? newPath = null;
        if (dto.DecisionFile != null)
        {
            previousPath = existing.DecisionFilePath;
            newPath = await SaveDecisionFileAsync(
                existing,
                dto.DecisionFile,
                cancellationToken);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(newPath))
            {
                DeleteFileIfExists(newPath);
            }
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousPath)
            && !string.Equals(previousPath, newPath, StringComparison.Ordinal))
        {
            DeleteFileIfExists(previousPath);
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Update",
            "Equipment",
            existing.Id,
            new
            {
                Before = before,
                After = SnapshotEquipment(existing),
                ChangedDecisionFile = dto.DecisionFile is not null
            },
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:int}/decision-file")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> DownloadDecisionFile(
        int id,
        CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (equipment == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(equipment.DecisionFilePath))
        {
            return NotFound("Không tìm thấy file quyết định.");
        }

        var uploadDirectory = GetUploadDirectory();
        var storedPath = Path.GetFullPath(equipment.DecisionFilePath);
        if (!storedPath.StartsWith(
                uploadDirectory + Path.DirectorySeparatorChar,
                StringComparison.Ordinal)
            || !System.IO.File.Exists(storedPath))
        {
            return NotFound("Không tìm thấy file quyết định.");
        }

        var extension = Path.GetExtension(storedPath);
        var contentType = ContentTypes.GetValueOrDefault(extension, "application/octet-stream");
        return PhysicalFile(
            storedPath,
            contentType,
            equipment.DecisionFileName,
            enableRangeProcessing: true);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteEquipment(
        int id,
        CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipments.FindAsync(new object[] { id }, cancellationToken);
        if (equipment == null)
        {
            return NotFound();
        }

        var hasHistory = await _context.BorrowRecords.AnyAsync(
                record => record.EquipmentId == id,
                cancellationToken)
            || await _context.MaintenanceRecords.AnyAsync(
                record => record.EquipmentId == id,
                cancellationToken)
            || await _context.Penalties.AnyAsync(
                record => record.EquipmentId == id,
                cancellationToken);
        if (hasHistory)
        {
            return BadRequest("Không thể xóa thiết bị đã có lịch sử nghiệp vụ.");
        }

        var filePath = equipment.DecisionFilePath;
        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync(cancellationToken);
        DeleteFileIfExists(filePath);

        await _auditService.WriteAsync(
            HttpContext,
            "Delete",
            "Equipment",
            id,
            new { equipment.Name, equipment.Serial },
            cancellationToken);

        return NoContent();
    }

    [HttpGet("export")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> ExportEquipment(CancellationToken cancellationToken)
    {
        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .OrderByDescending(equipment => equipment.CreatedAt)
            .ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TaiSan");
        var headers = new[]
        {
            "ID", "Danh mục", "Tên tài sản", "Model", "Số seri", "Tên seri",
            "Vị trí", "Người chịu trách nhiệm", "Ngày nhập", "Hạn bảo hành",
            "Số hóa đơn", "Trạng thái", "Số lần mượn"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cells[1, index + 1].Value = headers[index];
        }

        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        for (var index = 0; index < equipments.Count; index++)
        {
            var equipment = equipments[index];
            var row = index + 2;
            worksheet.Cells[row, 1].Value = equipment.Id;
            worksheet.Cells[row, 2].Value = equipment.AssetCategory?.Name;
            worksheet.Cells[row, 3].Value = equipment.Name;
            worksheet.Cells[row, 4].Value = equipment.Model;
            worksheet.Cells[row, 5].Value = equipment.Serial;
            worksheet.Cells[row, 6].Value = equipment.SerialName;
            worksheet.Cells[row, 7].Value = equipment.Location;
            worksheet.Cells[row, 8].Value = equipment.ResponsiblePerson;
            worksheet.Cells[row, 9].Value = equipment.EntryDate?.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 10].Value = equipment.WarrantyExpiry?.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 11].Value = equipment.InvoiceNumber;
            worksheet.Cells[row, 12].Value = equipment.Status;
            worksheet.Cells[row, 13].Value = equipment.BorrowCount;
        }

        worksheet.Cells.AutoFitColumns();
        var stream = new MemoryStream();
        await package.SaveAsAsync(stream, cancellationToken);
        stream.Position = 0;
        var fileName = $"ThongKe_TaiSan_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(
            stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task<string> SaveDecisionFileAsync(
        Equipment equipment,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        var uploadDir = GetUploadDirectory();
        Directory.CreateDirectory(uploadDir);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(uploadDir, storedName);
        await using var stream = new FileStream(
            storedPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        await file.CopyToAsync(stream, cancellationToken);

        equipment.DecisionFileName = originalName;
        equipment.DecisionFilePath = storedPath;
        equipment.DecisionUploadedAt = DateTime.UtcNow;
        return storedPath;
    }

    private string? ValidateDecisionFile(IFormFile file)
    {
        var maxBytes = _configuration.GetValue(
            "Uploads:MaxDecisionFileBytes",
            10 * 1024 * 1024L);
        if (file.Length <= 0 || file.Length > maxBytes)
        {
            return $"File quyết định rỗng hoặc vượt quá {maxBytes / (1024 * 1024)} MB.";
        }

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        return AllowedExtensions.Contains(extension)
            ? null
            : "Định dạng file quyết định không được hỗ trợ.";
    }

    private string GetUploadDirectory()
    {
        return Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, "uploads", "equipment-decisions"));
    }

    private static bool HasRequiredEquipmentFields(EquipmentFormDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Name)
            && !string.IsNullOrWhiteSpace(dto.Model)
            && !string.IsNullOrWhiteSpace(dto.Serial)
            && !string.IsNullOrWhiteSpace(dto.Location);
    }

    private static void DeleteFileIfExists(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    private static object SnapshotEquipment(Equipment equipment)
    {
        return new
        {
            equipment.Name,
            equipment.Model,
            equipment.Serial,
            equipment.SerialName,
            equipment.Location,
            equipment.ResponsiblePerson,
            equipment.EntryDate,
            equipment.WarrantyExpiry,
            equipment.InvoiceNumber,
            equipment.Status,
            equipment.AssetCategoryId,
            equipment.DecisionFileName
        };
    }
}
