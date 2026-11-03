using System.Net.Http.Headers;
using System.Text.Json;

namespace TrazBase.App;

internal sealed record ReleaseUpdate(string Version, Uri InstallerUri);

internal static class GitHubReleaseUpdater
{
    private static readonly HttpClient Client = CreateClient();
    private const string ReleaseApi = "https://api.github.com/repos/BrunoPasseti21/Traz-base/releases/latest";

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TrazBase", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public static async Task<ReleaseUpdate?> CheckAsync(Version currentVersion)
    {
        using var response = await Client.GetAsync(ReleaseApi);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var release = await JsonDocument.ParseAsync(stream);
        var root = release.RootElement;
        var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var latest) || latest <= currentVersion) return null;

        if (!root.TryGetProperty("assets", out var assets)) return null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString();
            var url = asset.GetProperty("browser_download_url").GetString();
            if (name is not null && name.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(url, UriKind.Absolute, out var installerUri))
                return new ReleaseUpdate(latest.ToString(), installerUri);
        }
        return null;
    }

    public static async Task<string> DownloadInstallerAsync(Uri uri, IProgress<int>? progress = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"TrazBase-Setup-{Guid.NewGuid():N}.exe");
        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var totalBytes = response.Content.Headers.ContentLength;
        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloadedBytes += bytesRead;
            if (totalBytes is > 0)
                progress?.Report((int)Math.Clamp(downloadedBytes * 100 / totalBytes.Value, 0, 100));
        }
        return path;
    }
}
