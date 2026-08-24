using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;

    public InventoryController(
        AppDbContext context,
        IAuditService auditService,
        IFileStorage fileStorage,
        IConfiguration configuration)
    {
        _context = context;
        _auditService = auditService;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    public sealed class CreateInventoryDto
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        public int? AssetCategoryId { get; set; }
    }

    public sealed class ScanInventoryDto
    {
        [Required, MaxLength(128)]
        public string QrToken { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        public string Status { get; set; } = InventoryItemStatuses.Found;
        [MaxLength(2000)]
        public string Note { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await _context.InventorySessions
            .AsNoTracking()
            .Include(session => session.Items)
            .OrderByDescending(session => session.StartedAt)
            .ToListAsync(cancellationToken);

        return Ok(sessions.Select(session => new
        {
            session.Id,
            session.Code,
            session.Name,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            total = session.Items.Count,
            found = session.Items.Count(item => item.Status == InventoryItemStatuses.Found),
            wrongLocation = session.Items.Count(item => item.Status == InventoryItemStatuses.WrongLocation),
            damaged = session.Items.Count(item => item.Status == InventoryItemStatuses.Damaged),
            missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing),
            pending = session.Items.Count(item => item.Status == InventoryItemStatuses.Pending)
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetSession(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .AsNoTracking()
            .Include(item => item.LocationNode)
            .Include(item => item.AssetCategory)
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .Include(item => item.Items)
                .ThenInclude(item => item.InventoryItemEvidence)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }

        return Ok(new
        {
            session.Id,
            session.Code,
            session.Name,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            locationName = session.LocationNode?.Name,
            categoryName = session.AssetCategory?.Name,
            items = session.Items.OrderBy(item => item.Equipment!.Name).Select(item => new
            {
                item.Id,
                item.EquipmentId,
                assetCode = item.Equipment!.AssetCode,
                qrToken = item.Equipment.QrToken,
                equipmentName = item.Equipment.Name,
                serial = item.Equipment.Serial,
                expectedLocation = item.ExpectedLocationName,
                item.Status,
                item.ScannedAt,
                item.Note,
                evidence = item.InventoryItemEvidence.OrderByDescending(evidence => evidence.UploadedAt).Select(evidence => new
                {
                    evidence.Id,
                    evidence.EvidenceType,
                    evidence.OriginalFileName,
                    evidence.ContentType,
                    evidence.FileSize,
                    evidence.UploadedAt
                })
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateSession(
        [FromBody] CreateInventoryDto dto,
        CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên đợt kiểm kê là bắt buộc." });
        }

        if (dto.LocationNodeId.HasValue && !await _context.LocationNodes.AnyAsync(
                node => node.Id == dto.LocationNodeId.Value && node.IsActive,
                cancellationToken))
        {
            return BadRequest(new { message = "Vị trí kiểm kê không tồn tại hoặc đã ngừng sử dụng." });
        }
        if (dto.AssetCategoryId.HasValue && !await _context.AssetCategories.AnyAsync(
                category => category.Id == dto.AssetCategoryId.Value,
                cancellationToken))
        {
            return BadRequest(new { message = "Danh mục kiểm kê không tồn tại." });
        }

        var equipmentQuery = _context.Equipments.AsNoTracking().AsQueryable();
        if (dto.LocationNodeId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(item => item.LocationNodeId == dto.LocationNodeId.Value);
        }
        if (dto.AssetCategoryId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(item => item.AssetCategoryId == dto.AssetCategoryId.Value);
        }

        var equipment = await equipmentQuery
            .Select(item => new
            {
                item.Id,
                item.LocationNodeId,
                locationName = item.LocationNode!.Name
            })
            .ToListAsync(cancellationToken);
        if (equipment.Count == 0)
        {
            return BadRequest(new { message = "Phạm vi kiểm kê không có tài sản định danh." });
        }

        var session = new InventorySession
        {
            Code = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28],
            Name = dto.Name,
            LocationNodeId = dto.LocationNodeId,
            AssetCategoryId = dto.AssetCategoryId,
            CreatedByUserId = GetCurrentUserId(),
            Items = equipment.Select(item => new InventoryItem
            {
                EquipmentId = item.Id,
                ExpectedLocationNodeId = item.LocationNodeId,
                ExpectedLocationName = item.locationName ?? string.Empty,
                Status = InventoryItemStatuses.Pending
            }).ToList()
        };
        _context.InventorySessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(InventorySession),
            session.Id,
            new { session.Code, ItemCount = equipment.Count },
            cancellationToken);
        return Ok(new { session.Id, session.Code, message = "Đã tạo đợt kiểm kê." });
    }

    [HttpPost("{id:int}/scan")]
    public async Task<IActionResult> Scan(
        int id,
        [FromBody] ScanInventoryDto dto,
        CancellationToken cancellationToken)
    {
        var token = dto.QrToken.Trim();
        dto.Note = dto.Note.Trim();
        if (string.IsNullOrWhiteSpace(token) || dto.Note.Length > 2000)
        {
            return BadRequest(new { message = "QR token và ghi chú không hợp lệ." });
        }

        var requestedStatus = string.IsNullOrWhiteSpace(dto.Status)
            ? InventoryItemStatuses.Found
            : dto.Status.Trim();
        if (requestedStatus is not (InventoryItemStatuses.Found or InventoryItemStatuses.Damaged))
        {
            return BadRequest(new { message = "Kết quả kiểm kê không hợp lệ." });
        }

        var session = await _context.InventorySessions
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }
        if (session.Status != InventoryStatuses.Open)
        {
            return Conflict(new { message = "Đợt kiểm kê đã kết thúc." });
        }

        var inventoryItem = session.Items.SingleOrDefault(item => item.Equipment!.QrToken == token);
        if (inventoryItem is null)
        {
            return BadRequest(new { message = "QR không thuộc phạm vi của đợt kiểm kê." });
        }

        inventoryItem.Status = requestedStatus;
        if (dto.LocationNodeId.HasValue && dto.LocationNodeId.Value != inventoryItem.ExpectedLocationNodeId)
        {
            inventoryItem.Status = InventoryItemStatuses.WrongLocation;
        }
        inventoryItem.ScannedAt = DateTime.UtcNow;
        inventoryItem.ScannedByUserId = GetCurrentUserId();
        inventoryItem.Note = dto.Note;
        inventoryItem.Equipment!.LastInventoryAt = inventoryItem.ScannedAt;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Scan",
            nameof(InventoryItem),
            inventoryItem.Id,
            new { sessionId = id, inventoryItem.EquipmentId, inventoryItem.Status },
            cancellationToken);
        return Ok(new { message = "Đã ghi nhận kết quả quét QR.", inventoryItem.Status });
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }
        if (session.Status != InventoryStatuses.Open)
        {
            return Conflict(new { message = "Đợt kiểm kê đã kết thúc." });
        }

        foreach (var item in session.Items.Where(item => item.Status == InventoryItemStatuses.Pending))
        {
            item.Status = InventoryItemStatuses.Missing;
        }
        session.Status = InventoryStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Complete",
            nameof(InventorySession),
            id,
            new { Missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing) },
            cancellationToken);
        return Ok(new { message = "Đã kết thúc đợt kiểm kê." });
    }

    public sealed class UploadEvidenceDto
    {
        [Required] public IFormFile? File { get; set; }
        [Required, MaxLength(50)] public string EvidenceType { get; set; } = "PHOTO";
    }

    [HttpPost("{sessionId:int}/items/{itemId:int}/evidence")]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> UploadEvidence(
        int sessionId,
        int itemId,
        [FromForm] UploadEvidenceDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.File is null) return BadRequest(new { message = "Vui lòng chọn file minh chứng." });
        dto.EvidenceType = dto.EvidenceType.Trim().ToUpperInvariant();
        if (dto.EvidenceType is not ("PHOTO" or "DOCUMENT"))
            return BadRequest(new { message = "Loại minh chứng kiểm kê không hợp lệ." });

        var item = await _context.InventoryItems
            .Include(value => value.InventorySession)
            .SingleOrDefaultAsync(value => value.Id == itemId
                && value.InventorySessionId == sessionId, cancellationToken);
        if (item is null) return NotFound(new { message = "Không tìm thấy tài sản trong đợt kiểm kê." });

        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                dto.File,
                "inventories",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" },
                _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L),
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var evidence = new InventoryEvidence
        {
            InventoryItemId = item.Id,
            EvidenceType = dto.EvidenceType,
            OriginalFileName = stored.OriginalFileName,
            StoredPath = stored.StoredPath,
            ContentType = stored.ContentType,
            FileSize = stored.Length,
            UploadedByUserId = GetCurrentUserId()
        };
        _context.InventoryEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UploadEvidence", nameof(InventoryItem), item.Id,
            new { evidence.Id, evidence.EvidenceType }, cancellationToken);
        return Ok(new { evidence.Id, evidence.OriginalFileName, message = "Đã lưu minh chứng kiểm kê." });
    }

    [HttpGet("{sessionId:int}/items/{itemId:int}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(
        int sessionId,
        int itemId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.InventoryEvidence.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == evidenceId
                && value.InventoryItemId == itemId
                && value.InventoryItem!.InventorySessionId == sessionId,
                cancellationToken);
        if (evidence is null || !_fileStorage.IsSafePath(evidence.StoredPath)) return NotFound();
        var stream = new FileStream(evidence.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpGet("{id:int}/export.xlsx")]
    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var session = await LoadReportSession(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("ChenhLech");
        var headers = new[] { "Mã tài sản", "Tên tài sản", "Số seri", "Vị trí dự kiến", "Trạng thái", "Thời gian quét", "Ghi chú" };
        for (var index = 0; index < headers.Length; index++)
        {
            sheet.Cells[1, index + 1].Value = headers[index];
            sheet.Cells[1, index + 1].Style.Font.Bold = true;
        }
        for (var index = 0; index < session.Items.Count; index++)
        {
            var item = session.Items.ToList()[index];
            var row = index + 2;
            WriteExcelText(sheet, row, 1, item.Equipment?.AssetCode);
            WriteExcelText(sheet, row, 2, item.Equipment?.Name);
            WriteExcelText(sheet, row, 3, item.Equipment?.Serial);
            WriteExcelText(sheet, row, 4, item.ExpectedLocationName);
            WriteExcelText(sheet, row, 5, InventoryStatusLabel(item.Status));
            WriteExcelText(sheet, row, 6, item.ScannedAt?.ToString("dd/MM/yyyy HH:mm"));
            WriteExcelText(sheet, row, 7, item.Note);
        }
        sheet.Cells[sheet.Dimension?.Address ?? "A1"].AutoFitColumns();
        await using var stream = new MemoryStream();
        await package.SaveAsAsync(stream, cancellationToken);
        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KiemKe_{session.Code}.xlsx");
    }

    [HttpGet("{id:int}/export.pdf")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var session = await LoadReportSession(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        QuestPDF.Settings.License = LicenseType.Community;
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(8));
            page.Header().Column(column =>
            {
                column.Item().Text("BÁO CÁO CHÊNH LỆCH KIỂM KÊ TÀI SẢN").Bold().FontSize(16);
                column.Item().Text($"{session.Code} — {session.Name} — {InventoryStatusLabel(session.Status)}");
            });
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28); columns.RelativeColumn(1.4f); columns.RelativeColumn(2);
                    columns.RelativeColumn(1.4f); columns.RelativeColumn(1.5f); columns.RelativeColumn(1.2f);
                });
                table.Header(header =>
                {
                    foreach (var title in new[] { "STT", "Mã tài sản", "Tên tài sản", "Vị trí dự kiến", "Kết quả", "Ghi chú" })
                        header.Cell().Element(HeaderCell).Text(title);
                });
                foreach (var (item, index) in session.Items.Select((value, index) => (value, index)))
                {
                    table.Cell().Element(BodyCell).Text((index + 1).ToString());
                    table.Cell().Element(BodyCell).Text(item.Equipment?.AssetCode ?? "");
                    table.Cell().Element(BodyCell).Text(item.Equipment?.Name ?? "");
                    table.Cell().Element(BodyCell).Text(item.ExpectedLocationName);
                    table.Cell().Element(BodyCell).Text(InventoryStatusLabel(item.Status));
                    table.Cell().Element(BodyCell).Text(item.Note);
                }
            });
            page.Footer().AlignCenter().Text(text => { text.Span("LabManagement — Trang "); text.CurrentPageNumber(); });
        }));
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;
        return File(stream, "application/pdf", $"KiemKe_{session.Code}.pdf");

        static IContainer HeaderCell(IContainer container) => container.Background(Colors.Blue.Darken2)
            .Padding(4).DefaultTextStyle(style => style.FontColor(Colors.White).Bold());
        static IContainer BodyCell(IContainer container) => container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    private async Task<InventorySession?> LoadReportSession(int id, CancellationToken cancellationToken)
        => await _context.InventorySessions.AsNoTracking()
            .Include(session => session.Items).ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    private static void WriteExcelText(ExcelWorksheet sheet, int row, int column, string? value)
    {
        var text = value ?? string.Empty;
        sheet.Cells[row, column].Value = text.Length > 0 && text[0] is '=' or '+' or '-' or '@'
            ? "'" + text
            : text;
    }

    private static string InventoryStatusLabel(string status) => status switch
    {
        InventoryStatuses.Open => "Đang kiểm kê",
        InventoryStatuses.Completed => "Đã kết thúc",
        InventoryItemStatuses.Found => "Đã tìm thấy",
        InventoryItemStatuses.WrongLocation => "Sai vị trí",
        InventoryItemStatuses.Damaged => "Hư hỏng",
        InventoryItemStatuses.Missing => "Thất lạc",
        InventoryItemStatuses.Pending => "Chưa kiểm kê",
        _ => status
    };

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
