using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using PolsanLeave.Identity.Contracts;

namespace PolsanLeave.Web.Security;

/// <summary>Entra ID App Roles (docs/security.md). Manager is derived from the reporting line, not a role.</summary>
internal static class Roles
{
    public const string Employee = "Employee";
    public const string HrAdmin = "HrAdmin";
}

internal static class Policies
{
    public const string Employee = "Employee";
}

internal static class Authentication
{
    public const string AccessDeniedPath = "/erisim-yok";

    /// <summary>Entra ID sign-in (OIDC) with default-deny authorization (docs/security.md, AuthN / AuthZ).</summary>
    public static IServiceCollection AddPolsanLeaveAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));
        services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, o => o.AccessDeniedPath = AccessDeniedPath);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Employee, p => p.RequireAuthenticatedUser().RequireRole(Roles.Employee));

        services.AddControllersWithViews().AddMicrosoftIdentityUI();
        services.AddCascadingAuthenticationState();
        services.AddScoped<ISignedInUserAccessor, ClaimsSignedInUserAccessor>();
        return services;
    }
}

/// <summary>
/// The signed-in Entra user for the modules. A user without the Employee role is treated as not signed in,
/// so services refuse even if a page forgot its policy (docs/security.md: authorization in the service layer).
/// </summary>
internal sealed class ClaimsSignedInUserAccessor(AuthenticationStateProvider authenticationState) : ISignedInUserAccessor
{
    public const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public async Task<SignedInUser?> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = (await authenticationState.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole(Roles.Employee))
        {
            return null;
        }

        var objectId = user.FindFirstValue(ObjectIdClaim) ?? user.FindFirstValue("oid");
        var upn = user.FindFirstValue("preferred_username") ?? user.FindFirstValue(ClaimTypes.Upn);
        return objectId is null || upn is null ? null : new SignedInUser(objectId, upn);
    }
}
