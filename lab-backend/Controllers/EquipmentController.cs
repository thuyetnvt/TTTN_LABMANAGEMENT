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
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly IFileStorage _fileStorage;

    public EquipmentController(
        AppDbContext context,
        IConfiguration configuration,
        IAuditService auditService,
        IFileStorage fileStorage)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
        _fileStorage = fileStorage;
    }

    public sealed class EquipmentFormDto
    {
        [MaxLength(100)]
        public string AssetCode { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Model { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Serial { get; set; } = string.Empty;

        [MaxLength(255)]
        public string SerialName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string MacAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Imei { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirmwareVersion { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Manufacturer { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Supplier { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FundingSource { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal? PurchaseValue { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        public int? LocationNodeId { get; set; }

        [MaxLength(1000)]
        public string LocationChangeReason { get; set; } = string.Empty;

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

    public sealed class ImportEquipmentRowDto
    {
        [MaxLength(100)] public string AssetCode { get; set; } = string.Empty;
        [Required, MaxLength(255)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(255)] public string Model { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Serial { get; set; } = string.Empty;
        [MaxLength(255)] public string SerialName { get; set; } = string.Empty;
        [Required, MaxLength(255)] public string Location { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        [MaxLength(255)] public string ResponsiblePerson { get; set; } = string.Empty;
        public DateTime? EntryDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        [MaxLength(100)] public string InvoiceNumber { get; set; } = string.Empty;
        [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
    }

    public sealed class ImportEquipmentDto
    {
        [Required, MinLength(1), MaxLength(500)]
        public List<ImportEquipmentRowDto> Rows { get; set; } = [];
    }

    public sealed class ResolveEquipmentQrDto
    {
        [Required, MaxLength(200)]
        public string QrToken { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetEquipments(
        CancellationToken cancellationToken)
    {
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .OrderByDescending(equipment => equipment.CreatedAt)
            .ToListAsync(cancellationToken);

        if (User.IsInRole(Roles.Admin)
            || User.IsInRole(Roles.LabHead)
            || User.IsInRole(Roles.DeputyLabHead))
        {
            return Ok(equipments.Select(ToManagerDto));
        }

        return Ok(equipments.Select(ToBorrowerDto));
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetEquipmentsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.Equipments
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .AsQueryable();

        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(equipment =>
                equipment.Name.Contains(search)
                || equipment.AssetCode.Contains(search)
                || equipment.Serial.Contains(search)
                || equipment.Model.Contains(search)
                || equipment.Location.Contains(search)
                || (equipment.AssetCategory != null && equipment.AssetCategory.Name.Contains(search))
                || (equipment.LocationNode != null
                    && (equipment.LocationNode.Name.Contains(search) || equipment.LocationNode.Code.Contains(search))));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            if (string.Equals(status, "PROBLEM", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(equipment => equipment.Status == EquipmentStatuses.Broken
                    || equipment.Status == EquipmentStatuses.UnderWarranty
                    || equipment.Status == EquipmentStatuses.Missing);
            }
            else if (string.Equals(status, "WARRANTY_SOON", StringComparison.OrdinalIgnoreCase))
            {
                var now = DateTime.UtcNow;
                var deadline = now.AddDays(30);
                query = query.Where(equipment => equipment.WarrantyExpiry.HasValue
                    && equipment.WarrantyExpiry.Value >= now
                    && equipment.WarrantyExpiry.Value <= deadline);
            }
            else
            {
                query = query.Where(equipment => equipment.Status == status);
            }
        }
        if (paging.CategoryId.HasValue)
        {
            query = query.Where(equipment => equipment.AssetCategoryId == paging.CategoryId.Value);
        }
        if (paging.LocationNodeId.HasValue)
        {
            query = query.Where(equipment => equipment.LocationNodeId == paging.LocationNodeId.Value);
        }

        var page = await query
            .AsSingleQuery()
            .OrderByDescending(equipment => equipment.CreatedAt)
            .ThenBy(equipment => equipment.Id)
            .ToPagedResultAsync(paging, cancellationToken);

        var isManager = User.IsInRole(Roles.Admin)
            || User.IsInRole(Roles.LabHead)
            || User.IsInRole(Roles.DeputyLabHead);
        if (isManager)
        {
            return Ok(page.Map(ToManagerDto));
        }

        return Ok(page.Map(ToBorrowerDto));
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupEquipments(
        [FromQuery, MaxLength(200)] string? search = null,
        [FromQuery, Range(1, 50)] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Equipments.AsNoTracking().AsQueryable();
        var keyword = search?.Trim() ?? string.Empty;
        if (keyword.Length > 0)
        {
            query = query.Where(item =>
                item.Name.Contains(keyword)
                || item.Serial.Contains(keyword)
                || item.AssetCode.Contains(keyword));
        }

        var items = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Serial,
                item.AssetCode,
                item.Status,
                item.Location
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("resolve-qr")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> ResolveQr(
        [FromBody] ResolveEquipmentQrDto dto,
        CancellationToken cancellationToken)
    {
        var token = dto.QrToken.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Mã QR không hợp lệ." });
        }

        var equipment = await _context.Equipments
            .AsNoTracking()
            .Include(item => item.AssetCategory)
            .Include(item => item.LocationNode)
            .SingleOrDefaultAsync(item => item.QrToken == token, cancellationToken);
        if (equipment is null)
        {
            return NotFound(new { message = "Không tìm thấy tài sản từ mã QR." });
        }

        // Endpoint quét không bao giờ trả token ngược lại, kể cả cho quản lý.
        return Ok(ToBorrowerDto(equipment));
    }

    [HttpPost("import/preview")]
    [EnableRateLimiting("sensitive")]
    [Authorize(Roles = Roles.Managers)]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> PreviewImport(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(Path.GetFileName(file?.FileName ?? string.Empty));
        if (file is null || file.Length <= 0 || !string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Vui lòng chọn file Excel .xlsx hợp lệ." });
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "File Excel không được vượt quá 10 MB." });
        }

        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        await using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet?.Dimension is null)
        {
            return BadRequest(new { message = "File Excel không có dữ liệu." });
        }

        var headerMap = BuildHeaderMap(worksheet);
        var requiredHeaderGroups = new[]
        {
            new[] { "Tên thiết bị", "Tên tài sản", "Name" },
            new[] { "Model" },
            new[] { "Số seri", "Serial" },
            new[] { "Vị trí", "Location" }
        };
        var missingHeaders = requiredHeaderGroups
            .Where(group => !group.Any(header => headerMap.ContainsKey(NormalizeImportHeader(header))))
            .Select(group => group[0])
            .ToArray();
        if (missingHeaders.Length > 0)
        {
            return BadRequest(new { message = $"Thiếu cột bắt buộc: {string.Join(", ", missingHeaders)}." });
        }

        var existingSerials = await _context.Equipments.AsNoTracking()
            .Select(equipment => equipment.Serial).ToListAsync(cancellationToken);
        var existingAssetCodes = await _context.Equipments.AsNoTracking()
            .Select(equipment => equipment.AssetCode).ToListAsync(cancellationToken);
        var serialSet = existingSerials.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodeSet = existingAssetCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var locationNodes = await _context.LocationNodes.AsNoTracking()
            .Where(location => location.IsActive)
            .ToListAsync(cancellationToken);
        var seenSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenAssetCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<object>();
        var validCount = 0;
        var lastRow = Math.Min(worksheet.Dimension.End.Row, 501);

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = ReadImportRow(worksheet, headerMap, rowNumber);
            if (string.IsNullOrWhiteSpace(row.Name) && string.IsNullOrWhiteSpace(row.Serial)) continue;
            var errors = new List<string>();
            row.LocationNodeId = ResolveImportLocation(row.Location, locationNodes);
            if (string.IsNullOrWhiteSpace(row.Name)) errors.Add("Thiếu tên thiết bị");
            if (string.IsNullOrWhiteSpace(row.Model)) errors.Add("Thiếu model");
            if (string.IsNullOrWhiteSpace(row.Serial)) errors.Add("Thiếu số seri");
            if (string.IsNullOrWhiteSpace(row.Location)) errors.Add("Thiếu vị trí");
            else if (!row.LocationNodeId.HasValue) errors.Add("Vị trí không khớp mã/tên trong cây vị trí");
            if (!string.IsNullOrWhiteSpace(row.Serial) && (serialSet.Contains(row.Serial) || !seenSerials.Add(row.Serial)))
                errors.Add("Số seri đã tồn tại hoặc bị trùng trong file");
            if (!string.IsNullOrWhiteSpace(row.AssetCode) && (assetCodeSet.Contains(row.AssetCode) || !seenAssetCodes.Add(row.AssetCode)))
                errors.Add("Mã tài sản đã tồn tại hoặc bị trùng trong file");
            var valid = errors.Count == 0;
            if (valid) validCount++;
            rows.Add(new { rowNumber, row, errors, valid });
        }

        return Ok(new { rows, total = rows.Count, validCount, invalidCount = rows.Count - validCount });
    }

    [HttpPost("import")]
    [EnableRateLimiting("sensitive")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> Import(
        [FromBody] ImportEquipmentDto dto,
        CancellationToken cancellationToken)
    {
        var rows = dto.Rows.Select(row =>
        {
            row.AssetCode = row.AssetCode.Trim();
            row.Name = row.Name.Trim();
            row.Model = row.Model.Trim();
            row.Serial = row.Serial.Trim();
            row.SerialName = row.SerialName.Trim();
            row.Location = row.Location.Trim();
            row.ResponsiblePerson = row.ResponsiblePerson.Trim();
            row.InvoiceNumber = row.InvoiceNumber.Trim();
            row.Notes = row.Notes.Trim();
            return row;
        }).ToList();
        var errors = rows.SelectMany((row, index) =>
        {
            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.Name)) rowErrors.Add("Thiếu tên thiết bị");
            if (string.IsNullOrWhiteSpace(row.Model)) rowErrors.Add("Thiếu model");
            if (string.IsNullOrWhiteSpace(row.Serial)) rowErrors.Add("Thiếu số seri");
            if (string.IsNullOrWhiteSpace(row.Location)) rowErrors.Add("Thiếu vị trí");
            if (!row.LocationNodeId.HasValue) rowErrors.Add("Thiếu vị trí hợp lệ trong cây vị trí");
            return rowErrors.Select(error => new { row = index + 1, error });
        }).ToList();
        if (errors.Count > 0) return BadRequest(new { message = "Dữ liệu import chưa hợp lệ.", errors });

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var serialSet = (await _context.Equipments.Select(equipment => equipment.Serial).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assetCodeSet = (await _context.Equipments.Select(equipment => equipment.AssetCode).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var locationNodes = await _context.LocationNodes.AsNoTracking()
            .Where(location => location.IsActive)
            .ToListAsync(cancellationToken);
        var imported = new List<Equipment>();
        foreach (var row in rows)
        {
            if (!row.LocationNodeId.HasValue || !locationNodes.Any(location => location.Id == row.LocationNodeId.Value))
                row.LocationNodeId = ResolveImportLocation(row.Location, locationNodes);
            if (!row.LocationNodeId.HasValue)
                return BadRequest(new { message = $"Vị trí của tài sản {row.Name} không tồn tại trong cây vị trí." });
            if (!serialSet.Add(row.Serial)) return Conflict(new { message = $"Số seri {row.Serial} đã tồn tại." });
            var assetCode = row.AssetCode;
            if (string.IsNullOrWhiteSpace(assetCode))
            {
                do { assetCode = CreateAssetCode(); } while (!assetCodeSet.Add(assetCode));
            }
            else if (!assetCodeSet.Add(assetCode))
            {
                return Conflict(new { message = $"Mã tài sản {assetCode} đã tồn tại." });
            }
            imported.Add(new Equipment
            {
                AssetCode = assetCode,
                Name = row.Name,
                Model = row.Model,
                Serial = row.Serial,
                SerialName = row.SerialName,
                Location = row.Location,
                LocationNodeId = row.LocationNodeId,
                ResponsiblePerson = row.ResponsiblePerson,
                EntryDate = row.EntryDate,
                WarrantyExpiry = row.WarrantyExpiry,
                InvoiceNumber = row.InvoiceNumber,
                Notes = row.Notes,
                Status = EquipmentStatuses.Available,
                CreatedAt = DateTime.UtcNow
            });
        }
        _context.Equipments.AddRange(imported);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Import", "Equipment", null,
            new { Count = imported.Count }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = $"Đã import {imported.Count} tài sản.", count = imported.Count });
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

        var fileValidationMessage = await ValidateDecisionFileAsync(dto.DecisionFile, cancellationToken);
        if (fileValidationMessage is not null)
        {
            return BadRequest(new { message = fileValidationMessage });
        }

        var serial = dto.Serial.Trim();
        var assetCode = string.IsNullOrWhiteSpace(dto.AssetCode)
            ? CreateAssetCode()
            : dto.AssetCode.Trim();
        if (await _context.Equipments.AnyAsync(
                equipment => equipment.AssetCode == assetCode,
                cancellationToken))
        {
            return BadRequest(new { message = "Mã tài sản đã tồn tại." });
        }
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

        if (dto.LocationNodeId.HasValue
            && !await _context.LocationNodes.AnyAsync(
                location => location.Id == dto.LocationNodeId && location.IsActive,
                cancellationToken))
        {
            return BadRequest(new { message = "Vị trí không tồn tại hoặc đã ngừng sử dụng." });
        }
        if (!dto.LocationNodeId.HasValue)
        {
            return BadRequest(new { message = "Tài sản mới phải chọn vị trí từ cây vị trí." });
        }

        var equipment = new Equipment
        {
            AssetCode = assetCode,
            Name = dto.Name.Trim(),
            Model = dto.Model.Trim(),
            Serial = serial,
            SerialName = dto.SerialName.Trim(),
            DeviceType = dto.DeviceType.Trim(),
            MacAddress = dto.MacAddress.Trim(),
            Imei = dto.Imei.Trim(),
            FirmwareVersion = dto.FirmwareVersion.Trim(),
            Manufacturer = dto.Manufacturer.Trim(),
            Supplier = dto.Supplier.Trim(),
            FundingSource = dto.FundingSource.Trim(),
            PurchaseValue = dto.PurchaseValue,
            Notes = dto.Notes.Trim(),
            Location = dto.Location.Trim(),
            LocationNodeId = dto.LocationNodeId,
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
            await _fileStorage.DeleteAsync(storedPath, CancellationToken.None);
            throw;
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            "Equipment",
            equipment.Id,
            new { equipment.Name, equipment.Serial },
            cancellationToken);

        return Ok(ToManagerDto(equipment));
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

        if (existing.Status == EquipmentStatuses.BorrowPending
            || await HasLockedBorrowRequestAsync(id, cancellationToken))
        {
            return Conflict(new
            {
                message = "Không thể sửa hoặc điều chuyển thiết bị khi đang có phiếu mượn chờ xử lý/bàn giao."
            });
        }

        if (!HasRequiredEquipmentFields(dto))
        {
            return BadRequest(new { message = "Tên, model, số seri và vị trí là bắt buộc." });
        }

        if (dto.DecisionFile is not null)
        {
            var fileValidationMessage = await ValidateDecisionFileAsync(dto.DecisionFile, cancellationToken);
            if (fileValidationMessage is not null)
            {
                return BadRequest(new { message = fileValidationMessage });
            }
        }

        var serial = dto.Serial.Trim();
        var assetCode = string.IsNullOrWhiteSpace(dto.AssetCode)
            ? (string.IsNullOrWhiteSpace(existing.AssetCode) ? CreateAssetCode() : existing.AssetCode)
            : dto.AssetCode.Trim();
        if (await _context.Equipments.AnyAsync(
                equipment => equipment.Id != id && equipment.AssetCode == assetCode,
                cancellationToken))
        {
            return BadRequest(new { message = "Mã tài sản đã tồn tại." });
        }
        if (await _context.Equipments.AnyAsync(
                equipment => equipment.Id != id && equipment.Serial == serial,
                cancellationToken))
        {
            return BadRequest(new { message = "Số seri đã tồn tại." });
        }

        dto.Status = EquipmentStatuses.Normalize(dto.Status);
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
            record => record.EquipmentId == id && record.Status == MaintenanceStatuses.InProgress,
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

        if (dto.LocationNodeId.HasValue
            && !await _context.LocationNodes.AnyAsync(
                location => location.Id == dto.LocationNodeId && location.IsActive,
                cancellationToken))
        {
            return BadRequest(new { message = "Vị trí không tồn tại hoặc đã ngừng sử dụng." });
        }
        if (!dto.LocationNodeId.HasValue)
        {
            return BadRequest(new { message = "Tài sản phải chọn vị trí từ cây vị trí." });
        }

        var before = SnapshotEquipment(existing);
        var previousLocationNodeId = existing.LocationNodeId;
        var previousLocationName = existing.Location;
        var locationChanged = existing.LocationNodeId != dto.LocationNodeId
            || !string.Equals(existing.Location.Trim(), dto.Location.Trim(), StringComparison.OrdinalIgnoreCase);
        if (locationChanged && string.IsNullOrWhiteSpace(dto.LocationChangeReason))
        {
            return BadRequest(new { message = "Khi điều chuyển tài sản phải nhập lý do." });
        }

        existing.AssetCode = assetCode;
        existing.Name = dto.Name.Trim();
        existing.Model = dto.Model.Trim();
        existing.Serial = serial;
        existing.SerialName = dto.SerialName.Trim();
        existing.DeviceType = dto.DeviceType.Trim();
        existing.MacAddress = dto.MacAddress.Trim();
        existing.Imei = dto.Imei.Trim();
        existing.FirmwareVersion = dto.FirmwareVersion.Trim();
        existing.Manufacturer = dto.Manufacturer.Trim();
        existing.Supplier = dto.Supplier.Trim();
        existing.FundingSource = dto.FundingSource.Trim();
        existing.PurchaseValue = dto.PurchaseValue;
        existing.Notes = dto.Notes.Trim();
        existing.Location = dto.Location.Trim();
        existing.LocationNodeId = dto.LocationNodeId;
        existing.ResponsiblePerson = dto.ResponsiblePerson.Trim();
        existing.EntryDate = dto.EntryDate;
        existing.WarrantyExpiry = dto.WarrantyExpiry;
        existing.InvoiceNumber = dto.InvoiceNumber.Trim();
        existing.Status = dto.Status;
        existing.AssetCategoryId = dto.AssetCategoryId;

        if (locationChanged)
        {
            var locationIds = new[] { previousLocationNodeId, dto.LocationNodeId }
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .ToArray();
            var locationNames = await _context.LocationNodes.AsNoTracking()
                .Where(location => locationIds.Contains(location.Id))
                .ToDictionaryAsync(location => location.Id, location => location.Name, cancellationToken);
            var fromLocationName = previousLocationNodeId.HasValue && locationNames.TryGetValue(previousLocationNodeId.Value, out var oldName)
                ? oldName
                : previousLocationName.Trim();
            var toLocationName = dto.LocationNodeId.HasValue && locationNames.TryGetValue(dto.LocationNodeId.Value, out var newName)
                ? newName
                : dto.Location.Trim();
            _context.EquipmentLocationHistories.Add(new EquipmentLocationHistory
            {
                EquipmentId = existing.Id,
                FromLocationNodeId = previousLocationNodeId,
                ToLocationNodeId = dto.LocationNodeId,
                FromLocationName = fromLocationName,
                ToLocationName = toLocationName,
                Reason = dto.LocationChangeReason.Trim(),
                ChangedByUserId = GetCurrentUserId(),
                ChangedAt = DateTime.UtcNow
            });
        }

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
                await _fileStorage.DeleteAsync(newPath, CancellationToken.None);
            }
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousPath)
            && !string.Equals(previousPath, newPath, StringComparison.Ordinal))
        {
            await _fileStorage.DeleteAsync(previousPath, cancellationToken);
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

    [HttpPost("{id:int}/inventory")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> InventoryEquipment(
        int id,
        CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (equipment is null)
        {
            return NotFound(new { message = "Không tìm thấy tài sản." });
        }

        equipment.LastInventoryAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Inventory",
            "Equipment",
            id,
            new { equipment.AssetCode, equipment.QrToken, equipment.Status },
            cancellationToken);
        return Ok(new { message = "Đã ghi nhận kiểm kê tài sản.", equipment.LastInventoryAt });
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

        var extension = Path.GetExtension(equipment.DecisionFilePath);
        var contentType = ContentTypes.GetValueOrDefault(extension, "application/octet-stream");
        var stream = await _fileStorage.OpenReadAsync(equipment.DecisionFilePath, cancellationToken);
        return stream is null
            ? NotFound("Không tìm thấy file quyết định.")
            : File(stream, contentType, equipment.DecisionFileName, enableRangeProcessing: true);
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

        if (equipment.Status == EquipmentStatuses.BorrowPending
            || await HasLockedBorrowRequestAsync(id, cancellationToken))
        {
            return Conflict(new
            {
                message = "Không thể xóa thiết bị khi đang có phiếu mượn chờ xử lý/bàn giao."
            });
        }

        var hasHistory = await _context.BorrowRecords.AnyAsync(
                record => record.EquipmentId == id
                    || record.Details.Any(detail => detail.EquipmentId == id),
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
        await _fileStorage.DeleteAsync(filePath ?? string.Empty, cancellationToken);

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
        var maxBytes = _configuration.GetValue(
            "Uploads:MaxDecisionFileBytes",
            10 * 1024 * 1024L);
        var stored = await _fileStorage.SaveAsync(
            file,
            "equipment-decisions",
            AllowedExtensions,
            maxBytes,
            cancellationToken);
        equipment.DecisionFileName = stored.OriginalFileName;
        equipment.DecisionFilePath = stored.StoredPath;
        equipment.DecisionUploadedAt = DateTime.UtcNow;
        return stored.StoredPath;
    }

    private async Task<string?> ValidateDecisionFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var maxBytes = _configuration.GetValue(
            "Uploads:MaxDecisionFileBytes",
            10 * 1024 * 1024L);
        if (file.Length <= 0 || file.Length > maxBytes)
        {
            return $"File quyết định rỗng hoặc vượt quá {maxBytes / (1024 * 1024)} MB.";
        }

        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return "Định dạng file quyết định không được hỗ trợ.";
        }
        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, ContentTypes[extension], StringComparison.OrdinalIgnoreCase))
        {
            return "MIME type không khớp với phần mở rộng file.";
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header, cancellationToken);
        var validSignature = extension switch
        {
            ".pdf" => read >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            ".png" => read >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".doc" => read >= 4 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0,
            ".docx" => read >= 2 && header[0] == 0x50 && header[1] == 0x4B,
            _ => false
        };
        return validSignature ? null : "Nội dung file không khớp với phần mở rộng.";
    }

    private static bool HasRequiredEquipmentFields(EquipmentFormDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Name)
            && !string.IsNullOrWhiteSpace(dto.Model)
            && !string.IsNullOrWhiteSpace(dto.Serial)
            && !string.IsNullOrWhiteSpace(dto.Location);
    }

    private static object SnapshotEquipment(Equipment equipment)
    {
        return new
        {
            equipment.AssetCode,
            equipment.QrToken,
            equipment.Name,
            equipment.Model,
            equipment.Serial,
            equipment.SerialName,
            equipment.DeviceType,
            equipment.MacAddress,
            equipment.Imei,
            equipment.FirmwareVersion,
            equipment.Manufacturer,
            equipment.Supplier,
            equipment.FundingSource,
            equipment.PurchaseValue,
            equipment.Notes,
            equipment.Location,
            equipment.LocationNodeId,
            equipment.ResponsiblePerson,
            equipment.EntryDate,
            equipment.WarrantyExpiry,
            equipment.InvoiceNumber,
            equipment.Status,
            equipment.AssetCategoryId,
            equipment.DecisionFileName
        };
    }

    private static BorrowerEquipmentDto ToBorrowerDto(Equipment equipment)
    {
        return new BorrowerEquipmentDto
        {
            Id = equipment.Id,
            AssetCode = equipment.AssetCode,
            Name = equipment.Name,
            Model = equipment.Model,
            Serial = equipment.Serial,
            SerialName = equipment.SerialName,
            DeviceType = equipment.DeviceType,
            Manufacturer = equipment.Manufacturer,
            ImagePath = equipment.ImagePath,
            Location = equipment.Location,
            LocationNodeId = equipment.LocationNodeId,
            LocationName = equipment.LocationNode?.Name ?? equipment.Location,
            Status = equipment.Status,
            AssetCategoryId = equipment.AssetCategoryId,
            CategoryName = equipment.AssetCategory?.Name
        };
    }

    private static ManagerEquipmentDto ToManagerDto(Equipment equipment)
    {
        return new ManagerEquipmentDto
        {
            Id = equipment.Id,
            AssetCode = equipment.AssetCode,
            QrToken = equipment.QrToken,
            Name = equipment.Name,
            Model = equipment.Model,
            Serial = equipment.Serial,
            SerialName = equipment.SerialName,
            DeviceType = equipment.DeviceType,
            MacAddress = equipment.MacAddress,
            Imei = equipment.Imei,
            FirmwareVersion = equipment.FirmwareVersion,
            Manufacturer = equipment.Manufacturer,
            Supplier = equipment.Supplier,
            FundingSource = equipment.FundingSource,
            PurchaseValue = equipment.PurchaseValue,
            ImagePath = equipment.ImagePath,
            LastInventoryAt = equipment.LastInventoryAt,
            Notes = equipment.Notes,
            Location = equipment.Location,
            LocationNodeId = equipment.LocationNodeId,
            LocationName = equipment.LocationNode?.Name ?? equipment.Location,
            ResponsiblePerson = equipment.ResponsiblePerson,
            DecisionFileName = equipment.DecisionFileName,
            HasDecisionFile = !string.IsNullOrEmpty(equipment.DecisionFilePath),
            EntryDate = equipment.EntryDate,
            WarrantyExpiry = equipment.WarrantyExpiry,
            InvoiceNumber = equipment.InvoiceNumber,
            Status = equipment.Status,
            BorrowCount = equipment.BorrowCount,
            AssetCategoryId = equipment.AssetCategoryId,
            CategoryName = equipment.AssetCategory?.Name,
            CreatedAt = equipment.CreatedAt
        };
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> HasLockedBorrowRequestAsync(
        int equipmentId,
        CancellationToken cancellationToken)
        => _context.BorrowRecords
            .AsNoTracking()
            .AnyAsync(record =>
                (record.EquipmentId == equipmentId
                    || record.Details.Any(detail => detail.EquipmentId == equipmentId))
                && BorrowLockRules.EquipmentLockedBorrowStatuses.Contains(record.Status),
                cancellationToken);

    [HttpGet("{id:int}/location-history")]
    public async Task<IActionResult> GetLocationHistory(int id, CancellationToken cancellationToken)
    {
        var exists = await _context.Equipments.AnyAsync(item => item.Id == id, cancellationToken);
        if (!exists) return NotFound(new { message = "Không tìm thấy tài sản." });
        var history = await _context.EquipmentLocationHistories.AsNoTracking()
            .Include(item => item.ChangedByUser)
            .Where(item => item.EquipmentId == id)
            .OrderByDescending(item => item.ChangedAt)
            .Select(item => new
            {
                item.Id,
                fromLocation = string.IsNullOrWhiteSpace(item.FromLocationName) ? "Chưa xác định" : item.FromLocationName,
                toLocation = string.IsNullOrWhiteSpace(item.ToLocationName) ? "Chưa xác định" : item.ToLocationName,
                item.Reason,
                item.ChangedAt,
                changedBy = item.ChangedByUser!.Username
            })
            .ToListAsync(cancellationToken);
        return Ok(history);
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var column = worksheet.Dimension!.Start.Column; column <= worksheet.Dimension.End.Column; column++)
        {
            var header = NormalizeImportHeader(worksheet.Cells[1, column].Text);
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header)) map[header] = column;
        }
        return map;
    }

    private static ImportEquipmentRowDto ReadImportRow(
        ExcelWorksheet worksheet,
        IReadOnlyDictionary<string, int> headers,
        int rowNumber)
    {
        var entryDate = ParseImportDate(GetImportCell(worksheet, headers, rowNumber, "Ngày nhập", "EntryDate"));
        var warrantyExpiry = ParseImportDate(GetImportCell(worksheet, headers, rowNumber, "Hạn bảo hành", "WarrantyExpiry"));
        return new ImportEquipmentRowDto
        {
            AssetCode = GetImportCell(worksheet, headers, rowNumber, "Mã tài sản", "AssetCode"),
            Name = GetImportCell(worksheet, headers, rowNumber, "Tên thiết bị", "Tên tài sản", "Name"),
            Model = GetImportCell(worksheet, headers, rowNumber, "Model"),
            Serial = GetImportCell(worksheet, headers, rowNumber, "Số seri", "Serial"),
            SerialName = GetImportCell(worksheet, headers, rowNumber, "Tên seri", "SerialName"),
            Location = GetImportCell(worksheet, headers, rowNumber, "Vị trí", "Location"),
            LocationNodeId = null,
            ResponsiblePerson = GetImportCell(worksheet, headers, rowNumber, "Người chịu trách nhiệm", "ResponsiblePerson"),
            EntryDate = entryDate,
            WarrantyExpiry = warrantyExpiry,
            InvoiceNumber = GetImportCell(worksheet, headers, rowNumber, "Số hóa đơn", "InvoiceNumber"),
            Notes = GetImportCell(worksheet, headers, rowNumber, "Ghi chú", "Notes")
        };
    }

    private static string GetImportCell(
        ExcelWorksheet worksheet,
        IReadOnlyDictionary<string, int> headers,
        int rowNumber,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(NormalizeImportHeader(name), out var column))
            {
                return worksheet.Cells[rowNumber, column].Text.Trim();
            }
        }
        return string.Empty;
    }

    private static DateTime? ParseImportDate(string value)
    {
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    private static int? ResolveImportLocation(string value, IReadOnlyCollection<LocationNode> locations)
    {
        var normalized = NormalizeImportHeader(value);
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return locations.FirstOrDefault(location => NormalizeImportHeader(location.Code) == normalized)?.Id
            ?? locations.FirstOrDefault(location => NormalizeImportHeader(location.Name) == normalized)?.Id;
    }

    private static string NormalizeImportHeader(string value)
    {
        return string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    private static string CreateAssetCode()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"IOT-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
    }
}
