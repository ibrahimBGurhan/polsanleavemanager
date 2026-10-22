using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.E2E;

/// <summary>
/// Owns the app-under-test lifecycle (docs/testing.md): the real host on a free Kestrel port and one
/// headless Chromium, started for the test class and stopped on dispose — success, failure or timeout.
/// </summary>
public class E2EApp : IAsyncLifetime
{
    private PolsanLeaveAppFactory _factory = null!;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    public string BaseUrl { get; private set; } = "";

    public PolsanLeaveAppFactory Factory => _factory;

    /// <summary>Service overrides for a fixture variant (e.g. a failing service), applied after the defaults.</summary>
    protected virtual Action<IServiceCollection>? OverrideServices => null;

    public async ValueTask InitializeAsync()
    {
        _factory = new PolsanLeaveAppFactory { OverrideServices = OverrideServices };
        _factory.UseKestrel(0);
        _factory.StartServer();
        await _factory.InitializeDatabaseAsync();
        BaseUrl = _factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>A fresh browser context signed in as <paramref name="user"/> (Turkish locale).</summary>
    public async Task<IPage> OpenAsAsync(TestUser user, string path = "/")
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl, Locale = "tr-TR" });
        await context.AddCookiesAsync([new Cookie { Name = TestAuthHandler.CookieName, Value = user.ToCookieValue(), Url = BaseUrl }]);
        var page = await context.NewPageAsync();
        await page.GotoAsync(path);
        return page;
    }

    /// <summary>The request form, once its Blazor circuit is live.</summary>
    public async Task<IPage> OpenFormAsAsync(TestUser user)
    {
        var page = await OpenAsAsync(user, "/izin/yeni");
        await page.Locator("[data-testid=leave-form][data-ready=true]").WaitForAsync();
        return page;
    }
}
