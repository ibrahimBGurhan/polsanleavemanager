using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Web.Tests;

/// <summary>
/// A Blazor Server circuit is one long-lived DI scope, and its event handlers interleave (e.g. the preview
/// after a date change still running when "Gönder" is clicked). Service calls from one scope must therefore
/// never share a DbContext (plan 0001, D9).
/// </summary>
[Trait("Category", "Integration")]
public sealed class CircuitScopeIntegrationTests : IAsyncLifetime
{
    private readonly PolsanLeaveAppFactory _app = new();

    public async ValueTask InitializeAsync() => await _app.InitializeDatabaseAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task ConcurrentPreviewAndSubmit_InOneCircuitScope_BothComplete()
    {
        await using var circuit = _app.Services.CreateAsyncScope();
        ((IHostEnvironmentAuthenticationStateProvider)circuit.ServiceProvider.GetRequiredService<AuthenticationStateProvider>())
            .SetAuthenticationState(Task.FromResult(new AuthenticationState(PolsanLeaveAppFactory.Ayse.ToPrincipal())));
        var leave = circuit.ServiceProvider.GetRequiredService<ILeaveRequestService>();
        var start = new DateOnly(2026, 11, 2);
        var end = new DateOnly(2026, 11, 3);

        var previews = Enumerable.Range(0, 4).Select(_ => leave.PreviewAsync(start, end, TestContext.Current.CancellationToken)).ToList();
        var submit = leave.SubmitAsync(new SubmitLeaveRequest(start, end, null), TestContext.Current.CancellationToken);
        await Task.WhenAll([.. previews, submit]);
        var submitted = await submit;
        var previewed = await Task.WhenAll(previews);

        Assert.True(submitted.IsSuccess);
        Assert.All(previewed, p => Assert.Equal(2, p.Value.WorkingDays));
    }
}
