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

        public string Status { get; set; } = ConsumableRequestStatuses.Pending;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovalDate { get; set; }

        public DateTime? HandedOverAt { get; set; }
        public int? HandedOverByUserId { get; set; }
        public User? HandedOverByUser { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public int? ReceivedByUserId { get; set; }
        public User? ReceivedByUser { get; set; }
        public ICollection<ConsumableRequestLotAllocation> LotAllocations { get; set; } = new List<ConsumableRequestLotAllocation>();
    }
}
