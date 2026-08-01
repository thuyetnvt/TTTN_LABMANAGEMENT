using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabManagementAPI.Models
{
    public class ConsumableRequest
    {
        public int Id { get; set; }

        public int ConsumableId { get; set; }
        public Consumable? Consumable { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int Quantity { get; set; }
        
        [Required]
        public string Reason { get; set; } = string.Empty;

        // "Chờ duyệt", "Đã cấp phát", "Từ chối"
        public string Status { get; set; } = "Chờ duyệt";

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovalDate { get; set; }
    }
}
