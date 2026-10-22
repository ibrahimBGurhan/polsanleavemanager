using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PolsanLeave.TestInfrastructure;

/// <summary>
/// Test-only sign-in (FD-6: never in production assemblies). The user comes from the
/// <see cref="CookieName"/> cookie — a cookie, so Blazor's WebSocket circuit carries it too (E2E).
/// No cookie → no result, and the real Entra (OIDC) challenge runs.
/// </summary>
public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string CookieName = "PolsanLeave.TestUser";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(Request.Cookies.TryGetValue(CookieName, out var value) && TestUser.Parse(value) is { } user
            ? AuthenticateResult.Success(new AuthenticationTicket(user.ToPrincipal(), SchemeName))
            : AuthenticateResult.NoResult());
}

/// <summary>A signed-in test user; serialized into the <see cref="TestAuthHandler.CookieName"/> cookie.</summary>
public sealed record TestUser(string ObjectId, string UserPrincipalName, string Name, params string[] Roles)
{
    public static TestUser Employee(string objectId, string upn, string name) => new(objectId, upn, name, "Employee");

    /// <summary>The claims Entra issues for the app (oid, preferred_username, name, roles).</summary>
    public ClaimsPrincipal ToPrincipal()
    {
        var claims = new List<Claim>
        {
            new("http://schemas.microsoft.com/identity/claims/objectidentifier", ObjectId),
            new("preferred_username", UserPrincipalName),
            new("name", Name),
        };
        claims.AddRange(Roles.Select(r => new Claim("roles", r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, TestAuthHandler.SchemeName, "name", "roles"));
    }

    public string ToCookieValue() => Uri.EscapeDataString(string.Join('|', ObjectId, UserPrincipalName, Name, string.Join(',', Roles)));

    public static TestUser? Parse(string cookieValue)
    {
        var parts = Uri.UnescapeDataString(cookieValue).Split('|');
        return parts.Length == 4
            ? new TestUser(parts[0], parts[1], parts[2], parts[3].Split(',', StringSplitOptions.RemoveEmptyEntries))
            : null;
    }
}
