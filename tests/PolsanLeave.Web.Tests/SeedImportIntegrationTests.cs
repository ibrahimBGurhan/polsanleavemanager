using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.TestInfrastructure;
using PolsanLeave.Web.Composition;
using PolsanLeave.Web.Seeding;

namespace PolsanLeave.Web.Tests;

/// <summary>The fictional development seed imports through the modules' Contracts and is idempotent.</summary>
[Trait("Category", "Integration")]
public sealed class SeedImportIntegrationTests : IAsyncLifetime
{
    private static readonly string DevSeedPath = Path.Combine(AppContext.BaseDirectory, "seed", "dev-seed.json");

    private readonly TestDatabase _database = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _services = new ServiceCollection().AddPolsanLeaveModules(_database.ConnectionString).BuildServiceProvider();
        await _services.MigratePolsanLeaveModulesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Import_DevSeed_CreatesEmployeesManagersAndOpeningBalances()
    {
        var seed = await SeedImport.ReadAsync(DevSeedPath, TestContext.Current.CancellationToken);

        var result = await SeedImport.ImportAsync(_services, seed, TestContext.Current.CancellationToken);

        Assert.Equal(6, result.Employees);
        Assert.Equal(6, result.Balances.Written);

        await using var scope = _services.CreateAsyncScope();
        var ids = await scope.ServiceProvider.GetRequiredService<IEmployeeImporter>().UpsertAsync([], TestContext.Current.CancellationToken);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Import_Twice_IsIdempotent()
    {
        var seed = await SeedImport.ReadAsync(DevSeedPath, TestContext.Current.CancellationToken);
        var first = await SeedImport.ImportAsync(_services, seed, TestContext.Current.CancellationToken);

        var second = await SeedImport.ImportAsync(_services, seed, TestContext.Current.CancellationToken);

        Assert.Equal(new BalanceSeedReport(0, 6, []), second.Balances with { Unchanged = [] });
        Assert.Empty(second.Balances.Unchanged);
        Assert.Equal(first.Employees, second.Employees);
    }

    [Fact]
    public async Task Import_ManagerAndOpeningBalance_AreReadableThroughContracts()
    {
        var seed = await SeedImport.ReadAsync(DevSeedPath, TestContext.Current.CancellationToken);
        await SeedImport.ImportAsync(_services, seed, TestContext.Current.CancellationToken);

        await using var scope = _services.CreateAsyncScope();
        var ids = await scope.ServiceProvider.GetRequiredService<IEmployeeImporter>().UpsertAsync(
            seed.Employees.Select(e => new EmployeeSeed(e.EmployeeNumber, e.FullName, e.UserPrincipalName, e.HireDate, e.BirthDate, e.ManagerEmployeeNumber)).ToList(),
            TestContext.Current.CancellationToken);
        var ayse = await scope.ServiceProvider.GetRequiredService<IEmployeeDirectory>().FindAsync(ids["1002"], TestContext.Current.CancellationToken);
        var opening = await scope.ServiceProvider.GetRequiredService<IEntitlementService>().GetAsync(ids["1002"], seed.OpeningDate, TestContext.Current.CancellationToken);

        Assert.Equal(ids["1001"], ayse?.ManagerId);
        Assert.Equal(9, opening.TotalDays);
    }

    [Fact]
    public async Task Import_DifferentOpeningDate_IsRefused()
    {
        var seed = await SeedImport.ReadAsync(DevSeedPath, TestContext.Current.CancellationToken);
        await SeedImport.ImportAsync(_services, seed, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SeedImport.ImportAsync(_services, seed with { OpeningDate = seed.OpeningDate.AddDays(1) }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Import_ManagerNotInSeed_IsRefused()
    {
        var seed = await SeedImport.ReadAsync(DevSeedPath, TestContext.Current.CancellationToken);
        var broken = seed with { Employees = [.. seed.Employees.Take(1).Select(e => e with { ManagerEmployeeNumber = "9999" })] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => SeedImport.ImportAsync(_services, broken, TestContext.Current.CancellationToken));
    }
}
