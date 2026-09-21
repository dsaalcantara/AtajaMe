using System.Globalization;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Repositories;
using AtajaMe.Infrastructure.Persistence;
using Dapper;

namespace AtajaMe.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite para persistir y consultar los metadatos de sincronización de la máquina.
/// </summary>
public sealed class SyncMetadataRepository : ISyncMetadataRepository
{
    private const string IsoFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SyncMetadataRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<SyncMetadata?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT MachineId, LastSyncTimestamp, LastSyncHash
            FROM SyncMetadata
            LIMIT 1;
        ";

        var row = await connection.QuerySingleOrDefaultAsync<SyncMetadataRecord>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        if (row == null)
        {
            return null;
        }

        DateTime timestamp = DateTime.TryParse(row.LastSyncTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : DateTime.UnixEpoch;

        return new SyncMetadata(row.MachineId, timestamp, row.LastSyncHash);
    }

    public async Task SaveAsync(SyncMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = @"
            INSERT INTO SyncMetadata (MachineId, LastSyncTimestamp, LastSyncHash)
            VALUES (@MachineId, @LastSyncTimestamp, @LastSyncHash)
            ON CONFLICT(MachineId) DO UPDATE SET
                LastSyncTimestamp = excluded.LastSyncTimestamp,
                LastSyncHash = excluded.LastSyncHash;
        ";

        var parameters = new
        {
            metadata.MachineId,
            LastSyncTimestamp = metadata.LastSyncTimestamp.ToString(IsoFormat, CultureInfo.InvariantCulture),
            metadata.LastSyncHash
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private sealed record SyncMetadataRecord(string MachineId, string LastSyncTimestamp, string LastSyncHash);
}
