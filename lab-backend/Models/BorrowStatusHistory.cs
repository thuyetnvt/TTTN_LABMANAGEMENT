namespace LabManagementAPI.Models;

public class BorrowStatusHistory
{
    public long Id { get; set; }
    public int BorrowRecordId { get; set; }
    public BorrowRecord? BorrowRecord { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
