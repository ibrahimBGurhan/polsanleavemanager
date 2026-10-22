namespace PolsanLeave.Identity.Contracts;

/// <summary>An employee as other modules see it (docs/domain.md, Çalışan).</summary>
public sealed record EmployeeProfile(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    DateOnly HireDate,
    DateOnly BirthDate,
    Guid? ManagerId);

/// <summary>The authenticated Entra user, as supplied by the host (Web). Never taken from user input.</summary>
public sealed record SignedInUser(string EntraObjectId, string UserPrincipalName);
