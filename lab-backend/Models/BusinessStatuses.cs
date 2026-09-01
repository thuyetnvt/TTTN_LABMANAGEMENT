namespace LabManagementAPI.Models;

public static class BorrowStatuses
{
    public const string Pending = "BORROW_PENDING";
    public const string TeacherPending = "TEACHER_PENDING";
    public const string Approved = "APPROVED";
    public const string Borrowed = "BORROWED";
    public const string ReturnProcessing = "RETURN_PROCESSING";
    public const string Returned = "RETURNED";
    public const string ReturnedDamaged = "RETURNED_DAMAGED";
    public const string Rejected = "REJECTED";
    public const string ProcessingApproval = "APPROVAL_PROCESSING";
}

public static class ConsumableRequestStatuses
{
    public const string Pending = "CONSUMABLE_PENDING";
    public const string Processing = "CONSUMABLE_PROCESSING";
    public const string Approved = "CONSUMABLE_APPROVED";
    public const string HandedOver = "CONSUMABLE_HANDED_OVER";
    public const string Received = "CONSUMABLE_RECEIVED";
    public const string Issued = "CONSUMABLE_ISSUED";
    public const string Rejected = "REJECTED";
}

public static class MaintenanceStatuses
{
    public const string InProgress = "MAINTENANCE_IN_PROGRESS";
    public const string Completing = "MAINTENANCE_COMPLETING";
    public const string Completed = "MAINTENANCE_COMPLETED";
}

public static class PenaltyStatuses
{
    public const string Unpaid = "UNPAID";
    public const string Paid = "PAID";
}

public static class InventoryStatuses
{
    public const string Open = "INVENTORY_OPEN";
    public const string Reviewing = "INVENTORY_REVIEWING";
    public const string Completed = "INVENTORY_COMPLETED";
}

public static class InventoryReviewResolutions
{
    public const string ConfirmedFound = "CONFIRMED_FOUND";
    public const string UpdateLocation = "UPDATE_LOCATION";
    public const string KeepRecordedLocation = "KEEP_RECORDED_LOCATION";
    public const string MarkDamaged = "MARK_DAMAGED";
    public const string MarkMissing = "MARK_MISSING";
}

public static class InventoryItemStatuses
{
    public const string Pending = "INVENTORY_PENDING";
    public const string Found = "INVENTORY_FOUND";
    public const string WrongLocation = "INVENTORY_WRONG_LOCATION";
    public const string Damaged = "INVENTORY_DAMAGED";
    public const string Missing = "INVENTORY_MISSING";
}

public static class StatusCodeMap
{
    public static readonly IReadOnlyDictionary<string, string> LegacyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Chờ duyệt"] = BorrowStatuses.Pending,
            ["Chờ GV duyệt"] = BorrowStatuses.TeacherPending,
            ["Đang xử lý duyệt"] = BorrowStatuses.ProcessingApproval,
            ["Đang xử lý trả"] = BorrowStatuses.ReturnProcessing,
            ["Đang mượn"] = BorrowStatuses.Borrowed,
            ["Đã trả"] = BorrowStatuses.Returned,
            ["Đã trả (Hỏng)"] = BorrowStatuses.ReturnedDamaged,
            ["Đã trả (Bảo hành)"] = BorrowStatuses.ReturnedDamaged,
            ["Từ chối"] = BorrowStatuses.Rejected,
            ["Đang xử lý"] = MaintenanceStatuses.InProgress,
            ["Hoàn tất"] = MaintenanceStatuses.Completed,
            ["Hoàn thành"] = MaintenanceStatuses.Completed,
            ["Đã cấp phát"] = ConsumableRequestStatuses.Issued,
            ["Chưa thanh toán"] = PenaltyStatuses.Unpaid,
            ["Đã thanh toán"] = PenaltyStatuses.Paid
        };

    public static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [BorrowStatuses.Pending] = "Chờ duyệt",
            [BorrowStatuses.TeacherPending] = "Chờ giảng viên duyệt",
            [BorrowStatuses.Approved] = "Đã duyệt, chờ bàn giao",
            [BorrowStatuses.Borrowed] = "Đang mượn",
            [BorrowStatuses.ReturnProcessing] = "Đang xử lý trả",
            [BorrowStatuses.Returned] = "Đã trả",
            [BorrowStatuses.ReturnedDamaged] = "Đã trả, có hư hỏng",
            [BorrowStatuses.Rejected] = "Từ chối",
            [BorrowStatuses.ProcessingApproval] = "Đang xử lý duyệt",
            [ConsumableRequestStatuses.Pending] = "Chờ duyệt cấp phát",
            [ConsumableRequestStatuses.Processing] = "Đang xử lý cấp phát",
            [ConsumableRequestStatuses.Approved] = "Đã duyệt, chờ bàn giao",
            [ConsumableRequestStatuses.HandedOver] = "Đã bàn giao, chờ xác nhận",
            [ConsumableRequestStatuses.Received] = "Đã nhận vật tư",
            [ConsumableRequestStatuses.Issued] = "Đã cấp phát",
            [MaintenanceStatuses.InProgress] = "Đang bảo trì",
            [MaintenanceStatuses.Completing] = "Đang nghiệm thu",
            [MaintenanceStatuses.Completed] = "Đã hoàn thành bảo trì",
            [PenaltyStatuses.Unpaid] = "Chưa thanh toán",
            [PenaltyStatuses.Paid] = "Đã thanh toán",
            [InventoryStatuses.Open] = "Đang kiểm kê",
            [InventoryStatuses.Reviewing] = "Đang đối soát",
            [InventoryStatuses.Completed] = "Đã kết thúc kiểm kê",
            [EquipmentStatuses.Available] = "Sẵn sàng",
            [EquipmentStatuses.Broken] = "Hỏng",
            [EquipmentStatuses.Missing] = "Thất lạc",
            [EquipmentStatuses.UnderWarranty] = "Đang bảo hành"
        };

    public static string Normalize(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (LegacyMap.TryGetValue(candidate, out var mapped)) return mapped;
        return EquipmentStatuses.Normalize(candidate);
    }

    public static string Label(string? value)
    {
        var normalized = Normalize(value);
        return Labels.TryGetValue(normalized, out var label) ? label : normalized;
    }
}
