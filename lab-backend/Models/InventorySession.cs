namespace LabManagementAPI.Models;

public class InventorySession
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? LocationNodeId { get; set; }
    public LocationNode? LocationNode { get; set; }
    public int? AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public string Status { get; set; } = InventoryStatuses.Open;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
