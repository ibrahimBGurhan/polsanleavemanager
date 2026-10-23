using PolsanLeave.SharedKernel;

namespace PolsanLeave.Leave.Contracts;

/// <summary>
/// Annual-leave requests of the signed-in employee (spec 0001). Every operation acts on the current
/// employee only — no method takes an employee or request id (docs/security.md, IDOR).
/// </summary>
public interface ILeaveRequestService
{
    /// <summary>Kalan · Bekleyen · Kullanılabilir, with the movements behind them (R-3).</summary>
    Task<Result<BalanceSummary>> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>The current employee's requests, newest first (R-7).</summary>
    Task<Result<IReadOnlyList<LeaveRequestItem>>> GetMineAsync(CancellationToken cancellationToken = default);

    /// <summary>Working days of a range and the available balance after it, before submitting (R-8).</summary>
    Task<Result<LeavePreview>> PreviewAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>Creates a <see cref="LeaveRequestStatus.Pending"/> annual-leave request (R-5, R-6).</summary>
    Task<Result<LeaveRequestItem>> SubmitAsync(SubmitLeaveRequest request, CancellationToken cancellationToken = default);
}

public sealed record SubmitLeaveRequest(DateOnly StartDate, DateOnly EndDate, string? Description);

public sealed record LeaveRequestItem(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDays,
    LeaveRequestStatus Status,
    DateTimeOffset CreatedAtUtc);

public sealed record LeavePreview(int WorkingDays, int AvailableAfter);

/// <param name="HasApprover">False when the employee has no direct manager: requests cannot be submitted (AC-30).</param>
public sealed record BalanceSummary(int Remaining, int Pending, int Available, IReadOnlyList<BalanceMovement> Movements, bool HasApprover = true);

/// <summary>One credit or debit and what caused it (docs/domain.md: the balance never changes silently).</summary>
public sealed record BalanceMovement(DateOnly Date, int Days, BalanceMovementSource Source, string Reference);

public enum BalanceMovementSource
{
    OpeningBalance,
    Accrual,
    Adjustment,
    ApprovedRequest,
    PendingRequest,
}

public enum LeaveRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
}
