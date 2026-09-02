namespace LabManagementAPI.Models;

/// <summary>
/// Borrow workflow states that still own a reservation or are waiting for a
/// handover. An equipment item must not be edited, moved, deleted, or sent to
/// maintenance while one of these requests references it.
/// </summary>
public static class BorrowLockRules
{
    public static readonly string[] EquipmentLockedBorrowStatuses =
    [
        BorrowStatuses.Pending,
        BorrowStatuses.TeacherPending,
        BorrowStatuses.ProcessingApproval,
        BorrowStatuses.Approved
    ];

    public static bool IsEquipmentLockedByBorrowStatus(string? status)
        => EquipmentLockedBorrowStatuses.Contains(status, StringComparer.Ordinal);
}
