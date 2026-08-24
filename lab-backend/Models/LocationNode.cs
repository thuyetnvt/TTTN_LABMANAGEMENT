namespace LabManagementAPI.Models;

public class LocationNode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public LocationNode? Parent { get; set; }
    public ICollection<LocationNode> Children { get; set; } = new List<LocationNode>();
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
