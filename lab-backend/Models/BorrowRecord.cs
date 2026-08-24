namespace LabManagementAPI.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }

        public int? TeacherId { get; set; }
        public User? Teacher { get; set; }

        public DateTime BorrowDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string Status { get; set; } = BorrowStatuses.Pending;

        public string ReturnCondition { get; set; } = string.Empty;
        public string ReturnInspectionNote { get; set; } = string.Empty;
        public bool? IsUnderWarrantyAtReturn { get; set; }
        public string WarrantyAction { get; set; } = string.Empty;
        public decimal CompensationAmount { get; set; }
        public int? InspectedByUserId { get; set; }
        public User? InspectedByUser { get; set; }
        public ICollection<BorrowRequestDetail> Details { get; set; } = new List<BorrowRequestDetail>();
    }
}
