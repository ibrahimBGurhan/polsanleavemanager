namespace PolsanLeave.Identity.Internal.Domain;

/// <summary>
/// Decides which employee record a signed-in Entra user is. Match by bound <c>oid</c> first; otherwise by
/// UPN, but only onto a record not yet bound to another account (a reassigned UPN never takes over a record).
/// </summary>
internal static class EmployeeMatcher
{
    public static Employee? Match(IEnumerable<Employee> candidates, string entraObjectId, string userPrincipalName)
    {
        var list = candidates.ToList();
        var byObjectId = list.FirstOrDefault(e => e.EntraObjectId == entraObjectId);
        if (byObjectId is not null)
        {
            return byObjectId;
        }

        var upn = Employee.NormalizeUpn(userPrincipalName);
        return list.FirstOrDefault(e => e.EntraObjectId is null && e.UserPrincipalName == upn);
    }
}
