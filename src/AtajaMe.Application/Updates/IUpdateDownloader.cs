namespace AtajaMe.Application.Updates;

public interface IUpdateDownloader
{
    Task<string> DownloadInstallerAsync(
        string downloadUrl,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
