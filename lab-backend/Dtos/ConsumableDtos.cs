namespace LabManagementAPI.Dtos;

public class BorrowerConsumableDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int AvailableQuantity { get; init; }
    public int MinQuantity { get; init; }
    public int? AssetCategoryId { get; init; }
    public string? CategoryName { get; init; }
}

public sealed class ManagerConsumableDto : BorrowerConsumableDto
{
    public int ReservedQuantity { get; init; }
    public string ResponsiblePerson { get; init; } = string.Empty;
    public DateTime? EntryDate { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string Supplier { get; init; } = string.Empty;
    public decimal? UnitCost { get; init; }
    public string StorageLocation { get; init; } = string.Empty;
    public string LotNumber { get; init; } = string.Empty;
    public DateTime? ExpiryDate { get; init; }
    public int LotCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
