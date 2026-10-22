using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;
using PolsanLeave.TestInfrastructure;
using PolsanLeave.Web.Security;

namespace PolsanLeave.Web.Tests;

/// <summary>Service-layer role check: only Employee-role users reach the modules (AC-2, docs/security.md).</summary>
public class ClaimsSignedInUserAccessorTests
{
    private static Task<PolsanLeave.Identity.Contracts.SignedInUser?> Resolve(ClaimsPrincipal principal)
    {
        var provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(new AuthenticationState(principal));
        return new ClaimsSignedInUserAccessor(provider).GetAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_EmployeeRole_ReturnsObjectIdAndUpn()
    {
        var user = await Resolve(PolsanLeaveAppFactory.Ayse.ToPrincipal());

        Assert.Equal(new PolsanLeave.Identity.Contracts.SignedInUser("oid-ayse", "ayse.demir@ornek.polsan.test"), user);
    }

    [Fact]
    public async Task Get_WithoutEmployeeRole_ReturnsNull() =>
        Assert.Null(await Resolve(PolsanLeaveAppFactory.WithoutRole.ToPrincipal()));

    [Fact]
    public async Task Get_Anonymous_ReturnsNull() =>
        Assert.Null(await Resolve(new ClaimsPrincipal(new ClaimsIdentity())));
}
