using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal;
using PolsanLeave.Leave.Internal.Data;

namespace PolsanLeave.Leave;

/// <summary>Composition entry point of the Leave module. Requires the Identity, Balance and Calendar modules and an IBusinessClock.</summary>
public static class LeaveModule
{
    public static IServiceCollection AddLeaveModule(this IServiceCollection services, string connectionString)
    {
        // A factory, not a scoped context: a Blazor circuit is one long scope whose handlers interleave (plan 0001, D9).
        services.AddDbContextFactory<LeaveDbContext>(o => Configure(o, connectionString));
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        return services;
    }

    public static async Task MigrateLeaveModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<LeaveDbContext>>().CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", LeaveDbContext.Schema));
}
