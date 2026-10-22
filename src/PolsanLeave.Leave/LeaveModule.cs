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
        services.AddDbContext<LeaveDbContext>(o => Configure(o, connectionString));
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        return services;
    }

    public static async Task MigrateLeaveModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LeaveDbContext>().Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", LeaveDbContext.Schema));
}
