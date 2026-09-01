namespace LabManagementAPI.Dtos;

/// <summary>
/// Thông tin tài sản an toàn cho người mượn (Giảng viên/Sinh viên).
/// Không chứa QR token, dữ liệu mua sắm, tài chính hoặc thông tin quản trị nội bộ.
/// </summary>
public class BorrowerEquipmentDto
{
    public int Id { get; init; }
    public string AssetCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Serial { get; init; } = string.Empty;
    public string SerialName { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string ImagePath { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int? LocationNodeId { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public DateTime? WarrantyExpiry { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? AssetCategoryId { get; init; }
    public string? CategoryName { get; init; }
}

/// <summary>
/// Thông tin đầy đủ dành riêng cho quản trị viên, Trưởng lab và Phó lab.
/// </summary>
public sealed class ManagerEquipmentDto : BorrowerEquipmentDto
{
    public string QrToken { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string Imei { get; init; } = string.Empty;
    public string FirmwareVersion { get; init; } = string.Empty;
    public string Supplier { get; init; } = string.Empty;
    public string FundingSource { get; init; } = string.Empty;
    public decimal? PurchaseValue { get; init; }
    public DateTime? LastInventoryAt { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string ResponsiblePerson { get; init; } = string.Empty;
    public string DecisionFileName { get; init; } = string.Empty;
    public bool HasDecisionFile { get; init; }
    public DateTime? EntryDate { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public int BorrowCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
