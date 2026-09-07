using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/approval-delegations")]
[ApiController]
[Authorize]
public class ApprovalDelegationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IApprovalDelegationService _delegationService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public ApprovalDelegationController(
        AppDbContext context,
        IApprovalDelegationService delegationService,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _context = context;
        _delegationService = delegationService;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public sealed class CreateApprovalDelegationDto
    {
        [Range(1, int.MaxValue)]
        public int DelegateUserId { get; set; }

        [Required, MaxLength(50)]
        public string Scope { get; set; } = ApprovalDelegationScopes.Both;

        public bool CanHandover { get; set; }

        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyApprovalPermissions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isManager = role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead;
        var delegations = await _context.ApprovalDelegations
            .AsNoTracking()
            .Where(item => item.DelegateUserId == userId
                && item.DelegateUser != null
                && item.DelegateUser.IsActive
                && item.DelegateUser.Role == Roles.Teacher
                && item.IsActive
                && item.StartsAt <= DateTime.UtcNow
                && item.EndsAt >= DateTime.UtcNow)
            .OrderBy(item => item.EndsAt)
            .Select(item => new
            {
                item.Id,
                item.Scope,
                item.CanHandover,
                item.StartsAt,
                item.EndsAt,
                item.Reason,
                delegatorName = item.DelegatorUser!.FullName,
                delegatorUsername = item.DelegatorUser.Username
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            canApproveBorrow = isManager
                || delegations.Any(item => ApprovalDelegationScopes.Includes(item.Scope, ApprovalDelegationScopes.BorrowRequest)),
            canApproveConsumable = isManager
                || delegations.Any(item => ApprovalDelegationScopes.Includes(item.Scope, ApprovalDelegationScopes.ConsumableRequest)),
            canHandoverBorrow = isManager
                || delegations.Any(item => item.CanHandover && ApprovalDelegationScopes.Includes(item.Scope, ApprovalDelegationScopes.BorrowRequest)),
            canHandoverConsumable = isManager
                || delegations.Any(item => item.CanHandover && ApprovalDelegationScopes.Includes(item.Scope, ApprovalDelegationScopes.ConsumableRequest)),
            delegations
        });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> GetDelegations(CancellationToken cancellationToken)
    {
        var delegations = await _context.ApprovalDelegations
            .AsNoTracking()
            .Include(item => item.DelegatorUser)
            .Include(item => item.DelegateUser)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.DelegatorUserId,
                delegatorName = item.DelegatorUser!.FullName,
                delegatorUsername = item.DelegatorUser.Username,
                item.DelegateUserId,
                delegateName = item.DelegateUser!.FullName,
                delegateUsername = item.DelegateUser.Username,
                delegateCode = item.DelegateUser.UniversityCode,
                item.Scope,
                item.CanHandover,
                item.StartsAt,
                item.EndsAt,
                item.Reason,
                item.IsActive,
                item.CreatedAt,
                item.RevokedAt,
                status = !item.IsActive
                    ? "REVOKED"
                    : item.EndsAt >= DateTime.UtcNow
                        ? (item.StartsAt <= DateTime.UtcNow ? "ACTIVE" : "SCHEDULED")
                        : "EXPIRED"
            })
            .ToListAsync(cancellationToken);

        return Ok(delegations);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> CreateDelegation(
        [FromBody] CreateApprovalDelegationDto dto,
        CancellationToken cancellationToken)
    {
        var scope = dto.Scope.Trim().ToUpperInvariant();
        if (!ApprovalDelegationScopes.All.Contains(scope))
        {
            return BadRequest(new { message = "Phạm vi ủy quyền không hợp lệ." });
        }

        var startsAt = dto.StartsAt.UtcDateTime;
        var endsAt = dto.EndsAt.UtcDateTime;
        if (endsAt <= startsAt)
        {
            return BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });
        }

        if (endsAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Thời gian ủy quyền đã kết thúc." });
        }

        var reason = dto.Reason.Trim();
        if (reason.Length == 0)
        {
            return BadRequest(new { message = "Lý do ủy quyền là bắt buộc." });
        }

        var delegatorUserId = GetCurrentUserId();
        if (dto.DelegateUserId == delegatorUserId)
        {
            return BadRequest(new { message = "Không thể ủy quyền cho chính tài khoản đang thao tác." });
        }

        var delegateUser = await _context.Users
            .AsNoTracking()
            .Where(item => item.Id == dto.DelegateUserId)
            .Select(item => new { item.Id, item.Role, item.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (delegateUser is null || delegateUser.Role != Roles.Teacher || !delegateUser.IsActive)
        {
            return BadRequest(new { message = "Tài khoản được ủy quyền phải là giảng viên đang hoạt động." });
        }

        var hasOverlap = await _context.ApprovalDelegations.AnyAsync(item =>
            item.DelegateUserId == dto.DelegateUserId
            && item.IsActive
            && item.StartsAt < endsAt
            && item.EndsAt > startsAt
            && (scope == ApprovalDelegationScopes.Both
                || item.Scope == ApprovalDelegationScopes.Both
                || item.Scope == scope),
            cancellationToken);
        if (hasOverlap)
        {
            return Conflict(new { message = "Giảng viên này đã có quyền ủy quyền trùng thời gian và phạm vi." });
        }

        var delegation = new ApprovalDelegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = dto.DelegateUserId,
            Scope = scope,
            CanHandover = dto.CanHandover,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Reason = reason,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ApprovalDelegations.Add(delegation);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(ApprovalDelegation),
            delegation.Id,
            new { delegation.DelegateUserId, delegation.Scope, delegation.CanHandover, delegation.StartsAt, delegation.EndsAt },
            cancellationToken);
        var notificationUrl = scope == ApprovalDelegationScopes.ConsumableRequest
            ? "/dashboard/consumable-requests"
            : "/dashboard/borrow-requests";
        await _notificationService.NotifyUserAsync(
            delegation.DelegateUserId,
            "APPROVAL_DELEGATION_CREATED",
            "Bạn được ủy quyền duyệt",
            $"Bạn được ủy quyền {scopeLabel(scope)}{(delegation.CanHandover ? " và bàn giao" : string.Empty)} từ {VietnamTime.Now(startsAt):dd/MM/yyyy HH:mm} đến {VietnamTime.Now(endsAt):dd/MM/yyyy HH:mm}.",
            notificationUrl,
            cancellationToken);

        return Ok(new { delegation.Id, message = "Đã tạo quyền ủy quyền duyệt." });
    }

    [HttpPut("{id:int}/revoke")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> RevokeDelegation(int id, CancellationToken cancellationToken)
    {
        var delegation = await _context.ApprovalDelegations
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (delegation is null)
        {
            return NotFound(new { message = "Không tìm thấy quyền ủy quyền." });
        }

        if (!delegation.IsActive)
        {
            return Conflict(new { message = "Quyền ủy quyền đã được thu hồi hoặc hết hiệu lực." });
        }

        delegation.IsActive = false;
        delegation.RevokedAt = DateTime.UtcNow;
        delegation.RevokedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Revoke",
            nameof(ApprovalDelegation),
            id,
            cancellationToken: cancellationToken);
        await _notificationService.NotifyUserAsync(
            delegation.DelegateUserId,
            "APPROVAL_DELEGATION_REVOKED",
            "Quyền ủy quyền đã bị thu hồi",
            "Quyền duyệt được ủy quyền cho bạn đã bị thu hồi.",
            "/dashboard/borrow-history",
            cancellationToken);

        return Ok(new { message = "Đã thu hồi quyền ủy quyền." });
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private static string scopeLabel(string scope) => scope switch
    {
        ApprovalDelegationScopes.BorrowRequest => "duyệt yêu cầu mượn/trả",
        ApprovalDelegationScopes.ConsumableRequest => "duyệt yêu cầu cấp phát",
        _ => "duyệt yêu cầu mượn/trả và cấp phát"
    };
}
