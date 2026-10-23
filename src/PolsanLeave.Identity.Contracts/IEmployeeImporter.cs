namespace PolsanLeave.Identity.Contracts;

/// <summary>Loads HR's employee master data (spec 0001, R-2; plan 0001, Risk 4). Idempotent by employee number.</summary>
public interface IEmployeeImporter
{
    /// <summary>Creates or updates every employee, then assigns managers. Returns employee number → id.</summary>
    Task<IReadOnlyDictionary<string, Guid>> UpsertAsync(IReadOnlyList<EmployeeSeed> employees, CancellationToken cancellationToken = default);
}

public sealed record EmployeeSeed(
    string EmployeeNumber,
    string FullName,
    string UserPrincipalName,
    DateOnly HireDate,
    DateOnly BirthDate,
    string? ManagerEmployeeNumber);
