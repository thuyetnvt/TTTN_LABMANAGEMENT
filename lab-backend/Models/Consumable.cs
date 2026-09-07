using System.ComponentModel.DataAnnotations;

namespace LabManagementAPI.Models
{
    public class Consumable
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Unit { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int ReservedQuantity { get; set; }

        public int MinQuantity { get; set; } = 5;

        // Kept for backwards compatibility with legacy records and reports.
        public string ResponsiblePerson { get; set; } = string.Empty;

        // CreatedByUserId is nullable so existing rows created before this
        // relationship was introduced can continue to be read safely.
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public int? ResponsibleUserId { get; set; }
        public User? ResponsibleUser { get; set; }

        public int? AssetCategoryId { get; set; }
        public AssetCategory? AssetCategory { get; set; }

        public DateTime? EntryDate { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string Supplier { get; set; } = string.Empty;
        public decimal? UnitCost { get; set; }
        public string StorageLocation { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ConsumableLot> Lots { get; set; } = new List<ConsumableLot>();
    }
}
