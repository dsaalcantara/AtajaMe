using Microsoft.Data.Sqlite;

namespace AtajaMe.Infrastructure.Persistence;

/// <summary>
/// Contrato para la creación y apertura de conexiones a la base de datos SQLite.
/// </summary>
public interface ISqliteConnectionFactory
{
    /// <summary>
    /// Crea una nueva conexión SQLite configurada.
    /// </summary>
    SqliteConnection CreateConnection();

    /// <summary>
    /// Crea y abre una nueva conexión SQLite aplicando los pragmas de rendimiento y seguridad.
    /// </summary>
    Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
