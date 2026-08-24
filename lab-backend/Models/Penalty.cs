using System.ComponentModel.DataAnnotations;

namespace LabManagementAPI.Models
{
    public class Penalty
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }

        public int BorrowRecordId { get; set; }
        public BorrowRecord? BorrowRecord { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = PenaltyStatuses.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
