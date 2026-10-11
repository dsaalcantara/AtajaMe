using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace AtajaMe.Infrastructure.Persistence;

/// <summary>
/// Inicializador del esquema de base de datos SQLite para AtajaMe.
/// Crea las tablas e índices necesarios si no existen.
/// </summary>
public sealed class SqliteDatabaseInitializer
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseInitializer(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await InitializeWithConnectionAsync(connection, cancellationToken);
    }

    public static async Task InitializeWithConnectionAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        const string schemaSql = @"
            -- Carpetas (Jerarquía de hasta 3 niveles)
            CREATE TABLE IF NOT EXISTS Folders (
                Id TEXT PRIMARY KEY,
                ParentId TEXT NULL,
                Name TEXT NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ParentId) REFERENCES Folders(Id) ON DELETE CASCADE
            );

            -- Atajos de texto
            CREATE TABLE IF NOT EXISTS Snippets (
                Id TEXT PRIMARY KEY,
                FolderId TEXT NULL,
                Code TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Content TEXT NOT NULL,
                CaseSensitive INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                IsFavorite INTEGER NOT NULL DEFAULT 0,
                IsInTrash INTEGER NOT NULL DEFAULT 0,
                DeletedAt TEXT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                UsageCount INTEGER NOT NULL DEFAULT 0,
                LastUsedAt TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (FolderId) REFERENCES Folders(Id) ON DELETE SET NULL
            );

            -- Aplicaciones excluidas
            CREATE TABLE IF NOT EXISTS ExcludedApplications (
                Id TEXT PRIMARY KEY,
                ProcessName TEXT NOT NULL UNIQUE,
                Description TEXT NULL,
                CreatedAt TEXT NOT NULL
            );

            -- Configuración general de la aplicación
            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            -- Metadatos de Sincronización
            CREATE TABLE IF NOT EXISTS SyncMetadata (
                MachineId TEXT PRIMARY KEY,
                LastSyncTimestamp TEXT NOT NULL,
                LastSyncHash TEXT NOT NULL
            );

            -- Índices para optimización de consultas
            CREATE INDEX IF NOT EXISTS IX_Snippets_Code ON Snippets(Code);
            CREATE INDEX IF NOT EXISTS IX_Snippets_FolderId ON Snippets(FolderId);
            CREATE INDEX IF NOT EXISTS IX_Snippets_IsFavorite ON Snippets(IsFavorite);
            CREATE INDEX IF NOT EXISTS IX_Snippets_IsInTrash ON Snippets(IsInTrash);
            CREATE INDEX IF NOT EXISTS IX_Snippets_LastUsedAt ON Snippets(LastUsedAt);
            CREATE INDEX IF NOT EXISTS IX_Folders_ParentId ON Folders(ParentId);
            CREATE INDEX IF NOT EXISTS IX_ExcludedApplications_ProcessName ON ExcludedApplications(ProcessName);
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Migración de datos: reemplazar cualquier prefijo '@' existente por 'c'
        await using var migrateCommand = connection.CreateCommand();
        migrateCommand.CommandText = @"
            UPDATE Snippets 
            SET Code = 'c' || SUBSTR(Code, 2),
                UpdatedAt = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
            WHERE Code LIKE '@%';
        ";
        await migrateCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
