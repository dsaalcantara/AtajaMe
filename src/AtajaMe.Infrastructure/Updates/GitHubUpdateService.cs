using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AtajaMe.Application.Updates;
using AtajaMe.Domain.Updates;

namespace AtajaMe.Infrastructure.Updates;

public sealed partial class GitHubUpdateService : IUpdateChecker, IUpdateDownloader
{
    private const string RepositoryOwner = "dsaalcantara";
    private const string RepositoryName = "AtajaMe";
    private const string LatestReleaseApiUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
    private const string DefaultReleasePageUrl = $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases";

    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AtajaMe-App");
        }
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var releaseData = await JsonSerializer.DeserializeAsync<GitHubReleasePayload>(
                jsonStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (releaseData == null || string.IsNullOrWhiteSpace(releaseData.TagName))
            {
                return null;
            }

            var latestVersion = ParseVersionFromTag(releaseData.TagName);
            if (latestVersion == null)
            {
                return null;
            }

            var isUpdateAvailable = latestVersion > currentVersion;

            string? installerDownloadUrl = null;
            if (releaseData.Assets != null)
            {
                var installerAsset = releaseData.Assets.FirstOrDefault(a =>
                    !string.IsNullOrWhiteSpace(a.BrowserDownloadUrl) &&
                    (a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true ||
                     a.Name?.Contains("Setup", StringComparison.OrdinalIgnoreCase) == true));

                installerDownloadUrl = installerAsset?.BrowserDownloadUrl;
            }

            return new UpdateInfo(
                CurrentVersion: currentVersion,
                LatestVersion: latestVersion,
                TagName: releaseData.TagName,
                ReleaseNotes: releaseData.Body ?? string.Empty,
                DownloadUrl: installerDownloadUrl,
                ReleasePageUrl: !string.IsNullOrWhiteSpace(releaseData.HtmlUrl) ? releaseData.HtmlUrl : DefaultReleasePageUrl,
                IsUpdateAvailable: isUpdateAvailable);
        }
        catch
        {
            // Resiliencia: si no hay conexión a internet o la cuota se agota, retornamos null de forma silenciosa
            return null;
        }
    }

    public async Task<string> DownloadInstallerAsync(
        string downloadUrl,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new ArgumentException("La URL de descarga no puede ser nula ni vacía.", nameof(downloadUrl));
        }

        var destinationFolder = Path.Combine(Path.GetTempPath(), "AtajaMeUpdates");
        Directory.CreateDirectory(destinationFolder);
        var targetFile = Path.Combine(destinationFolder, "AtajaMe_Setup.exe");

        if (File.Exists(targetFile))
        {
            try { File.Delete(targetFile); } catch { /* Ignore */ }
        }

        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percentage = (double)totalBytesRead / totalBytes;
                progress.Report(percentage);
            }
        }

        progress?.Report(1.0);
        return targetFile;
    }

    public static Version? ParseVersionFromTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        var cleaned = CleanTagRegex().Replace(tag.Trim(), string.Empty);
        return Version.TryParse(cleaned, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"^[^\d]+")]
    private static partial Regex CleanTagRegex();

    private sealed class GitHubReleasePayload
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
