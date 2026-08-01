namespace LabManagementAPI.Models;

public static class EquipmentStatuses
{
    public const string Available = "Rảnh";
    public const string Borrowed = "Đang mượn";
    public const string Broken = "Hỏng";
    public const string Warranty = "Bảo hành";

    public static readonly HashSet<string> All =
    [
        Available,
        Borrowed,
        Broken,
        Warranty
    ];
}
