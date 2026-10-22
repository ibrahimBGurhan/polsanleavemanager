using Microsoft.EntityFrameworkCore;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal.Data;
using PolsanLeave.Identity.Internal.Domain;

namespace PolsanLeave.Identity.Internal;

/// <summary>Resolves the signed-in Entra user to an employee record, binding the <c>oid</c> on first sign-in.</summary>
internal sealed class CurrentEmployee(ISignedInUserAccessor signedInUser, IdentityDbContext db) : ICurrentEmployee
{
    private EmployeeProfile? _resolved;
    private bool _isResolved;

    public async Task<EmployeeProfile?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_isResolved)
        {
            return _resolved;
        }

        var user = await signedInUser.GetAsync(cancellationToken);
        _resolved = user is null ? null : await ResolveAsync(user, cancellationToken);
        _isResolved = true;
        return _resolved;
    }

    private async Task<EmployeeProfile?> ResolveAsync(SignedInUser user, CancellationToken cancellationToken)
    {
        var upn = Employee.NormalizeUpn(user.UserPrincipalName);
        var candidates = await db.Employees
            .Where(e => e.EntraObjectId == user.EntraObjectId || e.UserPrincipalName == upn)
            .ToListAsync(cancellationToken);

        var employee = EmployeeMatcher.Match(candidates, user.EntraObjectId, user.UserPrincipalName);
        if (employee is null)
        {
            return null;
        }

        if (employee.EntraObjectId is null)
        {
            employee.BindEntraAccount(user.EntraObjectId);
            await db.SaveChangesAsync(cancellationToken);
        }

        return employee.ToProfile();
    }
}

internal sealed class EmployeeDirectory(IdentityDbContext db) : IEmployeeDirectory
{
    public async Task<EmployeeProfile?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        return employee?.ToProfile();
    }
}

internal static class EmployeeMapping
{
    public static EmployeeProfile ToProfile(this Employee e) =>
        new(e.Id, e.EmployeeNumber, e.FullName, e.HireDate, e.BirthDate, e.ManagerId);
}
