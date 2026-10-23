using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Balance.Internal.Data;
using PolsanLeave.Balance.Internal.Domain;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Balance.Tests;

[Trait("Category", "Integration")]
public sealed class EntitlementServiceIntegrationTests : IAsyncLifetime
{
    private static readonly DateOnly Opening = new(2026, 1, 1);
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();
    private readonly IEmployeeDirectory _employees = Substitute.For<IEmployeeDirectory>();
    private readonly Guid _employeeId = Guid.CreateVersion7();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _services = new ServiceCollection()
            .AddBalanceModule(_database.ConnectionString)
            .AddSingleton(_employees)
            .BuildServiceProvider();
        await _services.MigrateBalanceModuleAsync(TestContext.Current.CancellationToken);

        _employees.FindAsync(_employeeId, Arg.Any<CancellationToken>())
            .Returns(new EmployeeProfile(_employeeId, "1001", "Ayşe Yılmaz", new DateOnly(2020, 6, 15), new DateOnly(1992, 3, 3), null));

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();
        db.Settings.Add(new BalanceSettings(Opening));
        db.Adjustments.Add(BalanceAdjustment.OpeningBalance(Guid.CreateVersion7(), _employeeId, Opening, 10, Now));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Get_OpeningBalancePlusAnniversaryAccrual_EveryEntryHasSource()
    {
        await using var scope = _services.CreateAsyncScope();

        var entitlement = await scope.ServiceProvider.GetRequiredService<IEntitlementService>()
            .GetAsync(_employeeId, new DateOnly(2026, 10, 22), TestContext.Current.CancellationToken);

        // Opening 10 + 6th anniversary on 2026-06-15 (seniority 6, age 34) = 20.
        Assert.Equal(30, entitlement.TotalDays);
        Assert.Collection(entitlement.Entries,
            e => Assert.Equal((EntitlementSource.OpeningBalance, 10, "Açılış bakiyesi"), (e.Source, e.Days, e.Reason)),
            e => Assert.Equal((EntitlementSource.Accrual, 20, new DateOnly(2026, 6, 15)), (e.Source, e.Days, e.Date)));
    }

    [Fact]
    public async Task Get_BeforeAnniversary_OnlyOpeningBalance()
    {
        await using var scope = _services.CreateAsyncScope();

        var entitlement = await scope.ServiceProvider.GetRequiredService<IEntitlementService>()
            .GetAsync(_employeeId, new DateOnly(2026, 6, 14), TestContext.Current.CancellationToken);

        Assert.Equal(10, entitlement.TotalDays);
    }

    [Fact]
    public async Task Adjustment_RequiresReason_AppendOnly()
    {
        Assert.Throws<ArgumentException>(() =>
            new BalanceAdjustment(Guid.CreateVersion7(), _employeeId, Opening, 2, AdjustmentKind.Manual, " ", Now));

        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();
            db.Adjustments.Remove(db.Adjustments.Single(a => a.EmployeeId == _employeeId));
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        // Changing a recorded adjustment is refused too (verify 1, V-3).
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BalanceDbContext>();
            db.Entry(db.Adjustments.Single(a => a.EmployeeId == _employeeId)).Property(a => a.Days).CurrentValue = 99;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        await using (var scope = _services.CreateAsyncScope())
        {
            var stored = scope.ServiceProvider.GetRequiredService<BalanceDbContext>().Adjustments.Single(a => a.EmployeeId == _employeeId);
            Assert.Equal(10, stored.Days);
        }
    }
}
