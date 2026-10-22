using Microsoft.EntityFrameworkCore;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal.Data;
using PolsanLeave.Identity.Internal.Domain;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Identity.Internal;

internal sealed class EmployeeImporter(IdentityDbContext db, IBusinessClock clock) : IEmployeeImporter
{
    public async Task<IReadOnlyDictionary<string, Guid>> UpsertAsync(IReadOnlyList<EmployeeSeed> employees, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employees);
        Validate(employees);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var numbers = employees.Select(e => e.EmployeeNumber).ToList();
        var existing = await db.Employees.Where(e => numbers.Contains(e.EmployeeNumber)).ToDictionaryAsync(e => e.EmployeeNumber, cancellationToken);

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
