using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Services;

public interface IApprovalDelegationService
{
    Task<bool> CanApproveAsync(
        int userId,
        string? role,
        string scope,
        CancellationToken cancellationToken = default);

    Task<bool> CanHandoverAsync(
        int userId,
        string? role,
        string scope,
        CancellationToken cancellationToken = default);

    Task<ApprovalDelegation?> GetActiveDelegationAsync(
        int userId,
        string scope,
        CancellationToken cancellationToken = default);
}

public sealed class ApprovalDelegationService : IApprovalDelegationService
{
    private readonly AppDbContext _context;

    public ApprovalDelegationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanApproveAsync(
        int userId,
        string? role,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead)
        {
            return true;
        }

        return await _context.ApprovalDelegations
            .AsNoTracking()
            .AnyAsync(item =>
                item.DelegateUserId == userId
                && item.DelegateUser != null
                && item.DelegateUser.IsActive
                && item.DelegateUser.Role == Roles.Teacher
                && item.IsActive
                && item.StartsAt <= DateTime.UtcNow
                && item.EndsAt >= DateTime.UtcNow
                && (item.Scope == scope || item.Scope == ApprovalDelegationScopes.Both),
                cancellationToken);
    }

    public async Task<bool> CanHandoverAsync(
        int userId,
        string? role,
        string scope,
        CancellationToken cancellationToken = default)
    {
        if (role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead)
        {
            return true;
        }

        return await _context.ApprovalDelegations
            .AsNoTracking()
            .AnyAsync(item =>
                item.DelegateUserId == userId
                && item.DelegateUser != null
                && item.DelegateUser.IsActive
                && item.DelegateUser.Role == Roles.Teacher
                && item.IsActive
                && item.CanHandover
                && item.StartsAt <= DateTime.UtcNow
                && item.EndsAt >= DateTime.UtcNow
                && (item.Scope == scope || item.Scope == ApprovalDelegationScopes.Both),
                cancellationToken);
    }

    public Task<ApprovalDelegation?> GetActiveDelegationAsync(
        int userId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        return _context.ApprovalDelegations
            .AsNoTracking()
            .Include(item => item.DelegatorUser)
            .Include(item => item.DelegateUser)
            .Where(item =>
                item.DelegateUserId == userId
                && item.DelegateUser != null
                && item.DelegateUser.IsActive
                && item.DelegateUser.Role == Roles.Teacher
                && item.IsActive
                && item.StartsAt <= DateTime.UtcNow
                && item.EndsAt >= DateTime.UtcNow
                && (item.Scope == scope || item.Scope == ApprovalDelegationScopes.Both))
            .OrderByDescending(item => item.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
