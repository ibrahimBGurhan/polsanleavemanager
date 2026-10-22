using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal.Data;
using PolsanLeave.Identity.Internal.Domain;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Identity.Tests;

[Trait("Category", "Integration")]
public sealed class CurrentEmployeeIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabase _database = new();
    private readonly ISignedInUserAccessor _signedInUser = Substitute.For<ISignedInUserAccessor>();
    private ServiceProvider _services = null!;
    private Guid _managerId;
    private Guid _employeeId;

    public async ValueTask InitializeAsync()
    {
        _services = new ServiceCollection()
            .AddIdentityModule(_database.ConnectionString)
            .AddSingleton(_signedInUser)
            .BuildServiceProvider();
        await _services.MigrateIdentityModuleAsync(TestContext.Current.CancellationToken);

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = new Employee(Guid.CreateVersion7(), "1000", "Yönetici Kişi", "yonetici@polsan.com.tr", new DateOnly(2015, 3, 1), new DateOnly(1980, 5, 5));
        var employee = new Employee(Guid.CreateVersion7(), "1001", "Ayşe Yılmaz", "ayse@polsan.com.tr", new DateOnly(2020, 6, 15), new DateOnly(1992, 2, 29));
        employee.AssignManager(manager.Id);
        db.Employees.AddRange(manager, employee);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _managerId = manager.Id;
        _employeeId = employee.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Get_FirstSignInByUpn_ReturnsProfileAndBindsObjectId()
    {
        _signedInUser.GetAsync(Arg.Any<CancellationToken>()).Returns(new SignedInUser("oid-ayse", "Ayse@Polsan.com.tr"));

        await using (var scope = _services.CreateAsyncScope())
        {
            var profile = await scope.ServiceProvider.GetRequiredService<ICurrentEmployee>().GetAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(profile);
            Assert.Equal(_employeeId, profile.Id);
            Assert.Equal(_managerId, profile.ManagerId);
            Assert.Equal(new DateOnly(2020, 6, 15), profile.HireDate);
        }

        await using (var scope = _services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Employees
                .SingleAsync(e => e.Id == _employeeId, TestContext.Current.CancellationToken);
            Assert.Equal("oid-ayse", stored.EntraObjectId);
        }
    }

    [Fact]
    public async Task Get_NoMatchingEmployee_ReturnsNull()
    {
        _signedInUser.GetAsync(Arg.Any<CancellationToken>()).Returns(new SignedInUser("oid-stranger", "stranger@polsan.com.tr"));

        await using var scope = _services.CreateAsyncScope();

        Assert.Null(await scope.ServiceProvider.GetRequiredService<ICurrentEmployee>().GetAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_NotSignedIn_ReturnsNull()
    {
        _signedInUser.GetAsync(Arg.Any<CancellationToken>()).Returns((SignedInUser?)null);

        await using var scope = _services.CreateAsyncScope();

        Assert.Null(await scope.ServiceProvider.GetRequiredService<ICurrentEmployee>().GetAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_KnownId_ReturnsProfile()
    {
        await using var scope = _services.CreateAsyncScope();

        var profile = await scope.ServiceProvider.GetRequiredService<IEmployeeDirectory>().FindAsync(_managerId, TestContext.Current.CancellationToken);

        Assert.Equal("1000", profile?.EmployeeNumber);
        Assert.Null(profile?.ManagerId);
    }
}
