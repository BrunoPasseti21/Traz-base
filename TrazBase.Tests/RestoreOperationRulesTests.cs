using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class RestoreOperationRulesTests
{
    [TestMethod]
    [DataRow("ARQEMP.fbk", "ARQEMP")]
    [DataRow("arqemp_2024.fbk", "ARQEMP")]
    [DataRow("backup_ARQOCOR.FBK", "ARQOCOR")]
    [DataRow("ARQOCOR_19.fbk", "ARQOCOR")]
    [DataRow("arqsist_10.fbk", "ARQSIST")]
    [DataRow("ARQSIST.fbk", "ARQSIST")]
    [DataRow("ARQSIST_10.FBK", "ARQSIST")]
    [DataRow("HISTORICO.fbk", null)]
    [DataRow("", null)]
    public void MatchKnownDatabase_MatchesCaseInsensitively(string fileName, string? expected)
    {
        Assert.AreEqual(expected, RestoreOperationRules.MatchKnownDatabase(fileName));
    }

    [TestMethod]
    public void GetDestinationPath_UsesFolderOfFbkAndCanonicalName()
    {
        var fbk = Path.Combine(@"C:\bases\SL-123\BKP", "arqemp_10.fbk");

        var destination = RestoreOperationRules.GetDestinationPath(fbk, "ARQEMP");

        Assert.AreEqual(Path.Combine(@"C:\bases\SL-123\BKP", "ARQEMP.FDB"), destination);
    }

    [TestMethod]
    public void GetBackupDirectory_AppendsBkp()
    {
        Assert.AreEqual(Path.Combine(@"C:\bases\SL-123", "BKP"), RestoreOperationRules.GetBackupDirectory(@"C:\bases\SL-123"));
    }

    [TestMethod]
    public void ResolveBackupDirectory_ReturnsBkpWhenItExists()
    {
        using var temp = new TempDirectory();
        var bkp = Directory.CreateDirectory(Path.Combine(temp.Path, "BKP")).FullName;

        Assert.AreEqual(bkp, RestoreOperationRules.ResolveBackupDirectory(temp.Path));
    }

    [TestMethod]
    public void ResolveBackupDirectory_ReturnsSelectedFolderWhenBkpDoesNotExist()
    {
        using var temp = new TempDirectory();

        Assert.AreEqual(temp.Path, RestoreOperationRules.ResolveBackupDirectory(temp.Path));
    }

    [TestMethod]
    public void BuildPlan_MapsKnownFilesAndLeavesUnknownsWithoutDestination()
    {
        var fbks = new[]
        {
            @"C:\base\BKP\ARQSIST.fbk",
            @"C:\base\BKP\desconhecido.fbk",
            @"C:\base\BKP\ARQEMP.fbk",
        };

        var plan = RestoreOperationRules.BuildPlan(fbks);

        Assert.HasCount(3, plan);
        Assert.AreEqual(@"C:\base\BKP\ARQEMP.FDB", plan.Single(item => item.Database == "ARQEMP").DestinationPath);
        Assert.AreEqual(@"C:\base\BKP\ARQSIST.FDB", plan.Single(item => item.Database == "ARQSIST").DestinationPath);
        var unknown = plan.Single(item => item.Database is null);
        Assert.AreEqual(@"C:\base\BKP\desconhecido.fbk", unknown.FbkPath);
        Assert.AreEqual("", unknown.DestinationPath);
    }

    [TestMethod]
    public void BuildPlan_OrdersByFilePath()
    {
        var fbks = new[] { @"C:\base\BKP\b.fbk", @"C:\base\BKP\A.fbk", @"C:\base\BKP\c.fbk" };

        var plan = RestoreOperationRules.BuildPlan(fbks);

        CollectionAssert.AreEqual(
            new[] { @"C:\base\BKP\A.fbk", @"C:\base\BKP\b.fbk", @"C:\base\BKP\c.fbk" },
            plan.Select(item => item.FbkPath).ToArray());
    }

    [TestMethod]
    public void GetDuplicateDestinations_ReportsFilesMappedToTheSameTarget()
    {
        var fbks = new[]
        {
            @"C:\base\BKP\ARQEMP.fbk",
            @"C:\base\BKP\arqemp_backup.fbk",
            @"C:\base\BKP\ARQSIST.fbk",
        };

        var duplicates = RestoreOperationRules.GetDuplicateDestinations(RestoreOperationRules.BuildPlan(fbks));

        Assert.HasCount(1, duplicates);
        Assert.AreEqual(@"C:\base\BKP\ARQEMP.FDB", duplicates[0]);
    }

    [TestMethod]
    public void GetDuplicateDestinations_ReportsNothingWhenAllTargetsAreDistinct()
    {
        var fbks = new[]
        {
            @"C:\base\BKP\ARQEMP.fbk",
            @"C:\base\BKP\ARQOCOR.fbk",
            @"C:\base\BKP\ARQSIST.fbk",
        };

        var duplicates = RestoreOperationRules.GetDuplicateDestinations(RestoreOperationRules.BuildPlan(fbks));

        Assert.IsEmpty(duplicates);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateDirectory(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TrazBaseTests-" + Guid.NewGuid().ToString("N"))).FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }

    [TestMethod]
    public void GetDuplicateDestinations_DetectsDuplicatesAgainstManualChoices()
    {
        var plan = new List<RestorePlanItem>
        {
            new(@"C:\base\BKP\ARQEMP.fbk", "ARQEMP", @"C:\base\BKP\ARQEMP.FDB"),
            new(@"C:\base\BKP\estranho.fbk", "ARQEMP", @"C:\base\BKP\ARQEMP.FDB"), // escolhido manualmente pelo operador
        };

        var duplicates = RestoreOperationRules.GetDuplicateDestinations(plan);

        Assert.HasCount(1, duplicates);
    }
}
