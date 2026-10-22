using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal;
using PolsanLeave.Identity.Internal.Data;

namespace PolsanLeave.Identity;

/// <summary>Composition entry point of the Identity module. The host registers an <see cref="ISignedInUserAccessor"/>.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(o => Configure(o, connectionString));
        services.AddScoped<ICurrentEmployee, CurrentEmployee>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        return services;
    }

    public static async Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(cancellationToken);
    }

    internal static void Configure(DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema));
}
