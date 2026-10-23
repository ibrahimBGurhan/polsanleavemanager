using PolsanLeave.Balance.Contracts;
using PolsanLeave.Leave.Contracts;

namespace PolsanLeave.Leave.Internal.Domain;

/// <summary>
/// R-3: Kalan = credits − approved requests; Bekleyen = pending requests; Kullanılabilir = Kalan − Bekleyen.
/// Every movement carries its source (docs/domain.md, invariants).
/// </summary>
internal static class BalanceSummaryCalculator
{
    public static BalanceSummary Calculate(Entitlement entitlement, IEnumerable<LeaveRequest> requests)
    {
        var credits = entitlement.Entries.Select(e => new BalanceMovement(e.Date, e.Days, ToSource(e.Source), e.Reason));
        var debits = requests
            .Where(r => r.IsActive)
            .Select(r => new BalanceMovement(
                r.StartDate,
                -r.WorkingDays,
                r.Status == LeaveRequestStatus.Approved ? BalanceMovementSource.ApprovedRequest : BalanceMovementSource.PendingRequest,
                r.Id.ToString()));
        var movements = credits.Concat(debits).OrderBy(m => m.Date).ToList();

        var remaining = movements.Where(m => m.Source != BalanceMovementSource.PendingRequest).Sum(m => m.Days);
        var pending = -movements.Where(m => m.Source == BalanceMovementSource.PendingRequest).Sum(m => m.Days);

        return new BalanceSummary(remaining, pending, remaining - pending, movements);
    }

    private static BalanceMovementSource ToSource(EntitlementSource source) => source switch
    {
        EntitlementSource.OpeningBalance => BalanceMovementSource.OpeningBalance,
        EntitlementSource.Accrual => BalanceMovementSource.Accrual,
        EntitlementSource.Adjustment => BalanceMovementSource.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };
}
