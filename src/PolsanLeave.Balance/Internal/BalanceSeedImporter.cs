using Microsoft.EntityFrameworkCore;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Balance.Internal.Data;
using PolsanLeave.Balance.Internal.Domain;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Balance.Internal;

internal sealed class BalanceSeedImporter(IDbContextFactory<BalanceDbContext> contexts, IBusinessClock clock) : IBalanceSeedImporter
{
    public async Task<BalanceSeedReport> ImportAsync(DateOnly openingDate, IReadOnlyList<OpeningBalanceSeed> openingBalances, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openingBalances);
        if (openingBalances.Any(b => b.Days < 0))
        {
            throw new InvalidOperationException("Invalid balance seed: opening balances cannot be negative (BR-4).");
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var settings = await db.Settings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            db.Settings.Add(new BalanceSettings(openingDate));
        }
        else if (settings.OpeningDate != openingDate)
        {
            throw new InvalidOperationException($"Opening date is already {settings.OpeningDate:yyyy-MM-dd}; it cannot change to {openingDate:yyyy-MM-dd}.");
        }

        var ids = openingBalances.Select(b => b.EmployeeId).ToList();
        var stored = await db.Adjustments
            .Where(a => a.Kind == AdjustmentKind.OpeningBalance && ids.Contains(a.EmployeeId))
            .ToDictionaryAsync(a => a.EmployeeId, a => a.Days, cancellationToken);

        var written = 0;
        var unchanged = new List<Guid>();
        foreach (var seed in openingBalances)
        {
            if (stored.TryGetValue(seed.EmployeeId, out var days))
            {
                if (days != seed.Days)
                {
                    unchanged.Add(seed.EmployeeId);
                }

                continue;
            }

            db.Adjustments.Add(BalanceAdjustment.OpeningBalance(Guid.CreateVersion7(clock.UtcNow), seed.EmployeeId, openingDate, seed.Days, clock.UtcNow));
            written++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BalanceSeedReport(written, openingBalances.Count - written, unchanged);
    }
}
