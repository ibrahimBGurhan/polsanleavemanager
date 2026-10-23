using PolsanLeave.Identity.Internal.Domain;

namespace PolsanLeave.Identity.Tests;

public class EmployeeMatcherTests
{
    private static Employee NewEmployee(string number, string upn, string? boundObjectId = null)
    {
        var employee = new Employee(Guid.CreateVersion7(), number, "Test Kişi", upn, new DateOnly(2020, 1, 1), new DateOnly(1990, 1, 1));
        if (boundObjectId is not null)
        {
            employee.BindEntraAccount(boundObjectId);
        }

        return employee;
    }

    [Fact]
    public void Resolve_NoMatchingEmployee_ReturnsProfileMissing()
    {
        var match = EmployeeMatcher.Match([NewEmployee("1001", "ayse@polsan.com.tr")], "oid-x", "mehmet@polsan.com.tr");

        Assert.Null(match);
    }

    [Fact]
    public void Match_BoundObjectId_WinsEvenIfUpnChanged()
    {
        var bound = NewEmployee("1001", "ayse@polsan.com.tr", "oid-1");

        Assert.Same(bound, EmployeeMatcher.Match([bound], "oid-1", "ayse.yeni@polsan.com.tr"));
    }

    [Fact]
    public void Match_UnboundRecord_ByUpnCaseInsensitive()
    {
        var unbound = NewEmployee("1001", "ayse@polsan.com.tr");

        Assert.Same(unbound, EmployeeMatcher.Match([unbound], "oid-1", "  Ayse@Polsan.com.TR "));
    }

    [Fact]
    public void Match_UpnOfRecordBoundToAnotherAccount_ReturnsNull()
    {
        var boundElsewhere = NewEmployee("1001", "ayse@polsan.com.tr", "oid-original");

        Assert.Null(EmployeeMatcher.Match([boundElsewhere], "oid-intruder", "ayse@polsan.com.tr"));
    }
}

public class EmployeeTests
{
    [Fact]
    public void BindEntraAccount_AlreadyBoundToAnother_Throws()
    {
        var employee = new Employee(Guid.CreateVersion7(), "1001", "Ayşe", "ayse@polsan.com.tr", new DateOnly(2020, 1, 1), new DateOnly(1990, 1, 1));
        employee.BindEntraAccount("oid-1");

        Assert.Throws<InvalidOperationException>(() => employee.BindEntraAccount("oid-2"));
    }

    [Fact]
    public void AssignManager_Self_Throws()
    {
        var employee = new Employee(Guid.CreateVersion7(), "1001", "Ayşe", "ayse@polsan.com.tr", new DateOnly(2020, 1, 1), new DateOnly(1990, 1, 1));

        Assert.Throws<InvalidOperationException>(() => employee.AssignManager(employee.Id));
    }
}
