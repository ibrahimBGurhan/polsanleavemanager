namespace PolsanLeave.Identity.Internal.Domain;

/// <summary>Employee master data from HR's seed (spec 0001, R-2). Bound to an Entra account on first sign-in.</summary>
internal sealed class Employee
{
    private Employee()
    {
    }

    public Employee(Guid id, string employeeNumber, string fullName, string userPrincipalName, DateOnly hireDate, DateOnly birthDate)
    {
        Id = id;
        EmployeeNumber = employeeNumber;
        FullName = fullName;
        UserPrincipalName = NormalizeUpn(userPrincipalName);
        HireDate = hireDate;
        BirthDate = birthDate;
    }

    public Guid Id { get; private set; }

    public string EmployeeNumber { get; private set; } = "";

    public string FullName { get; private set; } = "";

    /// <summary>Normalized (trimmed, lower-invariant) UPN — the key HR knows.</summary>
    public string UserPrincipalName { get; private set; } = "";

    /// <summary>Entra object id (<c>oid</c>), set on first sign-in and never changed afterwards.</summary>
    public string? EntraObjectId { get; private set; }

    public DateOnly HireDate { get; private set; }

    public DateOnly BirthDate { get; private set; }

    public Guid? ManagerId { get; private set; }

    public static string NormalizeUpn(string upn) => upn.Trim().ToLowerInvariant();

    public void BindEntraAccount(string entraObjectId)
    {
        if (EntraObjectId is not null && EntraObjectId != entraObjectId)
        {
            throw new InvalidOperationException("Employee is already bound to another Entra account.");
        }

        EntraObjectId = entraObjectId;
    }

    public void AssignManager(Guid? managerId)
    {
        if (managerId == Id)
        {
            throw new InvalidOperationException("An employee cannot be their own manager (BR-6).");
        }

        ManagerId = managerId;
    }

    public void UpdateMasterData(string fullName, string userPrincipalName, DateOnly hireDate, DateOnly birthDate)
    {
        FullName = fullName;
        UserPrincipalName = NormalizeUpn(userPrincipalName);
        HireDate = hireDate;
        BirthDate = birthDate;
    }
}
