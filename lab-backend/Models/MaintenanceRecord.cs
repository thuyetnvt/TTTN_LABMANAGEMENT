using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabManagementAPI.Models
{
    public class MaintenanceRecord
    {
        public int Id { get; set; }

        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }

        public DateTime MaintenanceDate { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public decimal Cost { get; set; }

        public string PerformedBy { get; set; } = string.Empty;

        public string Status { get; set; } = MaintenanceStatuses.InProgress;

        public DateTime? CompletedAt { get; set; }

        public string Result { get; set; } = string.Empty;
    }
}
