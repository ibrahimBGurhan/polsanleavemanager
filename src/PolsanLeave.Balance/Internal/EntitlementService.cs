using Microsoft.EntityFrameworkCore;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Balance.Internal.Data;
using PolsanLeave.Balance.Internal.Domain;
using PolsanLeave.Identity.Contracts;

namespace PolsanLeave.Balance.Internal;

internal sealed class EntitlementService(IDbContextFactory<BalanceDbContext> contexts, IEmployeeDirectory employees) : IEntitlementService
{
    public async Task<Entitlement> GetAsync(Guid employeeId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var employee = await employees.FindAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException($"Employee {employeeId} does not exist.");
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Balance opening date is not configured; import the seed data first.");

        var adjustments = await db.Adjustments.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.EffectiveDate <= asOf)
            .ToListAsync(cancellationToken);

        var entries = adjustments
            .Select(a => new EntitlementEntry(
                a.EffectiveDate,
                a.Days,
                a.Kind == AdjustmentKind.OpeningBalance ? EntitlementSource.OpeningBalance : EntitlementSource.Adjustment,
                a.Reason))
            .Concat(EntitlementCalculator.Accruals(employee.HireDate, employee.BirthDate, settings.OpeningDate, asOf)
                .Select(a => new EntitlementEntry(a.Anniversary, a.Days, EntitlementSource.Accrual, $"{a.SeniorityYears}. yıl hak edişi")))
            .OrderBy(e => e.Date)
            .ToList();

        return new Entitlement(entries);
    }
}
