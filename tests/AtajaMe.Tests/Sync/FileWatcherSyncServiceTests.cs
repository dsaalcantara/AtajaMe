using System.IO;
using AtajaMe.Infrastructure.Sync;

namespace AtajaMe.Tests.Sync;

public class FileWatcherSyncServiceTests : IDisposable
{
    private readonly string _tempSyncDir;

    public FileWatcherSyncServiceTests()
    {
        _tempSyncDir = Path.Combine(Path.GetTempPath(), $"AtajaMe_WatcherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSyncDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempSyncDir))
            {
                Directory.Delete(_tempSyncDir, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void StartAndStop_ManagesRunningStateCleanly()
    {
        using var watcherService = new FileWatcherSyncService(_tempSyncDir, debounceMilliseconds: 100);

        Assert.False(watcherService.IsRunning);

        watcherService.Start();
        Assert.True(watcherService.IsRunning);

        watcherService.Stop();
        Assert.False(watcherService.IsRunning);
    }

    [Fact]
    public async Task FileChanged_TriggersRemoteManifestChangedEventWithDebounce()
    {
        using var watcherService = new FileWatcherSyncService(_tempSyncDir, debounceMilliseconds: 150);
        var tcs = new TaskCompletionSource<bool>();

        watcherService.RemoteManifestChanged += () =>
        {
            tcs.TrySetResult(true);
            return Task.CompletedTask;
        };

        watcherService.Start();

        var manifestFile = Path.Combine(_tempSyncDir, "sync_manifest.json");
        await File.WriteAllTextAsync(manifestFile, "{\"Version\": 1}");

        // Esperar a que se dispare el evento debounced (margen holgado para runners CI)
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.Same(tcs.Task, completedTask);
        Assert.True(await tcs.Task);

        watcherService.Stop();
    }
}
