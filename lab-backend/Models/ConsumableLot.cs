namespace LabManagementAPI.Models;

public class ConsumableLot
{
    public int Id { get; set; }
    public int ConsumableId { get; set; }
    public Consumable? Consumable { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public int InitialQuantity { get; set; }
    public int Quantity { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ConsumableRequestLotAllocation> RequestAllocations { get; set; } = new List<ConsumableRequestLotAllocation>();
}
