namespace AtajaMe.Domain.Updates;

public sealed record UpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string TagName,
    string ReleaseNotes,
    string? DownloadUrl,
    string ReleasePageUrl,
    bool IsUpdateAvailable);
