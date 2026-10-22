using System.Text.Json;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Web.Composition;

namespace PolsanLeave.Web.Seeding;

/// <summary>
/// HR's start-up data (spec 0001, R-2/R-3/R-4; plan 0001, Risk 4): opening date, employees, opening
/// balances. Real files hold personal data and live outside the repository (<c>seed/*.local.json</c>).
/// </summary>
internal sealed record SeedFile(DateOnly OpeningDate, IReadOnlyList<SeedEmployee> Employees);

internal sealed record SeedEmployee(
    string EmployeeNumber,
    string FullName,
    string UserPrincipalName,
    DateOnly HireDate,
    DateOnly BirthDate,
    string? ManagerEmployeeNumber,
    int OpeningBalanceDays);

internal sealed record SeedImportResult(int Employees, BalanceSeedReport Balances);

/// <summary>Runs an idempotent import: employees first (Identity), then opening date and balances (Balance).</summary>
internal static class SeedImport
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<SeedFile> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SeedFile>(stream, Json, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file {path} is empty.");
    }

    public static async Task<SeedImportResult> ImportAsync(IServiceProvider services, SeedFile seed, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var ids = await scope.ServiceProvider.GetRequiredService<IEmployeeImporter>().UpsertAsync(
            seed.Employees.Select(e => new EmployeeSeed(e.EmployeeNumber, e.FullName, e.UserPrincipalName, e.HireDate, e.BirthDate, e.ManagerEmployeeNumber)).ToList(),
            cancellationToken);

        var balances = await scope.ServiceProvider.GetRequiredService<IBalanceSeedImporter>().ImportAsync(
            seed.OpeningDate,
            seed.Employees.Select(e => new OpeningBalanceSeed(ids[e.EmployeeNumber], e.OpeningBalanceDays)).ToList(),
            cancellationToken);

        return new SeedImportResult(ids.Count, balances);
    }

    /// <summary><c>PolsanLeave.Web --import-seed &lt;path&gt;</c>: migrate, import, report, exit.</summary>
    public static async Task<int> RunCommandAsync(IServiceProvider services, string path, TextWriter output)
    {
        try
        {
            var seed = await ReadAsync(path);
            await services.MigratePolsanLeaveModulesAsync();
            var result = await ImportAsync(services, seed);
            await output.WriteLineAsync($"Seed imported: {result.Employees} employees; opening balances written {result.Balances.Written}, already present {result.Balances.AlreadyPresent}.");
            if (result.Balances.Unchanged.Count > 0)
            {
                await output.WriteLineAsync($"Opening balances kept (seed differs; correct with an adjustment): {string.Join(", ", result.Balances.Unchanged)}");
            }

            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException or IOException)
        {
            await output.WriteLineAsync("Seed import failed: " + ex.Message);
            return 1;
        }
    }
}
