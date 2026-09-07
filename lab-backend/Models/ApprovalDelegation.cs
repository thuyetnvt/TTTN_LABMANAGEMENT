namespace LabManagementAPI.Models;

public static class ApprovalDelegationScopes
{
    public const string BorrowRequest = "BORROW_REQUEST";
    public const string ConsumableRequest = "CONSUMABLE_REQUEST";
    public const string Both = "BOTH";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BorrowRequest,
            ConsumableRequest,
            Both
        };

    public static bool Includes(string? scope, string requiredScope)
    {
        return string.Equals(scope, requiredScope, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, Both, StringComparison.OrdinalIgnoreCase);
    }
}

public class ApprovalDelegation
{
    public int Id { get; set; }

    public int DelegatorUserId { get; set; }
    public User? DelegatorUser { get; set; }

    public int DelegateUserId { get; set; }
    public User? DelegateUser { get; set; }

    public string Scope { get; set; } = ApprovalDelegationScopes.Both;
    public bool CanHandover { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public int? RevokedByUserId { get; set; }
    public User? RevokedByUser { get; set; }
}
