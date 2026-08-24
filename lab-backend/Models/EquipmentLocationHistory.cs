namespace LabManagementAPI.Models;

public class EquipmentLocationHistory
{
    public long Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public int? FromLocationNodeId { get; set; }
    public LocationNode? FromLocationNode { get; set; }
    public int? ToLocationNodeId { get; set; }
    public LocationNode? ToLocationNode { get; set; }
    public string FromLocationName { get; set; } = string.Empty;
    public string ToLocationName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
