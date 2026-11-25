using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace TrazBase.App;

internal sealed record ReleaseUpdate(string Version, Uri InstallerUri);

internal static class GitHubReleaseUpdater
{
    private static readonly HttpClient Client = CreateClient();

    // Arquivo estático servido pelo raw.githubusercontent.com: não consome o limite
    // de 60 requisições/hora da API do GitHub e funciona sem autenticação.
    private const string VersionManifestUrl =
        "https://raw.githubusercontent.com/BrunoPasseti21/Traz-base/main/version.json";

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TrazBase", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    /// <summary>
    /// Tag de rebuild compilada no executável. Serve para forçar uma nova atualização quando a
    /// versão precisa ser regenerada sem mudar o número (ex.: correções publicadas na 0.9.7).
    /// </summary>
    public static string? GetCurrentUpdateTag() =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "UpdateTag", StringComparison.OrdinalIgnoreCase))
            ?.Value;

    public static async Task<ReleaseUpdate?> CheckAsync(Version currentVersion, string? currentUpdateTag)
    {
        using var response = await Client.GetAsync(VersionManifestUrl);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return ParseVersionManifest(json, currentVersion, currentUpdateTag);
    }

    /// <summary>
    /// Lê o manifest estático publicado em version.json (atualizado por GitHub Action a cada release).
    /// Uma versão igual à atual ainda é oferecida quando o campo "tag" difere da tag compilada no app.
    /// </summary>
    internal static ReleaseUpdate? ParseVersionManifest(string json, Version currentVersion, string? currentUpdateTag)
    {
        using var manifest = JsonDocument.Parse(json);
        var root = manifest.RootElement;

        if (!root.TryGetProperty("version", out var versionElement)) return null;
        var versionText = versionElement.GetString()?.TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var latest)) return null;

        var manifestTag = root.TryGetProperty("tag", out var tagElement)
            ? tagElement.GetString()?.Trim()
            : null;
        var forcedReupdate = latest == currentVersion
            && !string.IsNullOrEmpty(manifestTag)
            && !string.Equals(manifestTag, currentUpdateTag ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        if (latest < currentVersion || (latest == currentVersion && !forcedReupdate)) return null;

        if (!root.TryGetProperty("installerUrl", out var urlElement)) return null;
        var url = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var installerUri)) return null;
        if (installerUri.Scheme != Uri.UriSchemeHttps) return null;
        if (!installerUri.AbsolutePath.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase)) return null;

        return new ReleaseUpdate(latest.ToString(), installerUri);
    }

    internal static ReleaseUpdate? ParseLatestRelease(string json, Version currentVersion)
    {
        using var release = JsonDocument.Parse(json);
        var root = release.RootElement;
        if (!root.TryGetProperty("tag_name", out var tagElement)) return null;
        var tag = tagElement.GetString()?.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var latest) || latest <= currentVersion) return null;

        if (!root.TryGetProperty("assets", out var assets)) return null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString();
            var url = asset.GetProperty("browser_download_url").GetString();
            if (name is not null && name.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(url, UriKind.Absolute, out var installerUri)
                && installerUri.Scheme == Uri.UriSchemeHttps)
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
