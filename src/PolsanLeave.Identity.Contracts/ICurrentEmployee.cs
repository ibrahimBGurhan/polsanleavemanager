namespace PolsanLeave.Identity.Contracts;

/// <summary>
/// The employee behind the current request. Modules derive "who" from here — never from an ID passed
/// in by the UI (docs/security.md, authorization in the service layer).
/// </summary>
public interface ICurrentEmployee
{
    /// <summary>The signed-in employee's profile, or <c>null</c> when no employee record matches (spec 0001, R-2).</summary>
    Task<EmployeeProfile?> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Implemented by the host: reads the authenticated Entra user (oid, UPN) of the current request.</summary>
public interface ISignedInUserAccessor
{
    Task<SignedInUser?> GetAsync(CancellationToken cancellationToken = default);
}

public interface IEmployeeDirectory
{
    Task<EmployeeProfile?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
