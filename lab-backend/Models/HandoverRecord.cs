namespace LabManagementAPI.Models;

public class HandoverRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int BorrowRecordId { get; set; }
    public BorrowRecord? BorrowRecord { get; set; }
    public int HandedOverByUserId { get; set; }
    public User? HandedOverByUser { get; set; }
    public int ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }
    public DateTime HandoverAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public ICollection<HandoverItem> Items { get; set; } = new List<HandoverItem>();
    public ICollection<HandoverEvidence> Evidence { get; set; } = new List<HandoverEvidence>();
}
