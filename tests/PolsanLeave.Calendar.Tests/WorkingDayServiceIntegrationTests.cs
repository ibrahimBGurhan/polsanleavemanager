using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Calendar.Contracts;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Calendar.Tests;

/// <summary>The migrated database carries the 2026–2027 calendar (real SQL Server).</summary>
[Trait("Category", "Integration")]
public sealed class WorkingDayServiceIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabase _database = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _services = new ServiceCollection().AddCalendarModule(_database.ConnectionString).BuildServiceProvider();
        await _services.MigrateCalendarModuleAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task CountWorkingDays_Oct26To30_2026_FromDatabase_Is4()
    {
        await using var scope = _services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkingDayService>();

        var result = await service.CountWorkingDaysAsync(new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 30), TestContext.Current.CancellationToken);

        Assert.Equal(4, result.Value);
    }

    [Fact]
    public async Task CountWorkingDays_RangeInto2028_FromDatabase_IsNotDefined()
    {
        await using var scope = _services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkingDayService>();

        var result = await service.CountWorkingDaysAsync(new DateOnly(2027, 12, 30), new DateOnly(2028, 1, 3), TestContext.Current.CancellationToken);

        Assert.Equal(CalendarErrors.NotDefined, result.Error);
    }
}
