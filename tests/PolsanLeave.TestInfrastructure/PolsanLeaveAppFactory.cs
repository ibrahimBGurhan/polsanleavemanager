using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PolsanLeave.Web.Composition;
using PolsanLeave.Web.Seeding;

namespace PolsanLeave.TestInfrastructure;

/// <summary>
/// The real Web host for host and E2E tests: own database with the fictional dev seed, a fixed clock
/// (Thursday 2026-10-22, 09:00 Istanbul), test sign-in via cookie, and a network-free Entra challenge.
/// Environment "Testing": production error handling (AC-38), no start-up migration.
/// </summary>
public class PolsanLeaveAppFactory : WebApplicationFactory<Program>
{
    public const string EntraAuthorizeEndpoint = "https://login.microsoftonline.com/00000000-0000-0000-0000-00000000aaaa/oauth2/v2.0/authorize";

    /// <summary>Employees of seed/dev-seed.json (all Employee role; numbers in comments).</summary>
    public static readonly TestUser Selin = TestUser.Employee("oid-selin", "selin.arslan@ornek.polsan.test", "Selin Arslan"); // 1000, no manager
    public static readonly TestUser Ayse = TestUser.Employee("oid-ayse", "ayse.demir@ornek.polsan.test", "Ayşe Demir"); // 1002, opening 9
    public static readonly TestUser Hasan = TestUser.Employee("oid-hasan", "hasan.yildiz@ornek.polsan.test", "Hasan Yıldız"); // 1005, opening 22
    public static readonly TestUser Stranger = TestUser.Employee("oid-stranger", "stranger@ornek.polsan.test", "Yabancı Kişi");
    public static readonly TestUser WithoutRole = new("oid-norole", "burak.koc@ornek.polsan.test", "Burak Koç");

    public TestDatabase Database { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 22, 6, 0, 0, TimeSpan.Zero));

    /// <summary>Extra service overrides applied after the defaults (e.g. a failing service for AC-38).</summary>
    public Action<IServiceCollection>? OverrideServices { get; init; }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await Services.MigratePolsanLeaveModulesAsync(cancellationToken);
        var seed = await SeedImport.ReadAsync(Path.Combine(AppContext.BaseDirectory, "seed", "dev-seed.json"), cancellationToken);
        await SeedImport.ImportAsync(Services, seed, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await Database.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PolsanLeave", Database.ConnectionString);
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-00000000aaaa");
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-00000000bbbb");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
            services.PostConfigure<AuthenticationOptions>(o => o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
            services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = EntraAuthorizeEndpoint,
                    Issuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-00000000aaaa/v2.0",
                };
                o.Configuration = configuration;
                o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

            OverrideServices?.Invoke(services);
        });
    }
}
