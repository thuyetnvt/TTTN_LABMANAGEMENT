namespace LabManagementAPI.Models;

public class MaintenancePartUsage
{
    public long Id { get; set; }
    public int MaintenanceRecordId { get; set; }
    public MaintenanceRecord? MaintenanceRecord { get; set; }
    public int ConsumableId { get; set; }
    public Consumable? Consumable { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
