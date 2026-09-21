namespace AtajaMe.Domain.Entities;

/// <summary>
/// Metadatos locales del estado de sincronización de la máquina.
/// </summary>
public sealed class SyncMetadata
{
    public string MachineId { get; }
    public DateTime LastSyncTimestamp { get; private set; }
    public string LastSyncHash { get; private set; }

    public SyncMetadata(string machineId, DateTime lastSyncTimestamp, string lastSyncHash)
    {
        if (string.IsNullOrWhiteSpace(machineId))
        {
            throw new ArgumentException("El MachineId no puede ser nulo ni vacío.", nameof(machineId));
        }

        MachineId = machineId;
        LastSyncTimestamp = lastSyncTimestamp;
        LastSyncHash = lastSyncHash ?? string.Empty;
    }

    public static SyncMetadata CreateDefault(string? machineId = null)
    {
        var id = string.IsNullOrWhiteSpace(machineId) ? Guid.NewGuid().ToString("N") : machineId;
        return new SyncMetadata(id, DateTime.UnixEpoch, string.Empty);
    }

    public void Update(DateTime timestampUtc, string manifestHash)
    {
        LastSyncTimestamp = timestampUtc;
        LastSyncHash = manifestHash ?? string.Empty;
    }
}
