using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class GitHubReleaseUpdaterTests
{
    private static readonly Version Current = new(0, 3, 0);

    [TestMethod]
    public void ParseLatestRelease_ReturnsUpdateWhenNewerVersionHasSetupAsset()
    {
        var json = """
        {
            "tag_name": "v0.4.0",
            "assets": [
                { "name": "TrazBase-Setup.exe", "browser_download_url": "https://github.com/BrunoPasseti21/Traz-base/releases/download/v0.4.0/TrazBase-Setup.exe" },
                { "name": "notas.txt", "browser_download_url": "https://example.com/notas.txt" }
            ]
        }
        """;

        var update = GitHubReleaseUpdater.ParseLatestRelease(json, Current);

        Assert.IsNotNull(update);
        Assert.AreEqual("0.4.0", update.Version);
        Assert.AreEqual(Uri.UriSchemeHttps, update.InstallerUri.Scheme);
        Assert.IsTrue(update.InstallerUri.AbsoluteUri.EndsWith("TrazBase-Setup.exe", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ParseLatestRelease_AcceptsTagWithoutVPrefix()
    {
        var json = ReleaseJson("0.4.0", ("TrazBase-Setup.exe", "https://example.com/TrazBase-Setup.exe"));

        var update = GitHubReleaseUpdater.ParseLatestRelease(json, Current);

        Assert.IsNotNull(update);
        Assert.AreEqual("0.4.0", update.Version);
    }

    [TestMethod]
    public void ParseLatestRelease_MatchesSetupExtensionCaseInsensitively()
    {
        var json = ReleaseJson("0.4.0", ("TrazBase-SETUP.EXE", "https://example.com/TrazBase-SETUP.EXE"));

        var update = GitHubReleaseUpdater.ParseLatestRelease(json, Current);

        Assert.IsNotNull(update);
    }

    [TestMethod]
    public void ParseLatestRelease_ReturnsNullWhenVersionIsNotNewer()
    {
        var same = ReleaseJson("v0.3.0", ("TrazBase-Setup.exe", "https://example.com/TrazBase-Setup.exe"));
        var older = ReleaseJson("v0.2.9", ("TrazBase-Setup.exe", "https://example.com/TrazBase-Setup.exe"));

        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(same, Current));
        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(older, Current));
    }

    [TestMethod]
    public void ParseLatestRelease_ReturnsNullWhenTagIsMissingOrInvalid()
    {
        var missingTag = """{ "assets": [ { "name": "TrazBase-Setup.exe", "browser_download_url": "https://example.com/TrazBase-Setup.exe" } ] }""";
        var invalidTag = ReleaseJson("versao-nova", ("TrazBase-Setup.exe", "https://example.com/TrazBase-Setup.exe"));

        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(missingTag, Current));
        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(invalidTag, Current));
    }

    [TestMethod]
    public void ParseLatestRelease_ReturnsNullWhenThereIsNoAssetsArray()
    {
        var json = """{ "tag_name": "v9.9.9" }""";

        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(json, Current));
    }

    [TestMethod]
    public void ParseLatestRelease_ReturnsNullWhenNoAssetIsTheSetupInstaller()
    {
        var json = ReleaseJson("v1.0.0", ("TrazBase.exe", "https://example.com/TrazBase.exe"));

        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(json, Current));
    }

    [TestMethod]
    public void ParseLatestRelease_RejectsNonHttpsAssetUrl()
    {
        var json = ReleaseJson("v1.0.0", ("TrazBase-Setup.exe", "http://example.com/TrazBase-Setup.exe"));

        Assert.IsNull(GitHubReleaseUpdater.ParseLatestRelease(json, Current));
    }

    [TestMethod]
    public void ParseLatestRelease_SkipsInvalidUrlsAndKeepsLooking()
    {
        var json = """
        {
            "tag_name": "v1.0.0",
            "assets": [
                { "name": "TrazBase-Setup.exe", "browser_download_url": "::url-invalida::" },
                { "name": "Outro-Setup.exe", "browser_download_url": "https://example.com/Outro-Setup.exe" }
            ]
        }
        """;

        var update = GitHubReleaseUpdater.ParseLatestRelease(json, Current);

        Assert.IsNotNull(update);
        Assert.AreEqual("https://example.com/Outro-Setup.exe", update.InstallerUri.AbsoluteUri);
    }

    private static string ReleaseJson(string tag, params (string Name, string Url)[] assets)
    {
        var assetJson = string.Join(",", assets.Select(asset =>
            $$"""{ "name": "{{asset.Name}}", "browser_download_url": "{{asset.Url}}" }"""));
        return $$"""{ "tag_name": "{{tag}}", "assets": [ {{assetJson}} ] }""";
    }
}
