using System.ComponentModel.DataAnnotations;

namespace LabManagementAPI.Models
{
    public class Consumable
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Unit { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int MinQuantity { get; set; } = 5;

        public string ResponsiblePerson { get; set; } = string.Empty;

        public int? AssetCategoryId { get; set; }
        public AssetCategory? AssetCategory { get; set; }

        public DateTime? EntryDate { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
