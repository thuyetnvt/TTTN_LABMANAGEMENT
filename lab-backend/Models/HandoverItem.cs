namespace LabManagementAPI.Models;

public class HandoverItem
{
    public int Id { get; set; }
    public int HandoverRecordId { get; set; }
    public HandoverRecord? HandoverRecord { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public string Condition { get; set; } = EquipmentStatuses.Available;
    public string Accessories { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
