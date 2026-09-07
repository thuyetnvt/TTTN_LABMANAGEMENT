namespace LabManagementAPI.Models;

public class HandoverIssueEvidence
{
    public long Id { get; set; }
    public int HandoverIssueReportId { get; set; }
    public HandoverIssueReport? HandoverIssueReport { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public int UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
