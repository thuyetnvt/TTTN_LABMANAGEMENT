using System.ComponentModel.DataAnnotations;

namespace LabManagementAPI.Models
{
    public class AssetCategory
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
