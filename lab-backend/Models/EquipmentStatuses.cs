namespace LabManagementAPI.Models;

public static class EquipmentStatuses
{
    public const string Available = "AVAILABLE";
    public const string BorrowPending = "BORROW_PENDING";
    public const string Borrowed = "BORROWED";
    public const string Returned = "RETURNED";
    public const string ReturnedDamaged = "RETURNED_DAMAGED";
    public const string Broken = "BROKEN";
    public const string Missing = "MISSING";

    public static readonly HashSet<string> All =
    [
        Available,
        BorrowPending,
        Borrowed,
        Returned,
        ReturnedDamaged,
        Broken,
        Missing
    ];

    public static readonly IReadOnlyDictionary<string, string> LegacyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Rảnh"] = Available,
            ["Sẵn sàng"] = Available,
            ["Đang mượn"] = Borrowed,
            ["Hỏng"] = Broken
        };

    public static string Normalize(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (LegacyMap.TryGetValue(candidate, out var mapped)) return mapped;
        return All.FirstOrDefault(status => status.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            ?? candidate;
    }
}
