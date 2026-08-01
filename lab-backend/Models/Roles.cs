namespace LabManagementAPI.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string LabHead = "Trưởng lab";
    public const string DeputyLabHead = "Phó lab";
    public const string Teacher = "Giảng viên";
    public const string Student = "Sinh viên";

    public const string Managers = Admin + "," + LabHead + "," + DeputyLabHead;
    public const string Borrowers = Student + "," + Teacher;

    public static readonly HashSet<string> All =
    [
        Admin,
        LabHead,
        DeputyLabHead,
        Teacher,
        Student
    ];
}
