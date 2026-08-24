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
}
