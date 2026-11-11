using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class BaseOperationRulesTests
{
    [TestMethod]
    public void ResolveLocalBaseDirectory_CombinesRootAndSolicitationNumber()
    {
        using var temp = new TempDirectory();

        var result = BaseOperationRules.ResolveLocalBaseDirectory(temp.Path, "123456");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "123456")), result);
    }

    [TestMethod]
    public void ResolveLocalBaseDirectory_AcceptsRootWithTrailingSeparator()
    {
        using var temp = new TempDirectory();
        var rootWithSeparator = temp.Path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var result = BaseOperationRules.ResolveLocalBaseDirectory(rootWithSeparator, "42");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "42")), result);
    }

    [TestMethod]
    public void ResolveLocalBaseDirectory_RejectsEmptyBasesRoot()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BaseOperationRules.ResolveLocalBaseDirectory("", "123"));
        Assert.ThrowsExactly<ArgumentException>(() => BaseOperationRules.ResolveLocalBaseDirectory("   ", "123"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("12a3")]
    [DataRow("12 3")]
    [DataRow("12-3")]
    [DataRow("../123")]
    [DataRow("SLC123")]
    public void ResolveLocalBaseDirectory_RejectsInvalidSolicitationNumbers(string sl)
    {
        Assert.ThrowsExactly<ArgumentException>(() => BaseOperationRules.ResolveLocalBaseDirectory(@"C:\Bases", sl));
    }

    [TestMethod]
    public void ResolveLocalBaseDirectory_ResultStaysInsideBasesRoot()
    {
        using var temp = new TempDirectory();

        var result = BaseOperationRules.ResolveLocalBaseDirectory(temp.Path, "999");

        var root = Path.GetFullPath(temp.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.IsTrue(result.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FindLocalBaseDirectory_ReturnsExactWhenItExists()
    {
        using var temp = new TempDirectory();
        var exact = Directory.CreateDirectory(Path.Combine(temp.Path, "123456")).FullName;

        var result = BaseOperationRules.FindLocalBaseDirectory(temp.Path, "123456");

        Assert.AreEqual(Path.GetFullPath(exact), result);
    }

    [TestMethod]
    public void FindLocalBaseDirectory_FallsBackToRenamedFolder()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "JOAO_123456"));

        var result = BaseOperationRules.FindLocalBaseDirectory(temp.Path, "123456");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "JOAO_123456")), result);
    }

    [TestMethod]
    public void FindLocalBaseDirectory_FallbackIsCaseInsensitive()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "joao_123456"));

        var result = BaseOperationRules.FindLocalBaseDirectory(temp.Path, "123456");

        Assert.IsTrue(result.EndsWith("joao_123456", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FindLocalBaseDirectory_DoesNotMatchPartialSuffix()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "JOAO_9123456")); // termina em _9123456, não _123456

        var result = BaseOperationRules.FindLocalBaseDirectory(temp.Path, "123456");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "123456")), result); // exata, que não existe
        Assert.IsFalse(Directory.Exists(result));
    }

    [TestMethod]
    public void FindLocalBaseDirectory_ReturnsExactPathWhenNothingExists()
    {
        using var temp = new TempDirectory();

        var result = BaseOperationRules.FindLocalBaseDirectory(temp.Path, "654321");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "654321")), result);
    }

    [TestMethod]
    public void GetSupportFilesToCopy_ReturnsBothWhenDestinationIsMissingAndSourceHasBoth()
    {
        using var temp = new TempDirectory();
        var baseDir = Directory.CreateDirectory(Path.Combine(temp.Path, "base")).FullName;
        var supportDir = Directory.CreateDirectory(Path.Combine(temp.Path, "apoio")).FullName;
        File.WriteAllText(Path.Combine(supportDir, "ARQEMP.FDB"), "emp");
        File.WriteAllText(Path.Combine(supportDir, "ARQOCOR.FDB"), "ocor");

        var result = BaseOperationRules.GetSupportFilesToCopy(baseDir, supportDir);

        Assert.HasCount(2, result);
        CollectionAssert.AreEquivalent(
            new[] { "ARQEMP.FDB", "ARQOCOR.FDB" },
            result.Select(item => item.Name).ToArray());
        foreach (var item in result)
        {
            Assert.AreEqual(Path.Combine(supportDir, item.Name), item.Source);
            Assert.AreEqual(Path.Combine(baseDir, item.Name), item.Destination);
        }
    }

    [TestMethod]
    public void GetSupportFilesToCopy_SkipsFilesThatAlreadyExistInDestination()
    {
        using var temp = new TempDirectory();
        var baseDir = Directory.CreateDirectory(Path.Combine(temp.Path, "base")).FullName;
        var supportDir = Directory.CreateDirectory(Path.Combine(temp.Path, "apoio")).FullName;
        File.WriteAllText(Path.Combine(baseDir, "ARQEMP.FDB"), "existente");
        File.WriteAllText(Path.Combine(supportDir, "ARQEMP.FDB"), "apoio-emp");
        File.WriteAllText(Path.Combine(supportDir, "ARQOCOR.FDB"), "apoio-ocor");

        var result = BaseOperationRules.GetSupportFilesToCopy(baseDir, supportDir);

        Assert.HasCount(1, result);
        Assert.AreEqual("ARQOCOR.FDB", result[0].Name);
    }

    [TestMethod]
    public void GetSupportFilesToCopy_ReturnsEmptyWhenEverythingExistsLocally()
    {
        using var temp = new TempDirectory();
        var baseDir = Directory.CreateDirectory(Path.Combine(temp.Path, "base")).FullName;
        var supportDir = Directory.CreateDirectory(Path.Combine(temp.Path, "apoio")).FullName;
        File.WriteAllText(Path.Combine(baseDir, "ARQEMP.FDB"), "emp");
        File.WriteAllText(Path.Combine(baseDir, "ARQOCOR.FDB"), "ocor");

        var result = BaseOperationRules.GetSupportFilesToCopy(baseDir, supportDir);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetSupportFilesToCopy_ThrowsWhenFileIsMissingInBothLocations()
    {
        using var temp = new TempDirectory();
        var baseDir = Directory.CreateDirectory(Path.Combine(temp.Path, "base")).FullName;
        var supportDir = Directory.CreateDirectory(Path.Combine(temp.Path, "apoio")).FullName;
        File.WriteAllText(Path.Combine(supportDir, "ARQEMP.FDB"), "emp"); // ARQOCOR.FDB falta nos dois lados

        Assert.ThrowsExactly<FileNotFoundException>(() => BaseOperationRules.GetSupportFilesToCopy(baseDir, supportDir));
    }

    [TestMethod]
    public void GetDestinationAction_MapsDialogResults()
    {
        Assert.AreEqual(ExistingDestinationAction.CreateBackup, BaseOperationRules.GetDestinationAction(System.Windows.Forms.DialogResult.Yes));
        Assert.AreEqual(ExistingDestinationAction.Replace, BaseOperationRules.GetDestinationAction(System.Windows.Forms.DialogResult.No));
        Assert.AreEqual(ExistingDestinationAction.Cancel, BaseOperationRules.GetDestinationAction(System.Windows.Forms.DialogResult.Cancel));
        Assert.AreEqual(ExistingDestinationAction.Cancel, BaseOperationRules.GetDestinationAction(System.Windows.Forms.DialogResult.OK));
        Assert.AreEqual(ExistingDestinationAction.Cancel, BaseOperationRules.GetDestinationAction(System.Windows.Forms.DialogResult.None));
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
}
