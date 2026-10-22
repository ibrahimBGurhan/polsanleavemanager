using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.SharedKernel;
using PolsanLeave.TestInfrastructure;
using static Microsoft.Playwright.Assertions;

namespace PolsanLeave.E2E;

/// <summary>The app with a request service whose submit fails unexpectedly (e.g. lock timeout, DB outage).</summary>
public sealed class FailingSubmitApp : E2EApp
{
    protected override Action<IServiceCollection>? OverrideServices => services =>
    {
        var original = services.Single(d => d.ServiceType == typeof(ILeaveRequestService));
        services.Remove(original);
        services.AddScoped<ILeaveRequestService>(sp =>
            new FailingSubmit((ILeaveRequestService)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!)));
    };

    private sealed class FailingSubmit(ILeaveRequestService inner) : ILeaveRequestService
    {
        public Task<Result<BalanceSummary>> GetSummaryAsync(CancellationToken cancellationToken = default) => inner.GetSummaryAsync(cancellationToken);

        public Task<Result<IReadOnlyList<LeaveRequestItem>>> GetMineAsync(CancellationToken cancellationToken = default) => inner.GetMineAsync(cancellationToken);

        public Task<Result<LeavePreview>> PreviewAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            inner.PreviewAsync(startDate, endDate, cancellationToken);

        public Task<Result<LeaveRequestItem>> SubmitAsync(SubmitLeaveRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom-secret-detail");
    }
}

/// <summary>AC-38 on the interactive form (review 1, F-1): generic Turkish message and a correlation id.</summary>
[Trait("Category", "E2E")]
public sealed class UnexpectedErrorTests(FailingSubmitApp app) : IClassFixture<FailingSubmitApp>
{
    [Fact]
    public async Task FormSubmit_UnexpectedError_ShowsGenericMessageAndCorrelationId()
    {
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Ayse);

        await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-02");
        await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-03");
        await page.GetByTestId("submit").ClickAsync();

        await Expect(page.GetByTestId("correlation-id")).ToHaveTextAsync(new Regex("^[0-9a-f]{32}$"));
        await Expect(page.GetByRole(Microsoft.Playwright.AriaRole.Alert)).ToContainTextAsync("Beklenmeyen bir hata oluştu.");
        await Expect(page.Locator("body")).Not.ToContainTextAsync("boom-secret-detail");
        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
