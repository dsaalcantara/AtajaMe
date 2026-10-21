using Microsoft.Data.Sqlite;

namespace AtajaMe.Infrastructure.Persistence;

/// <summary>
/// Fábrica de conexiones SQLite que garantiza la configuración de modo WAL, foreign keys y tiempos de espera.
/// </summary>
public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;
    private readonly bool _isMemoryDb;
    private int _walInitialized;

    public SqliteConnectionFactory(string? connectionString = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = Path.Combine(appData, "AtajaMe");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var dbPath = Path.Combine(directory, "atajame.db");
            _connectionString = $"Data Source={dbPath};Cache=Shared";
        }
        else if (!connectionString.Contains('='))
        {
            _connectionString = $"Data Source={connectionString};Cache=Shared";
        }
        else
        {
            _connectionString = connectionString;
        }

        _isMemoryDb = _connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase);
    }

    public static SqliteConnectionFactory CreateInMemory()
    {
        return new SqliteConnectionFactory("Data Source=:memory:;Mode=Memory;Cache=Shared");
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        if (!_isMemoryDb && Interlocked.Exchange(ref _walInitialized, 1) == 0)
        {
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = 5000;
                PRAGMA synchronous = NORMAL;
                PRAGMA cache_size = -512;
            ";
        }
        else
        {
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = 5000;
                PRAGMA synchronous = NORMAL;
                PRAGMA cache_size = -512;
            ";
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }
}
