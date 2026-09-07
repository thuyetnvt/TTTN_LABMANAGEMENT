namespace LabManagementAPI.Models;

public class HandoverIssueReport
{
    public int Id { get; set; }
    public int HandoverRecordId { get; set; }
    public HandoverRecord? HandoverRecord { get; set; }
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public int ReportedByUserId { get; set; }
    public User? ReportedByUser { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = HandoverIssueReportStatuses.Pending;
    public string ResolutionAction { get; set; } = string.Empty;
    public string ResolutionNote { get; set; } = string.Empty;
    public int? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
