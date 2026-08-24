namespace LabManagementAPI.Models;

public class MaintenanceSchedule
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public string Name { get; set; } = string.Empty;
    public int IntervalDays { get; set; }
    public string IntervalUnit { get; set; } = "DAY";
    public DateTime NextDueAt { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public string Checklist { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
