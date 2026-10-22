using Microsoft.Data.SqlClient;

namespace PolsanLeave.TestInfrastructure;

/// <summary>
/// A uniquely named SQL Server database for one test class (docs/testing.md, Integration veritabanı).
/// Server: <c>POLSANLEAVE_TEST_SQL</c> (CI) or the dedicated LocalDB instance <c>PolsanLeaveTest</c>.
/// The database is created by the modules' migrations and dropped on dispose.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public const string ServerVariable = "POLSANLEAVE_TEST_SQL";

    private const string LocalDbServer = @"Server=(localdb)\PolsanLeaveTest;Integrated Security=true;TrustServerCertificate=True";

    public TestDatabase()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(server) ? LocalDbServer : server)
        {
            InitialCatalog = "PolsanLeaveTest_" + Guid.NewGuid().ToString("N"),
        };
        ConnectionString = builder.ConnectionString;
        DatabaseName = builder.InitialCatalog;
    }

    public string ConnectionString { get; }

    public string DatabaseName { get; }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // DatabaseName is generated from a GUID above, never from input.
        command.CommandText = $"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
