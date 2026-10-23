using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal;
using PolsanLeave.Identity.Internal.Data;

namespace PolsanLeave.Identity;

/// <summary>Composition entry point of the Identity module. The host registers an <see cref="ISignedInUserAccessor"/> and an IBusinessClock.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
        // A factory, not a scoped context: a Blazor circuit is one long scope whose handlers interleave (plan 0001, D9).
        services.AddDbContextFactory<IdentityDbContext>(o => Configure(o, connectionString));
        services.AddScoped<ICurrentEmployee, CurrentEmployee>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IEmployeeImporter, EmployeeImporter>();
        return services;
    }

    public static async Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema));
}
