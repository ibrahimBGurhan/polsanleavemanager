using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Identity.Internal;
using PolsanLeave.Identity.Internal.Data;

namespace PolsanLeave.Identity.Tests;

/// <summary>Resolution caching within one scope (a Blazor circuit) — review 1, F-6.</summary>
public class CurrentEmployeeTests
{
    private readonly ISignedInUserAccessor _signedInUser = Substitute.For<ISignedInUserAccessor>();
    private readonly UnusedContextFactory _contexts = new();

    [Fact]
    public async Task Get_FirstResolutionFails_NextCallRetriesInsteadOfReplayingTheError()
    {
        _signedInUser.GetAsync(Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<SignedInUser?>(new TimeoutException("transient")),
            _ => Task.FromResult<SignedInUser?>(null));
        var current = new CurrentEmployee(_signedInUser, _contexts);

        await Assert.ThrowsAsync<TimeoutException>(() => current.GetAsync(TestContext.Current.CancellationToken));

        Assert.Null(await current.GetAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_SuccessfulResolution_IsReusedWithinTheScope()
    {
        _signedInUser.GetAsync(Arg.Any<CancellationToken>()).Returns((SignedInUser?)null);
        var current = new CurrentEmployee(_signedInUser, _contexts);

        await current.GetAsync(TestContext.Current.CancellationToken);
        await current.GetAsync(TestContext.Current.CancellationToken);

        await _signedInUser.Received(1).GetAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>These tests never reach the database (no signed-in user); any use is a test bug.</summary>
internal sealed class UnusedContextFactory : IDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext() => throw new InvalidOperationException("The database is not used in these tests.");
}
