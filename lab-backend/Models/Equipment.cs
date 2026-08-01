namespace LabManagementAPI.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public string SerialName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ResponsiblePerson { get; set; } = string.Empty;
        public string DecisionFileName { get; set; } = string.Empty;
        public string DecisionFilePath { get; set; } = string.Empty;
        public DateTime? DecisionUploadedAt { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Rảnh";
        public int BorrowCount { get; set; } = 0;
        public int? AssetCategoryId { get; set; }
        public AssetCategory? AssetCategory { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

