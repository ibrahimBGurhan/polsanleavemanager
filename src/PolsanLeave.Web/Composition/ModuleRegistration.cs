using PolsanLeave.Balance;
using PolsanLeave.Calendar;
using PolsanLeave.Identity;
using PolsanLeave.Leave;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Web.Composition;

/// <summary>Composition root: wires every module (docs/architecture.md) and the shared clock (FD-5).</summary>
internal static class ModuleRegistration
{
    public const string ConnectionStringName = "PolsanLeave";

    public static IServiceCollection AddPolsanLeaveModules(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();
        services.AddCalendarModule(connectionString);
        services.AddIdentityModule(connectionString);
        services.AddBalanceModule(connectionString);
        services.AddLeaveModule(connectionString);
        return services;
    }

    public static async Task MigratePolsanLeaveModulesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await services.MigrateCalendarModuleAsync(cancellationToken);
        await services.MigrateIdentityModuleAsync(cancellationToken);
        await services.MigrateBalanceModuleAsync(cancellationToken);
        await services.MigrateLeaveModuleAsync(cancellationToken);
    }
}
