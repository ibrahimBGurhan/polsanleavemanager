using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Calendar.Contracts;
using PolsanLeave.Calendar.Internal;
using PolsanLeave.Calendar.Internal.Data;

namespace PolsanLeave.Calendar;

/// <summary>Composition entry point of the Calendar module (docs/architecture.md).</summary>
public static class CalendarModule
{
    public static IServiceCollection AddCalendarModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CalendarDbContext>(o => Configure(o, connectionString));
        services.AddScoped<IWorkingDayService, WorkingDayService>();
        return services;
    }

    public static async Task MigrateCalendarModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CalendarDbContext>().Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CalendarDbContext.Schema));
}
