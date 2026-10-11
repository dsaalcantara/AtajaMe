namespace AtajaMe.Domain.Updates;

public interface IUpdateChecker
{
    Task<UpdateInfo?> CheckForUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default);
}
