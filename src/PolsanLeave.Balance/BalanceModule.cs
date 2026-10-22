using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Balance.Internal;
using PolsanLeave.Balance.Internal.Data;

namespace PolsanLeave.Balance;

/// <summary>Composition entry point of the Balance module. Requires the Identity module (IEmployeeDirectory) and an IBusinessClock.</summary>
public static class BalanceModule
{
    public static IServiceCollection AddBalanceModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BalanceDbContext>(o => Configure(o, connectionString));
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IBalanceSeedImporter, BalanceSeedImporter>();
        return services;
    }

    public static async Task MigrateBalanceModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BalanceDbContext>().Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", BalanceDbContext.Schema));
}
