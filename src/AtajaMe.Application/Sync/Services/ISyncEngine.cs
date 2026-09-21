using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Domain.Entities;
using AtajaMe.Domain.Enums;

namespace AtajaMe.Application.Sync.Services;

/// <summary>
/// Contrato del motor de sincronización (RF-22 a RF-24).
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Exporta el estado actual de la base de datos local y escribe el paquete sync_manifest.json en la carpeta remota.
    /// </summary>
    Task<SyncManifestDto> ExportManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa el paquete sync_manifest.json de la carpeta compartida, fusiona cambios y detecta conflictos (RF-23, RF-24).
    /// </summary>
    Task<SyncResult> ProcessRemoteManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resuelve un conflicto específico según la elección del usuario (KeepLocal, KeepRemote, DuplicateBoth) (RF-24).
    /// </summary>
    Task ResolveConflictAsync(SyncConflict conflict, SyncResolutionChoice choice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la lista de conflictos pendientes de resolución.
    /// </summary>
    Task<IReadOnlyList<SyncConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default);
}
