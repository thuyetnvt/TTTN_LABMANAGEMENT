namespace LabManagementAPI.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int InventorySessionId { get; set; }
    public InventorySession? InventorySession { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public int? ExpectedLocationNodeId { get; set; }
    public string ExpectedLocationName { get; set; } = string.Empty;
    public string Status { get; set; } = InventoryItemStatuses.Pending;
    public DateTime? ScannedAt { get; set; }
    public int? ScannedByUserId { get; set; }
    public User? ScannedByUser { get; set; }
    public string Note { get; set; } = string.Empty;
}
