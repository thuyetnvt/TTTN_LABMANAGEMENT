using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabManagementAPI.Controllers;

[Route("api/reports")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context) => _context = context;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        string? status,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeStatus(status, out status))
            return BadRequest(new { message = "Trạng thái tài sản không hợp lệ." });
        var equipmentQuery = FilterEquipment(from, to, categoryId, locationNodeId, status);
        var equipments = await equipmentQuery.AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .ToListAsync(cancellationToken);
        var equipmentIds = equipments.Select(equipment => equipment.Id).ToArray();
        var equipmentFilterApplied = from.HasValue
            || to.HasValue
            || categoryId.HasValue
            || locationNodeId.HasValue
            || !string.IsNullOrWhiteSpace(status);
        var now = DateTime.UtcNow;
        var fromUtc = ToUtcDateStart(from);
        var toUtcExclusive = ToUtcDateEndExclusive(to);
        var maintenanceQuery = _context.MaintenanceRecords.AsNoTracking();
        if (fromUtc.HasValue) maintenanceQuery = maintenanceQuery.Where(record => record.MaintenanceDate >= fromUtc.Value);
        if (toUtcExclusive.HasValue) maintenanceQuery = maintenanceQuery.Where(record => record.MaintenanceDate < toUtcExclusive.Value);
        if (equipmentFilterApplied) maintenanceQuery = maintenanceQuery.Where(record => equipmentIds.Contains(record.EquipmentId));
        var maintenance = await maintenanceQuery
            .Include(record => record.Equipment)
            .OrderByDescending(record => record.MaintenanceDate)
            .Take(200)
            .ToListAsync(cancellationToken);
        var maintenanceCost = maintenance.Sum(record => record.Cost);

        var borrowedQuery = _context.BorrowRequestDetails.AsNoTracking()
            .Include(detail => detail.BorrowRecord)
                .ThenInclude(record => record!.User)
            .Include(detail => detail.Equipment)
            .Where(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed);
        if (equipmentFilterApplied) borrowedQuery = borrowedQuery.Where(detail => equipmentIds.Contains(detail.EquipmentId));
        if (fromUtc.HasValue) borrowedQuery = borrowedQuery.Where(detail => detail.BorrowRecord!.BorrowDate >= fromUtc.Value);
        if (toUtcExclusive.HasValue) borrowedQuery = borrowedQuery.Where(detail => detail.BorrowRecord!.BorrowDate < toUtcExclusive.Value);
        var borrowed = await borrowedQuery
            .OrderBy(detail => detail.BorrowRecord!.ExpectedReturnDate)
            .Take(100)
            .Select(detail => new
            {
                id = detail.Id,
                borrowRecordId = detail.BorrowRecordId,
                user = detail.BorrowRecord!.User!.Username,
                equipment = detail.Equipment!.Name,
                serial = detail.Equipment.Serial,
                expectedReturnDate = detail.BorrowRecord.ExpectedReturnDate,
                overdue = detail.BorrowRecord.ExpectedReturnDate < now
            })
            .ToListAsync(cancellationToken);

        var consumablesQuery = _context.Consumables.AsNoTracking();
        if (categoryId.HasValue) consumablesQuery = consumablesQuery.Where(item => item.AssetCategoryId == categoryId.Value);
        var consumables = await consumablesQuery
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Code, item.Name, item.Unit, item.Quantity, item.MinQuantity })
            .ToListAsync(cancellationToken);
        var lowStock = consumables
            .Where(item => item.Quantity <= item.MinQuantity)
            .OrderBy(item => item.Quantity - item.MinQuantity)
            .ToList();
        var warrantySoon = equipments
            .Where(equipment => equipment.WarrantyExpiry.HasValue
                && equipment.WarrantyExpiry.Value >= now
                && equipment.WarrantyExpiry.Value <= now.AddDays(30))
            .OrderBy(equipment => equipment.WarrantyExpiry)
            .Select(equipment => new
            {
                equipment.Id,
                equipment.Name,
                equipment.Serial,
                equipment.WarrantyExpiry
            })
            .ToList();

        return Ok(new
        {
            filters = new { from, to, categoryId, locationNodeId, status },
            totals = new
            {
                assets = equipments.Count,
                borrowed = borrowed.Count,
                overdue = borrowed.Count(item => item.overdue),
                broken = equipments.Count(item => item.Status == EquipmentStatuses.Broken),
                underWarranty = equipments.Count(item => item.Status == EquipmentStatuses.Warranty),
                maintenanceCost
            },
            byStatus = equipments.GroupBy(item => item.Status)
                .Select(group => new { status = group.Key, count = group.Count() })
                .OrderByDescending(item => item.count),
            byCategory = equipments.GroupBy(item => item.AssetCategory?.Name ?? "Chưa phân loại")
                .Select(group => new { category = group.Key, count = group.Count() })
                .OrderByDescending(item => item.count),
            byLocation = equipments.GroupBy(item => item.LocationNode?.Name ?? item.Location)
                .Select(group => new { location = group.Key, count = group.Count() })
                .OrderByDescending(item => item.count),
            borrowed,
            lowStock,
            warrantySoon,
            maintenance = maintenance.Select(item => new
            {
                item.Id,
                equipment = item.Equipment?.Name ?? string.Empty,
                item.MaintenanceDate,
                item.PerformedBy,
                item.Cost,
                item.Status
            }),
            consumables
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        string? status,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeStatus(status, out status))
            return BadRequest(new { message = "Trạng thái tài sản không hợp lệ." });
        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        var equipments = await FilterEquipment(from, to, categoryId, locationNodeId, status)
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .OrderBy(equipment => equipment.Name)
            .ToListAsync(cancellationToken);
        var equipmentIds = equipments.Select(equipment => equipment.Id).ToArray();
        var fromUtc = ToUtcDateStart(from);
        var toUtcExclusive = ToUtcDateEndExclusive(to);
        var equipmentFilterApplied = from.HasValue
            || to.HasValue
            || categoryId.HasValue
            || locationNodeId.HasValue
            || !string.IsNullOrWhiteSpace(status);
        var maintenance = await _context.MaintenanceRecords.AsNoTracking()
            .Include(record => record.Equipment)
            .Where(record => (!fromUtc.HasValue || record.MaintenanceDate >= fromUtc.Value)
                && (!toUtcExclusive.HasValue || record.MaintenanceDate < toUtcExclusive.Value))
            .Where(record => !equipmentFilterApplied || equipmentIds.Contains(record.EquipmentId))
            .OrderByDescending(record => record.MaintenanceDate)
            .Take(2000)
            .ToListAsync(cancellationToken);
        var borrowed = await _context.BorrowRequestDetails.AsNoTracking()
            .Include(detail => detail.BorrowRecord).ThenInclude(record => record!.User)
            .Include(detail => detail.Equipment)
            .Where(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed)
            .Where(detail => !equipmentFilterApplied || equipmentIds.Contains(detail.EquipmentId))
            .Where(detail => !fromUtc.HasValue || detail.BorrowRecord!.BorrowDate >= fromUtc.Value)
            .Where(detail => !toUtcExclusive.HasValue || detail.BorrowRecord!.BorrowDate < toUtcExclusive.Value)
            .OrderBy(detail => detail.BorrowRecord!.ExpectedReturnDate)
            .ToListAsync(cancellationToken);
        var consumablesQuery = _context.Consumables.AsNoTracking();
        if (categoryId.HasValue) consumablesQuery = consumablesQuery.Where(item => item.AssetCategoryId == categoryId.Value);
        var consumables = await consumablesQuery
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        var assetsSheet = package.Workbook.Worksheets.Add("Tài sản");
        WriteHeaders(assetsSheet, ["Mã tài sản", "Tên", "Model", "Số seri", "Danh mục", "Vị trí", "Trạng thái", "Hạn bảo hành"]);
        for (var index = 0; index < equipments.Count; index++)
        {
            var item = equipments[index];
            var row = index + 2;
            WriteCell(assetsSheet, row, 1, item.AssetCode);
            WriteCell(assetsSheet, row, 2, item.Name);
            WriteCell(assetsSheet, row, 3, item.Model);
            WriteCell(assetsSheet, row, 4, item.Serial);
            WriteCell(assetsSheet, row, 5, item.AssetCategory?.Name);
            WriteCell(assetsSheet, row, 6, item.LocationNode?.Name ?? item.Location);
            WriteCell(assetsSheet, row, 7, item.Status);
            WriteCell(assetsSheet, row, 8, item.WarrantyExpiry?.ToString("dd/MM/yyyy"));
        }
        WriteNoDataRow(assetsSheet, equipments.Count, 8);

        var maintenanceSheet = package.Workbook.Worksheets.Add("Bảo trì");
        WriteHeaders(maintenanceSheet, ["Thiết bị", "Ngày", "Nội dung", "Người thực hiện", "Chi phí", "Trạng thái", "Kết quả"]);
        for (var index = 0; index < maintenance.Count; index++)
        {
            var item = maintenance[index];
            var row = index + 2;
            WriteCell(maintenanceSheet, row, 1, item.Equipment?.Name);
            WriteCell(maintenanceSheet, row, 2, item.MaintenanceDate.ToString("dd/MM/yyyy"));
            WriteCell(maintenanceSheet, row, 3, item.Description);
            WriteCell(maintenanceSheet, row, 4, item.PerformedBy);
            WriteCell(maintenanceSheet, row, 5, item.Cost);
            WriteCell(maintenanceSheet, row, 6, item.Status);
            WriteCell(maintenanceSheet, row, 7, item.Result);
        }
        WriteNoDataRow(maintenanceSheet, maintenance.Count, 7);

        var borrowedSheet = package.Workbook.Worksheets.Add("Đang mượn");
        WriteHeaders(borrowedSheet, ["Người mượn", "Thiết bị", "Số seri", "Ngày trả dự kiến", "Quá hạn"]);
        for (var index = 0; index < borrowed.Count; index++)
        {
            var item = borrowed[index];
            var row = index + 2;
            WriteCell(borrowedSheet, row, 1, item.BorrowRecord?.User?.Username ?? "Không xác định");
            WriteCell(borrowedSheet, row, 2, item.Equipment?.Name ?? "Không xác định");
            WriteCell(borrowedSheet, row, 3, item.Equipment?.Serial ?? string.Empty);
            WriteCell(borrowedSheet, row, 4, item.BorrowRecord?.ExpectedReturnDate.ToString("dd/MM/yyyy") ?? string.Empty);
            WriteCell(borrowedSheet, row, 5, item.BorrowRecord is not null && item.BorrowRecord.ExpectedReturnDate < DateTime.UtcNow ? "Có" : "Không");
        }
        WriteNoDataRow(borrowedSheet, borrowed.Count, 5);

        var consumableSheet = package.Workbook.Worksheets.Add("Vật tư");
        WriteHeaders(consumableSheet, ["Tên vật tư", "Đơn vị", "Số lượng", "Mức tối thiểu", "Trạng thái"]);
        for (var index = 0; index < consumables.Count; index++)
        {
            var item = consumables[index];
            var row = index + 2;
            WriteCell(consumableSheet, row, 1, item.Name);
            WriteCell(consumableSheet, row, 2, item.Unit);
            WriteCell(consumableSheet, row, 3, item.Quantity);
            WriteCell(consumableSheet, row, 4, item.MinQuantity);
            WriteCell(consumableSheet, row, 5, item.Quantity <= item.MinQuantity ? "Sắp hết" : "Đủ");
        }
        WriteNoDataRow(consumableSheet, consumables.Count, 5);

        foreach (var worksheet in package.Workbook.Worksheets)
        {
            worksheet.Cells[worksheet.Dimension?.Address ?? "A1"].AutoFitColumns();
        }
        var bytes = await package.GetAsByteArrayAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Báo cáo tài sản Phòng Lab IoT_{VietnamNow():yyyyMMddHHmm}.xlsx");
    }

    [HttpGet("export.pdf")]
    public async Task<IActionResult> ExportPdf(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        string? status,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeStatus(status, out status))
            return BadRequest(new { message = "Trạng thái tài sản không hợp lệ." });
        QuestPDF.Settings.License = LicenseType.Community;
        var equipments = await FilterEquipment(from, to, categoryId, locationNodeId, status)
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .OrderBy(equipment => equipment.Name)
            .ToListAsync(cancellationToken);
        var equipmentIds = equipments.Select(equipment => equipment.Id).ToArray();
        var fromUtc = ToUtcDateStart(from);
        var toUtcExclusive = ToUtcDateEndExclusive(to);
        var equipmentFilterApplied = from.HasValue
            || to.HasValue
            || categoryId.HasValue
            || locationNodeId.HasValue
            || !string.IsNullOrWhiteSpace(status);
        var maintenanceCost = await _context.MaintenanceRecords.AsNoTracking()
            .Where(record => (!fromUtc.HasValue || record.MaintenanceDate >= fromUtc.Value)
                && (!toUtcExclusive.HasValue || record.MaintenanceDate < toUtcExclusive.Value))
            .Where(record => !equipmentFilterApplied || equipmentIds.Contains(record.EquipmentId))
            .SumAsync(record => (decimal?)record.Cost, cancellationToken) ?? 0;
        var borrowedCount = await _context.BorrowRequestDetails.AsNoTracking()
            .CountAsync(detail => detail.BorrowRecord != null
                && detail.BorrowRecord.Status == BorrowStatuses.Borrowed
                && (!equipmentFilterApplied || equipmentIds.Contains(detail.EquipmentId))
                && (!fromUtc.HasValue || detail.BorrowRecord.BorrowDate >= fromUtc.Value)
                && (!toUtcExclusive.HasValue || detail.BorrowRecord.BorrowDate < toUtcExclusive.Value), cancellationToken);
        var overdueCount = await _context.BorrowRequestDetails.AsNoTracking()
            .CountAsync(detail => detail.BorrowRecord != null
                && detail.BorrowRecord.Status == BorrowStatuses.Borrowed
                && (!equipmentFilterApplied || equipmentIds.Contains(detail.EquipmentId))
                && (!fromUtc.HasValue || detail.BorrowRecord.BorrowDate >= fromUtc.Value)
                && (!toUtcExclusive.HasValue || detail.BorrowRecord.BorrowDate < toUtcExclusive.Value)
                && detail.BorrowRecord.ExpectedReturnDate < DateTime.UtcNow, cancellationToken);
        var consumablesQuery = _context.Consumables.AsNoTracking();
        if (categoryId.HasValue) consumablesQuery = consumablesQuery.Where(item => item.AssetCategoryId == categoryId.Value);
        var lowStockCount = await consumablesQuery
            .CountAsync(item => item.Quantity <= item.MinQuantity, cancellationToken);

        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Column(column =>
            {
                column.Item().Text("Báo cáo tài sản Phòng Lab IoT").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                var vietnamNow = VietnamNow();
                column.Item().Text($"Ngày xuất: {vietnamNow:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
            });
            page.Content().Column(column =>
            {
                column.Spacing(10);
                column.Item().Text($"Tổng tài sản: {equipments.Count}    |    Đang mượn: {borrowedCount}    |    Quá hạn: {overdueCount}").Bold();
                column.Item().Text($"Hỏng: {equipments.Count(item => item.Status == EquipmentStatuses.Broken)}    |    Bảo hành: {equipments.Count(item => item.Status == EquipmentStatuses.Warranty)}    |    Chi phí bảo trì: {maintenanceCost:N0} VNĐ");
                column.Item().Text($"Vật tư sắp hết: {lowStockCount}");
                column.Item().Text("Danh sách tài sản").Bold().FontSize(13);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("STT");
                        header.Cell().Element(HeaderCell).Text("Tên tài sản");
                        header.Cell().Element(HeaderCell).Text("Số seri");
                        header.Cell().Element(HeaderCell).Text("Vị trí");
                        header.Cell().Element(HeaderCell).Text("Trạng thái");
                    });
                    foreach (var (item, index) in equipments.Take(80).Select((item, index) => (item, index)))
                    {
                        table.Cell().Element(BodyCell).Text((index + 1).ToString());
                        table.Cell().Element(BodyCell).Text(PdfText(item.Name));
                        table.Cell().Element(BodyCell).Text(PdfText(item.Serial));
                        table.Cell().Element(BodyCell).Text(PdfText(item.LocationNode?.Name ?? item.Location));
                        table.Cell().Element(BodyCell).Text(PdfText(item.Status));
                    }
                    if (equipments.Count == 0)
                    {
                        table.Cell().Element(BodyCell).Text("Không có dữ liệu");
                        table.Cell().Element(BodyCell).Text(string.Empty);
                        table.Cell().Element(BodyCell).Text(string.Empty);
                        table.Cell().Element(BodyCell).Text(string.Empty);
                        table.Cell().Element(BodyCell).Text(string.Empty);
                    }
                });
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("LabManagement — Trang ");
                text.CurrentPageNumber();
            });
        }));
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", $"Báo cáo tài sản Phòng Lab IoT_{VietnamNow():yyyyMMddHHmm}.pdf");

        static QuestPDF.Infrastructure.IContainer HeaderCell(QuestPDF.Infrastructure.IContainer container)
            => container.Background(Colors.Blue.Darken2).Padding(4).DefaultTextStyle(style => style.FontColor(Colors.White).Bold());
        static QuestPDF.Infrastructure.IContainer BodyCell(QuestPDF.Infrastructure.IContainer container)
            => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    private IQueryable<Equipment> FilterEquipment(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        string? status)
    {
        var query = _context.Equipments.AsQueryable();
        var fromUtc = ToUtcDateStart(from);
        var toUtcExclusive = ToUtcDateEndExclusive(to);
        if (fromUtc.HasValue) query = query.Where(item => item.CreatedAt >= fromUtc.Value);
        if (toUtcExclusive.HasValue) query = query.Where(item => item.CreatedAt < toUtcExclusive.Value);
        if (categoryId.HasValue) query = query.Where(item => item.AssetCategoryId == categoryId.Value);
        if (locationNodeId.HasValue) query = query.Where(item => item.LocationNodeId == locationNodeId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }
        return query;
    }

    private static bool TryNormalizeStatus(string? value, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = null;
            return true;
        }

        normalized = EquipmentStatuses.LegacyMap.TryGetValue(value.Trim(), out var mappedStatus)
            ? mappedStatus
            : value.Trim().ToUpperInvariant();
        return EquipmentStatuses.All.Contains(normalized);
    }

    private static void WriteHeaders(ExcelWorksheet worksheet, string[] headers)
    {
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cells[1, index + 1].Value = SafeExcelText(headers[index]);
            worksheet.Cells[1, index + 1].Style.Font.Bold = true;
        }
    }

    private static void WriteCell(ExcelWorksheet worksheet, int row, int column, object? value)
    {
        worksheet.Cells[row, column].Value = value is string text ? SafeExcelText(text) : value;
    }

    private static void WriteNoDataRow(ExcelWorksheet worksheet, int itemCount, int columnCount)
    {
        if (itemCount > 0) return;
        worksheet.Cells[2, 1].Value = "Không có dữ liệu";
        worksheet.Cells[2, 1].Style.Font.Italic = true;
        if (columnCount > 1)
        {
            worksheet.Cells[2, 1, 2, columnCount].Merge = true;
        }
    }

    private static DateTime? ToUtcDateStart(DateTime? value)
    {
        if (!value.HasValue) return null;
        var vietnamDate = DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(vietnamDate, VietnamTimeZone());
    }

    private static DateTime? ToUtcDateEndExclusive(DateTime? value)
    {
        if (!value.HasValue) return null;
        var vietnamDate = DateTime.SpecifyKind(value.Value.Date.AddDays(1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(vietnamDate, VietnamTimeZone());
    }

    private static TimeZoneInfo VietnamTimeZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the identifier used by the other operating system.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the identifier used by the other operating system.
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Vietnam Standard Time",
            TimeSpan.FromHours(7),
            "Vietnam Standard Time",
            "Vietnam Standard Time");
    }

    private static DateTime VietnamNow() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone());

    private static string PdfText(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string SafeExcelText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
    }
}
