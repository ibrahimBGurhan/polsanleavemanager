using PolsanLeave.Web.Components;
using PolsanLeave.Web.Composition;
using PolsanLeave.Web.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(ModuleRegistration.ConnectionStringName)
    ?? throw new InvalidOperationException($"Connection string '{ModuleRegistration.ConnectionStringName}' is not configured.");
builder.Services.AddPolsanLeaveModules(connectionString);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (args is ["--import-seed", var seedPath])
{
    return await SeedImport.RunCommandAsync(app.Services, seedPath, Console.Out);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
return 0;
