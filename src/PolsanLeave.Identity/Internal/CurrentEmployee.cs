using Microsoft.EntityFrameworkCore;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal.Data;
using PolsanLeave.Identity.Internal.Domain;

namespace PolsanLeave.Identity.Internal;

/// <summary>
/// Resolves the signed-in Entra user to an employee record, binding the <c>oid</c> on first sign-in.
/// Resolved once per scope; concurrent callers in the same scope (a Blazor circuit) share that one resolution.
/// Only a successful resolution is kept: a failed or cancelled one is retried by the next caller (review 1, F-6).
/// </summary>
internal sealed class CurrentEmployee(ISignedInUserAccessor signedInUser, IDbContextFactory<IdentityDbContext> contexts) : ICurrentEmployee
{
    private Task<EmployeeProfile?>? _resolution;

    public async Task<EmployeeProfile?> GetAsync(CancellationToken cancellationToken = default)
    {
        var resolution = _resolution ??= ResolveAsync(cancellationToken);
        try
        {
            return await resolution;
        }
        catch
        {
            if (ReferenceEquals(_resolution, resolution))
            {
                _resolution = null;
            }

            throw;
        }
    }

    private async Task<EmployeeProfile?> ResolveAsync(CancellationToken cancellationToken)
    {
        var user = await signedInUser.GetAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
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

internal sealed class EmployeeDirectory(IDbContextFactory<IdentityDbContext> contexts) : IEmployeeDirectory
{
    public async Task<EmployeeProfile?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        return employee?.ToProfile();
    }
}

internal static class EmployeeMapping
{
    public static EmployeeProfile ToProfile(this Employee e) =>
        new(e.Id, e.EmployeeNumber, e.FullName, e.HireDate, e.BirthDate, e.ManagerId);
}
