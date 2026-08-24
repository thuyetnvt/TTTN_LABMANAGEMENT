namespace LabManagementAPI.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string QrToken { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public string SerialName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string Imei { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string FundingSource { get; set; } = string.Empty;
        public decimal? PurchaseValue { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public DateTime? LastInventoryAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        public LocationNode? LocationNode { get; set; }
        public string ResponsiblePerson { get; set; } = string.Empty;
        public string DecisionFileName { get; set; } = string.Empty;
        public string DecisionFilePath { get; set; } = string.Empty;
        public DateTime? DecisionUploadedAt { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = EquipmentStatuses.Available;
        public int BorrowCount { get; set; } = 0;
        public int? AssetCategoryId { get; set; }
        public AssetCategory? AssetCategory { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

