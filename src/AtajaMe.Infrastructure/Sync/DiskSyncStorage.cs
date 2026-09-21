using System.IO;
using System.Text.Json;
using AtajaMe.Application.Sync.DTOs;
using AtajaMe.Application.Sync.Services;

namespace AtajaMe.Infrastructure.Sync;

/// <summary>
/// Implementación de ISyncStorage para lectura y escritura atómica de sync_manifest.json en disco o nube (RF-22, RF-23).
/// </summary>
public sealed class DiskSyncStorage : ISyncStorage
{
    private const string ManifestFileName = "sync_manifest.json";
    private readonly string _syncDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiskSyncStorage(string syncDirectory)
    {
        _syncDirectory = syncDirectory ?? throw new ArgumentNullException(nameof(syncDirectory));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public bool IsAvailable()
    {
        try
        {
            return Directory.Exists(_syncDirectory);
        }
        catch
        {
            return false;
        }
    }

    public string GetSyncDirectory() => _syncDirectory;

    public async Task<SyncManifestDto?> ReadManifestAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            return null;
        }

        var filePath = Path.Combine(_syncDirectory, ManifestFileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        // Reintentos con retardo breve por si el cliente de nube (Drive/OneDrive) mantiene un lock transitorio
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return await JsonSerializer.DeserializeAsync<SyncManifestDto>(stream, _jsonOptions, cancellationToken);
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        return null;
    }

    public async Task WriteManifestAsync(SyncManifestDto manifest, CancellationToken cancellationToken = default)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (!Directory.Exists(_syncDirectory))
        {
            Directory.CreateDirectory(_syncDirectory);
        }

        var finalPath = Path.Combine(_syncDirectory, ManifestFileName);
        var tempPath = Path.Combine(_syncDirectory, $"{ManifestFileName}.tmp.{Guid.NewGuid():N}");

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, _jsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            // Reemplazo atómico
            File.Move(tempPath, finalPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // No bloquear
                }
            }
        }
    }
}
