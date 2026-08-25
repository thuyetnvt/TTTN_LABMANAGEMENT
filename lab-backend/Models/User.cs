namespace LabManagementAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? UniversityCode { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AvatarStorageKey { get; set; }
    public DateTime? AvatarUpdatedAt { get; set; }
}
