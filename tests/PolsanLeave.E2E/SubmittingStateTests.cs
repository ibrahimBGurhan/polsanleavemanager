using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.SharedKernel;
using PolsanLeave.TestInfrastructure;
using static Microsoft.Playwright.Assertions;

namespace PolsanLeave.E2E;

/// <summary>The app with a request service whose submit waits until the test releases it.</summary>
public sealed class HeldSubmitApp : E2EApp
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _submitCalls;

    public int SubmitCalls => Volatile.Read(ref _submitCalls);

    public void Release() => _release.TrySetResult();

    protected override Action<IServiceCollection>? OverrideServices => services =>
    {
        var original = services.Single(d => d.ServiceType == typeof(ILeaveRequestService));
        services.Remove(original);
        services.AddScoped<ILeaveRequestService>(sp =>
            new HeldSubmit((ILeaveRequestService)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!), this));
    };

    private sealed class HeldSubmit(ILeaveRequestService inner, HeldSubmitApp app) : ILeaveRequestService
    {
        public Task<Result<BalanceSummary>> GetSummaryAsync(CancellationToken cancellationToken = default) => inner.GetSummaryAsync(cancellationToken);

        public Task<Result<IReadOnlyList<LeaveRequestItem>>> GetMineAsync(CancellationToken cancellationToken = default) => inner.GetMineAsync(cancellationToken);

        public Task<Result<LeavePreview>> PreviewAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            inner.PreviewAsync(startDate, endDate, cancellationToken);

        public async Task<Result<LeaveRequestItem>> SubmitAsync(SubmitLeaveRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref app._submitCalls);
            await app._release.Task.WaitAsync(cancellationToken);
            return await inner.SubmitAsync(request, cancellationToken);
        }
    }
}

/// <summary>
/// The form's "gönderiliyor" state (spec 0001, User experience; AC-33; verify 1, V-1): while a submit is in
/// flight the button is disabled and says so, and a second click does not send a second submit.
/// </summary>
[Trait("Category", "E2E")]
public sealed class SubmittingStateTests(HeldSubmitApp app) : IClassFixture<HeldSubmitApp>
{
    [Fact]
    public async Task SubmitInProgress_ButtonDisabled_ShowsSubmitting()
    {
        var page = await app.OpenFormAsAsync(PolsanLeaveAppFactory.Ayse);

        try
        {
            await page.GetByLabel("Başlangıç tarihi").FillAsync("2026-11-02");
            await page.GetByLabel("Bitiş tarihi").FillAsync("2026-11-03");
            await page.GetByTestId("submit").DblClickAsync();

            var submit = page.GetByTestId("submit");
            await Expect(submit).ToBeDisabledAsync();
            await Expect(submit).ToHaveTextAsync("Gönderiliyor…");
            Assert.Equal(1, app.SubmitCalls);
        }
        finally
        {
            app.Release();
        }

        await Expect(page.GetByTestId("created")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("request-row")).ToHaveCountAsync(1);
        Assert.Equal(1, app.SubmitCalls);
    }
}
