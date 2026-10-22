namespace PolsanLeave.Balance.Contracts;

/// <summary>
/// The credit side of an employee's annual-leave balance: opening balance, anniversary accruals
/// (BR-1, BR-2) and adjustments. Debits (requests) belong to the Leave module (plan 0001, Risk 1).
/// </summary>
public interface IEntitlementService
{
    /// <summary>All credits dated on or before <paramref name="asOf"/>; never expire (BR-9).</summary>
    Task<Entitlement> GetAsync(Guid employeeId, DateOnly asOf, CancellationToken cancellationToken = default);
}

public sealed record Entitlement(IReadOnlyList<EntitlementEntry> Entries)
{
    public int TotalDays => Entries.Sum(e => e.Days);
}

/// <summary>One balance movement with its source — every credit is traceable (docs/domain.md, invariants).</summary>
public sealed record EntitlementEntry(DateOnly Date, int Days, EntitlementSource Source, string Reason);

public enum EntitlementSource
{
    OpeningBalance,
    Accrual,
    Adjustment,
}
