using AtajaMe.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace AtajaMe.Tests.Infrastructure;

public abstract class SqliteTestBase : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    protected readonly string ConnectionString;
    protected readonly ISqliteConnectionFactory ConnectionFactory;

    protected SqliteTestBase()
    {
        var dbName = $"test_db_{Guid.NewGuid():N}";
        ConnectionString = $"Data Source=file:{dbName}?mode=memory&cache=shared";
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        ConnectionFactory = new SqliteConnectionFactory(ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await _keepAliveConnection.OpenAsync();
        var initializer = new SqliteDatabaseInitializer(ConnectionFactory);
        await initializer.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        _keepAliveConnection.Close();
        _keepAliveConnection.Dispose();
        GC.SuppressFinalize(this);
    }
}
