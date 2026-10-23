using Microsoft.EntityFrameworkCore;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal.Data;
using PolsanLeave.Identity.Internal.Domain;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Identity.Internal;

internal sealed class EmployeeImporter(IDbContextFactory<IdentityDbContext> contexts, IBusinessClock clock) : IEmployeeImporter
{
    public async Task<IReadOnlyDictionary<string, Guid>> UpsertAsync(IReadOnlyList<EmployeeSeed> employees, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employees);
        Validate(employees);

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var numbers = employees.Select(e => e.EmployeeNumber).ToList();
        var existing = await db.Employees.Where(e => numbers.Contains(e.EmployeeNumber)).ToDictionaryAsync(e => e.EmployeeNumber, cancellationToken);
        await EnsureUpnsAreFreeAsync(db, employees, cancellationToken);

        foreach (var seed in employees)
        {
            if (existing.TryGetValue(seed.EmployeeNumber, out var employee))
            {
                employee.UpdateMasterData(seed.FullName, seed.UserPrincipalName, seed.HireDate, seed.BirthDate);
            }
            else
            {
                employee = new Employee(Guid.CreateVersion7(clock.UtcNow), seed.EmployeeNumber, seed.FullName, seed.UserPrincipalName, seed.HireDate, seed.BirthDate);
                db.Employees.Add(employee);
                existing.Add(seed.EmployeeNumber, employee);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var seed in employees)
        {
            existing[seed.EmployeeNumber].AssignManager(seed.ManagerEmployeeNumber is { } manager ? existing[manager].Id : null);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return existing.ToDictionary(p => p.Key, p => p.Value.Id);
    }

    /// <summary>
    /// A UPN stored for another employee number (a reused address, or two employees swapping addresses)
    /// would break the unique index; refuse it with a message the operator can act on (review 1, F-7).
    /// </summary>
    private static async Task EnsureUpnsAreFreeAsync(IdentityDbContext db, IReadOnlyList<EmployeeSeed> employees, CancellationToken cancellationToken)
    {
        var numberByUpn = employees.ToDictionary(e => Employee.NormalizeUpn(e.UserPrincipalName), e => e.EmployeeNumber);
        var upns = numberByUpn.Keys.ToList();
        var holders = await db.Employees.AsNoTracking()
            .Where(e => upns.Contains(e.UserPrincipalName))
            .Select(e => new { e.UserPrincipalName, e.EmployeeNumber })
            .ToListAsync(cancellationToken);

        var conflicts = holders
            .Where(h => numberByUpn[h.UserPrincipalName] != h.EmployeeNumber)
            .Select(h => $"UPN {h.UserPrincipalName} belongs to stored employee {h.EmployeeNumber}, not {numberByUpn[h.UserPrincipalName]}")
            .ToList();
        if (conflicts.Count > 0)
        {
            throw new InvalidOperationException(
                "Invalid employee seed: " + string.Join("; ", conflicts) + ". Free the UPN first (change the holder's UPN in a separate import).");
        }
    }

    private static void Validate(IReadOnlyList<EmployeeSeed> employees)
    {
        var problems = new List<string>();
        var numbers = employees.Select(e => e.EmployeeNumber).ToHashSet();

        problems.AddRange(employees.GroupBy(e => e.EmployeeNumber).Where(g => g.Count() > 1).Select(g => $"duplicate employee number {g.Key}"));
        problems.AddRange(employees.GroupBy(e => Employee.NormalizeUpn(e.UserPrincipalName)).Where(g => g.Count() > 1).Select(g => $"duplicate UPN {g.Key}"));
        foreach (var e in employees)
        {
            if (string.IsNullOrWhiteSpace(e.EmployeeNumber) || string.IsNullOrWhiteSpace(e.FullName) || string.IsNullOrWhiteSpace(e.UserPrincipalName))
            {
                problems.Add($"employee {e.EmployeeNumber}: number, name and UPN are required");
            }

            if (e.ManagerEmployeeNumber is { } manager && (manager == e.EmployeeNumber || !numbers.Contains(manager)))
            {
                problems.Add($"employee {e.EmployeeNumber}: manager {manager} is not another employee in the seed");
            }

            if (e.BirthDate >= e.HireDate)
            {
                problems.Add($"employee {e.EmployeeNumber}: birth date must precede hire date");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("Invalid employee seed: " + string.Join("; ", problems));
        }
    }
}
