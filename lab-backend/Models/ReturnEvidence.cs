namespace LabManagementAPI.Models;

public class ReturnEvidence
{
    public long Id { get; set; }
    public int BorrowRecordId { get; set; }
    public BorrowRecord? BorrowRecord { get; set; }
    public int? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    public string EvidenceType { get; set; } = "PHOTO_AFTER";
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public int UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
