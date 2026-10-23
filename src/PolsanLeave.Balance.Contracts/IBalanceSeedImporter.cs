namespace PolsanLeave.Balance.Contracts;

/// <summary>Loads the opening date and opening balances from HR's seed (spec 0001, R-3). Idempotent.</summary>
public interface IBalanceSeedImporter
{
    /// <summary>
    /// Sets the opening date once (a different date later is refused) and writes each employee's opening
    /// balance once; an existing opening balance is never changed — corrections are adjustments (BR-8).
    /// </summary>
    Task<BalanceSeedReport> ImportAsync(DateOnly openingDate, IReadOnlyList<OpeningBalanceSeed> openingBalances, CancellationToken cancellationToken = default);
}

public sealed record OpeningBalanceSeed(Guid EmployeeId, int Days);

/// <param name="Unchanged">Employees whose stored opening balance differs from the seed and was kept.</param>
public sealed record BalanceSeedReport(int Written, int AlreadyPresent, IReadOnlyList<Guid> Unchanged);
