using System.Globalization;
using PolsanLeave.Web.Components;
using PolsanLeave.Web.Composition;
using PolsanLeave.Web.Security;
using PolsanLeave.Web.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(ModuleRegistration.ConnectionStringName)
    ?? throw new InvalidOperationException($"Connection string '{ModuleRegistration.ConnectionStringName}' is not configured.");
builder.Services.AddPolsanLeaveModules(connectionString);
builder.Services.AddPolsanLeaveAuthentication(builder.Configuration);
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (args is ["--import-seed", var seedPath])
{
    return await SeedImport.RunCommandAsync(app.Services, seedPath, Console.Out);
}

// The UI is Turkish only (docs/conventions.md, Language); dates and numbers format as tr-TR.
var turkish = CultureInfo.GetCultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkish;
CultureInfo.DefaultThreadCurrentUICulture = turkish;
app.UseRequestLocalization(turkish.Name);

if (app.Environment.IsDevelopment())
{
    // Development only: schema is created on start-up; deployment applies migrations separately (plan 0001, Risk 7).
    await app.Services.MigratePolsanLeaveModulesAsync();
}
else
{
    // Unexpected errors: generic Turkish page with a correlation id, never a stack trace (AC-38).
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
return 0;
