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
        CancellationToken cancellationToken)
    {
        var equipmentQuery = FilterEquipment(from, to, categoryId, locationNodeId);
        var equipments = await equipmentQuery.AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var maintenanceQuery = _context.MaintenanceRecords.AsNoTracking();
        if (from.HasValue) maintenanceQuery = maintenanceQuery.Where(record => record.MaintenanceDate >= from.Value);
        if (to.HasValue) maintenanceQuery = maintenanceQuery.Where(record => record.MaintenanceDate <= to.Value);
        var maintenanceCost = await maintenanceQuery.SumAsync(record => (decimal?)record.Cost, cancellationToken) ?? 0;

        var borrowed = await _context.BorrowRequestDetails.AsNoTracking()
            .Include(detail => detail.BorrowRecord)
                .ThenInclude(record => record!.User)
            .Include(detail => detail.Equipment)
            .Where(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed)
            .OrderBy(detail => detail.BorrowRecord!.ExpectedReturnDate)
            .Take(100)
            .Select(detail => new
            {
                id = detail.BorrowRecordId,
                user = detail.BorrowRecord!.User!.Username,
                equipment = detail.Equipment!.Name,
                serial = detail.Equipment.Serial,
                expectedReturnDate = detail.BorrowRecord.ExpectedReturnDate,
                overdue = detail.BorrowRecord.ExpectedReturnDate < now
            })
            .ToListAsync(cancellationToken);

        var lowStock = await _context.Consumables.AsNoTracking()
            .Where(item => item.Quantity <= item.MinQuantity)
            .OrderBy(item => item.Quantity - item.MinQuantity)
            .Select(item => new { item.Id, item.Name, item.Unit, item.Quantity, item.MinQuantity })
            .ToListAsync(cancellationToken);
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
            filters = new { from, to, categoryId, locationNodeId },
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
            warrantySoon
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        CancellationToken cancellationToken)
    {
        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        var equipments = await FilterEquipment(from, to, categoryId, locationNodeId)
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .OrderBy(equipment => equipment.Name)
            .ToListAsync(cancellationToken);
        var maintenance = await _context.MaintenanceRecords.AsNoTracking()
            .Include(record => record.Equipment)
            .Where(record => (!from.HasValue || record.MaintenanceDate >= from.Value)
                && (!to.HasValue || record.MaintenanceDate <= to.Value))
            .OrderByDescending(record => record.MaintenanceDate)
            .Take(2000)
            .ToListAsync(cancellationToken);
        var borrowed = await _context.BorrowRequestDetails.AsNoTracking()
            .Include(detail => detail.BorrowRecord).ThenInclude(record => record!.User)
            .Include(detail => detail.Equipment)
            .Where(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed)
            .OrderBy(detail => detail.BorrowRecord!.ExpectedReturnDate)
            .ToListAsync(cancellationToken);
        var consumables = await _context.Consumables.AsNoTracking()
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        var assetsSheet = package.Workbook.Worksheets.Add("TaiSan");
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

        var maintenanceSheet = package.Workbook.Worksheets.Add("BaoTri");
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

        var borrowedSheet = package.Workbook.Worksheets.Add("DangMuon");
        WriteHeaders(borrowedSheet, ["Người mượn", "Thiết bị", "Số seri", "Ngày trả dự kiến", "Quá hạn"]);
        for (var index = 0; index < borrowed.Count; index++)
        {
            var item = borrowed[index];
            var row = index + 2;
            WriteCell(borrowedSheet, row, 1, item.BorrowRecord?.User?.Username);
            WriteCell(borrowedSheet, row, 2, item.Equipment?.Name);
            WriteCell(borrowedSheet, row, 3, item.Equipment?.Serial);
            WriteCell(borrowedSheet, row, 4, item.BorrowRecord?.ExpectedReturnDate.ToString("dd/MM/yyyy"));
            WriteCell(borrowedSheet, row, 5, item.BorrowRecord?.ExpectedReturnDate < DateTime.UtcNow ? "Có" : "Không");
        }

        var consumableSheet = package.Workbook.Worksheets.Add("VatTu");
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

        foreach (var worksheet in package.Workbook.Worksheets)
        {
            worksheet.Cells[worksheet.Dimension?.Address ?? "A1"].AutoFitColumns();
        }
        await using var stream = new MemoryStream();
        await package.SaveAsAsync(stream, cancellationToken);
        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoTaiSan_{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
    }

    [HttpGet("export.pdf")]
    public async Task<IActionResult> ExportPdf(
        DateTime? from,
        DateTime? to,
        int? categoryId,
        int? locationNodeId,
        CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var equipments = await FilterEquipment(from, to, categoryId, locationNodeId)
            .AsNoTracking()
            .Include(equipment => equipment.AssetCategory)
            .Include(equipment => equipment.LocationNode)
            .OrderBy(equipment => equipment.Name)
            .ToListAsync(cancellationToken);
        var maintenanceCost = await _context.MaintenanceRecords.AsNoTracking()
            .Where(record => (!from.HasValue || record.MaintenanceDate >= from.Value)
                && (!to.HasValue || record.MaintenanceDate <= to.Value))
            .SumAsync(record => (decimal?)record.Cost, cancellationToken) ?? 0;
        var borrowedCount = await _context.BorrowRequestDetails.AsNoTracking()
            .CountAsync(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed, cancellationToken);
        var overdueCount = await _context.BorrowRequestDetails.AsNoTracking()
            .CountAsync(detail => detail.BorrowRecord!.Status == BorrowStatuses.Borrowed
                && detail.BorrowRecord.ExpectedReturnDate < DateTime.UtcNow, cancellationToken);
        var lowStockCount = await _context.Consumables.AsNoTracking()
            .CountAsync(item => item.Quantity <= item.MinQuantity, cancellationToken);

        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Column(column =>
            {
                column.Item().Text("BÁO CÁO TÀI SẢN LAB IOT").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
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
                        table.Cell().Element(BodyCell).Text(item.Name);
                        table.Cell().Element(BodyCell).Text(item.Serial);
                        table.Cell().Element(BodyCell).Text(item.LocationNode?.Name ?? item.Location);
                        table.Cell().Element(BodyCell).Text(item.Status);
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
        stream.Position = 0;
        return File(stream, "application/pdf", $"BaoCaoTaiSan_{DateTime.UtcNow:yyyyMMddHHmm}.pdf");

        static QuestPDF.Infrastructure.IContainer HeaderCell(QuestPDF.Infrastructure.IContainer container)
            => container.Background(Colors.Blue.Darken2).Padding(4).DefaultTextStyle(style => style.FontColor(Colors.White).Bold());
        static QuestPDF.Infrastructure.IContainer BodyCell(QuestPDF.Infrastructure.IContainer container)
            => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    private IQueryable<Equipment> FilterEquipment(DateTime? from, DateTime? to, int? categoryId, int? locationNodeId)
    {
        var query = _context.Equipments.AsQueryable();
        if (from.HasValue) query = query.Where(item => item.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(item => item.CreatedAt <= to.Value);
        if (categoryId.HasValue) query = query.Where(item => item.AssetCategoryId == categoryId.Value);
        if (locationNodeId.HasValue) query = query.Where(item => item.LocationNodeId == locationNodeId.Value);
        return query;
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

    private static string SafeExcelText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
    }
}
