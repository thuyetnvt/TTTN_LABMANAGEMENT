namespace LabManagementAPI.Models
{
    public class ConsumableTransaction
    {
        public long Id { get; set; }

        public int ConsumableId { get; set; }
        public Consumable? Consumable { get; set; }

        public string Type { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int BeforeQuantity { get; set; }

        public int AfterQuantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? ConsumableRequestId { get; set; }
        public ConsumableRequest? ConsumableRequest { get; set; }
        public int? MaintenanceRecordId { get; set; }
        public MaintenanceRecord? MaintenanceRecord { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
