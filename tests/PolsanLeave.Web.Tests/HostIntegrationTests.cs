using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Web.Tests;

/// <summary>The real host: sign-in, roles, page states and ownership (spec 0001, AC-1 … AC-5).</summary>
[Trait("Category", "Integration")]
public sealed class HostIntegrationTests : IAsyncLifetime
{
    private readonly PolsanLeaveAppFactory _app = new();

    public async ValueTask InitializeAsync() => await _app.InitializeDatabaseAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    [Theory]
    [InlineData("/")]
    [InlineData("/izin/yeni")]
    [InlineData("/erisim-yok")]
    public async Task AnonymousRequest_AnyPage_RedirectsToEntraLogin(string path)
    {
        var response = await Client(null).GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(PolsanLeaveAppFactory.EntraAuthorizeEndpoint, response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/izin/yeni")]
    public async Task AuthenticatedWithoutEmployeeRole_Home_ShowsAccessDenied(string path)
    {
        var client = Client(PolsanLeaveAppFactory.WithoutRole);

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/erisim-yok", response.Headers.Location?.AbsolutePath ?? response.Headers.Location?.OriginalString.Split('?')[0]);

        var page = await PageAsync(client, response.Headers.Location!.ToString());
        Assert.Contains("Bu uygulamayı kullanma yetkiniz yok.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"balance\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Employee_Home_ShowsLegalBalanceAndEmptyList()
    {
        // Ayşe Demir: opening 9 + 5th anniversary on 2026-02-01 (age 31) → 14; no requests.
        var page = await PageAsync(Client(PolsanLeaveAppFactory.Ayse), "/");

        Assert.Contains("data-testid=\"balance-remaining\">23 gün<", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"balance-pending\">0 gün<", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"balance-available\">23 gün<", page, StringComparison.Ordinal);
        Assert.Contains("Henüz izin talebiniz yok.", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoMatchingEmployee_Home_ShowsProfileMissing_NoRequestButton()
    {
        var page = await PageAsync(Client(PolsanLeaveAppFactory.Stranger), "/");

        Assert.Contains("Profiliniz tanımlı değil, İK ile iletişime geçin.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"new-request\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OtherEmployeesRequest_NotReachableByAnyRoute()
    {
        var created = await SubmitAsAsync(PolsanLeaveAppFactory.Ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3), "özel açıklama"));

        var hasanHome = await PageAsync(Client(PolsanLeaveAppFactory.Hasan), "/");
        var byId = await Client(PolsanLeaveAppFactory.Hasan).GetAsync($"/izin/{created.Id}", TestContext.Current.CancellationToken);
        var ayseHome = await PageAsync(Client(PolsanLeaveAppFactory.Ayse), "/");

        Assert.Contains("Henüz izin talebiniz yok.", hasanHome, StringComparison.Ordinal);
        Assert.DoesNotContain("02.11.2026", hasanHome, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.Contains("02.11.2026", ayseHome, StringComparison.Ordinal);
        Assert.Contains("Beklemede", ayseHome, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnhandledException_ShowsGenericMessageAndCorrelationId_NoStackTrace()
    {
        var failing = NSubstitute.Substitute.For<ILeaveRequestService>();
        NSubstitute.SubstituteExtensions.Returns(
            failing.GetSummaryAsync(NSubstitute.Arg.Any<CancellationToken>()),
            _ => Task.FromException<PolsanLeave.SharedKernel.Result<BalanceSummary>>(new InvalidOperationException("boom-secret-detail")));
        var logs = new CapturingLoggerProvider();
        await using var app = new PolsanLeaveAppFactory { OverrideServices = s => s.AddScoped(_ => failing).AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(logs) };
        await app.InitializeDatabaseAsync(TestContext.Current.CancellationToken);
        var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $"{TestAuthHandler.CookieName}={PolsanLeaveAppFactory.Ayse.ToCookieValue()}");

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var page = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Beklenmeyen bir hata oluştu.", page, StringComparison.Ordinal);
        Assert.Matches("data-testid=\"correlation-id\">[0-9a-f]{32}<", page);
        Assert.DoesNotContain("boom-secret-detail", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", page, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", page, StringComparison.Ordinal);

        // Support finds the id the user reports in a log line, together with the exception (review 1, F-8).
        var correlationId = System.Text.RegularExpressions.Regex.Match(page, "data-testid=\"correlation-id\">([0-9a-f]{32})<").Groups[1].Value;
        Assert.Contains(logs.Entries, e => e.Message.Contains(correlationId, StringComparison.Ordinal) && e.Exception?.Message == "boom-secret-detail");
    }

    /// <summary>The page as the user reads it: Blazor HTML-encodes non-ASCII text (ı → &amp;#x131;).</summary>
    private static async Task<string> PageAsync(HttpClient client, string path) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(path, TestContext.Current.CancellationToken));

    private HttpClient Client(TestUser? user)
    {
        var client = _app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (user is not null)
        {
            client.DefaultRequestHeaders.Add("Cookie", $"{TestAuthHandler.CookieName}={user.ToCookieValue()}");
        }

        return client;
    }

    private async Task<LeaveRequestItem> SubmitAsAsync(TestUser user, SubmitLeaveRequest request)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        ((IHostEnvironmentAuthenticationStateProvider)scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>())
            .SetAuthenticationState(Task.FromResult(new AuthenticationState(user.ToPrincipal())));

        var result = await scope.ServiceProvider.GetRequiredService<ILeaveRequestService>().SubmitAsync(request, TestContext.Current.CancellationToken);
        return result.Value;
    }
}
