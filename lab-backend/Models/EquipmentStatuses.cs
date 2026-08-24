namespace LabManagementAPI.Models;

public static class EquipmentStatuses
{
    public const string Available = "AVAILABLE";
    public const string BorrowPending = "BORROW_PENDING";
    public const string Borrowed = "BORROWED";
    public const string Returned = "RETURNED";
    public const string ReturnedDamaged = "RETURNED_DAMAGED";
    public const string Broken = "BROKEN";
    public const string UnderWarranty = "UNDER_WARRANTY";
    public const string Warranty = UnderWarranty;
    public const string MaintenanceInProgress = "MAINTENANCE_IN_PROGRESS";
    public const string MaintenanceCompleted = "MAINTENANCE_COMPLETED";

    public static readonly HashSet<string> All =
    [
        Available,
        BorrowPending,
        Borrowed,
        Returned,
        ReturnedDamaged,
        Broken,
        UnderWarranty,
        MaintenanceInProgress,
        MaintenanceCompleted
    ];

    public static readonly IReadOnlyDictionary<string, string> LegacyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rảnh"] = Available,
            ["Sẵn sàng"] = Available,
            ["Đang mượn"] = Borrowed,
            ["Hỏng"] = Broken,
            ["Bảo hành"] = UnderWarranty,
            ["Bảo trì"] = MaintenanceInProgress
        };
}
