using System.Text.Json;
using SupraKit.App;

namespace SupraKit.Tests;

[TestClass]
public sealed class AppSettingsTests
{
    [TestMethod]
    public void TutorialFlags_DefaultToNotCompleted()
    {
        var settings = new AppSettings();

        Assert.IsFalse(settings.TutorialCompleted);
        Assert.IsNull(settings.TutorialVersionShown);
        Assert.IsFalse(settings.IsFreshInstall);
    }

    [TestMethod]
    public void ShouldShowTutorial_FirstInstallIgnoresBackgroundPreference()
    {
        var settings = new AppSettings
        {
            IsFreshInstall = true,
            TutorialCompleted = false,
            StartInBackground = true
        };

        Assert.IsTrue(settings.ShouldShowTutorial(new Version(0, 10, 0)));
    }

    [TestMethod]
    public void ShouldShowTutorial_UpdateToIntroVersionOnce()
    {
        var settings = new AppSettings
        {
            IsFreshInstall = false,
            TutorialCompleted = true,
            StartInBackground = true
        };
        var intro = Version.Parse(AppSettings.TutorialIntroVersion);

        Assert.IsTrue(settings.ShouldShowTutorial(intro));

        settings.TutorialVersionShown = AppSettings.TutorialIntroVersion;
        Assert.IsFalse(settings.ShouldShowTutorial(intro));
        Assert.IsFalse(settings.ShouldShowTutorial(new Version(intro.Major, intro.Minor, intro.Build + 1)));
    }

    [TestMethod]
    public void TutorialFlags_RoundtripThroughJson()
    {
        var settings = new AppSettings
        {
            TutorialCompleted = true,
            TutorialVersionShown = AppSettings.TutorialIntroVersion
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json)!;

        Assert.IsTrue(restored.TutorialCompleted);
        Assert.AreEqual(AppSettings.TutorialIntroVersion, restored.TutorialVersionShown);
        Assert.IsFalse(json.Contains("IsFreshInstall", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GetFirebirdPassword_ReturnsNullWhenNoPasswordWasSaved()
    {
        var settings = new AppSettings();

        Assert.IsNull(settings.GetFirebirdPassword());
        Assert.IsNull(settings.ProtectedFirebirdPassword);
    }

    [TestMethod]
    public void FirebirdPassword_RoundtripsThroughDpapi()
    {
        var settings = new AppSettings();

        settings.SetFirebirdPassword("s3nh@-secreta");

        Assert.IsNotNull(settings.ProtectedFirebirdPassword);
        Assert.AreNotEqual("s3nh@-secreta", settings.ProtectedFirebirdPassword);
        Assert.IsFalse(settings.ProtectedFirebirdPassword.Contains("s3nh@-secreta", StringComparison.Ordinal));
        Assert.AreEqual("s3nh@-secreta", settings.GetFirebirdPassword());
    }

    [TestMethod]
    public void FirebirdPassword_RoundtripsUnicodeCharacters()
    {
        var settings = new AppSettings();

        settings.SetFirebirdPassword("senha-çãõ-🔒");

        Assert.AreEqual("senha-çãõ-🔒", settings.GetFirebirdPassword());
    }

    [TestMethod]
    public void SetFirebirdPassword_ClearsPasswordWhenNullOrEmpty()
    {
        var settings = new AppSettings();
        settings.SetFirebirdPassword("abc123");

        settings.SetFirebirdPassword(null);
        Assert.IsNull(settings.ProtectedFirebirdPassword);
        Assert.IsNull(settings.GetFirebirdPassword());

        settings.SetFirebirdPassword("abc123");
        settings.SetFirebirdPassword("");
        Assert.IsNull(settings.ProtectedFirebirdPassword);
        Assert.IsNull(settings.GetFirebirdPassword());
    }

    [TestMethod]
    public void GetFirebirdPassword_DiscardsValueWhenBase64IsInvalid()
    {
        var settings = new AppSettings { ProtectedFirebirdPassword = "isso-não-é-base64!!" };

        Assert.IsNull(settings.GetFirebirdPassword());
        Assert.IsNull(settings.ProtectedFirebirdPassword);
    }

    [TestMethod]
    public void GetFirebirdPassword_DiscardsValueWhenDpapiPayloadIsInvalid()
    {
        var settings = new AppSettings { ProtectedFirebirdPassword = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 }) };

        Assert.IsNull(settings.GetFirebirdPassword());
        Assert.IsNull(settings.ProtectedFirebirdPassword);
    }
}
