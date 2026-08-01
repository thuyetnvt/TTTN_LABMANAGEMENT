namespace LabManagementAPI.Models
{
    public class BorrowRequestDetail
    {
        public int Id { get; set; }

        public int BorrowRecordId { get; set; }
        public BorrowRecord? BorrowRecord { get; set; }

        public int EquipmentId { get; set; }
        public Equipment? Equipment { get; set; }

        public int Quantity { get; set; } = 1;

        public string Note { get; set; } = string.Empty;
    }
}
