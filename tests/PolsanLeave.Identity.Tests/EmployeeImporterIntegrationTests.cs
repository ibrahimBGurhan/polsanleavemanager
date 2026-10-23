using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.SharedKernel;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Identity.Tests;

/// <summary>Seed import against data already in the database (review 1, F-7).</summary>
[Trait("Category", "Integration")]
public sealed class EmployeeImporterIntegrationTests : IAsyncLifetime
{
    private static readonly EmployeeSeed Ayse = new("1001", "Ayşe Demir", "ayse@ornek.polsan.test", new DateOnly(2021, 2, 1), new DateOnly(1994, 11, 5), null);
    private static readonly EmployeeSeed Burak = new("1002", "Burak Koç", "burak@ornek.polsan.test", new DateOnly(2016, 9, 12), new DateOnly(1985, 6, 20), null);

    private readonly TestDatabase _database = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 6, 0, 0, TimeSpan.Zero));
        _services = new ServiceCollection()
            .AddIdentityModule(_database.ConnectionString)
            .AddSingleton(clock)
            .AddSingleton(Substitute.For<ISignedInUserAccessor>())
            .BuildServiceProvider();
        await _services.MigrateIdentityModuleAsync(TestContext.Current.CancellationToken);
        await ImportAsync(Ayse, Burak);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Upsert_UpnOfAnotherStoredEmployee_IsRefusedWithMessage()
    {
        var newcomer = new EmployeeSeed("1003", "Can Öztürk", "AYSE@ornek.polsan.test", new DateOnly(2024, 2, 29), new DateOnly(1999, 1, 10), null);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ImportAsync(newcomer));

        Assert.Contains("ayse@ornek.polsan.test", error.Message, StringComparison.Ordinal);
        Assert.Contains("1001", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upsert_SwappedUpns_IsRefusedWithMessage()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ImportAsync(Ayse with { UserPrincipalName = Burak.UserPrincipalName }, Burak with { UserPrincipalName = Ayse.UserPrincipalName }));

        Assert.Contains("burak@ornek.polsan.test", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upsert_SameSeedAgain_Succeeds() =>
        Assert.Equal(2, (await ImportAsync(Ayse, Burak)).Count);

    private async Task<IReadOnlyDictionary<string, Guid>> ImportAsync(params EmployeeSeed[] employees)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IEmployeeImporter>().UpsertAsync(employees, TestContext.Current.CancellationToken);
    }
}
