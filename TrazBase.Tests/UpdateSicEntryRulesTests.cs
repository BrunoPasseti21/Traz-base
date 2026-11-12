using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class UpdateSicEntryRulesTests
{
    [TestMethod]
    public void NormalizeBaseFileName_TrimsSpacesAndStripsInvalidChars()
    {
        Assert.AreEqual("SIC", UpdateSicOperationRules.NormalizeBaseFileName("  SIC  "));
        Assert.AreEqual("SIC.zip", UpdateSicOperationRules.NormalizeBaseFileName("SIC*?.zip"));
        Assert.AreEqual(string.Empty, UpdateSicOperationRules.NormalizeBaseFileName(string.Empty));
        Assert.AreEqual("SIC", UpdateSicOperationRules.NormalizeBaseFileName("  SI C  "));
    }

    [TestMethod]
    public void ValidateEntry_DetectsMissingFieldsAndBadPath()
    {
        var errors = new List<UpdateSicOperationRules.UpdateSicValidationIssue>();
        UpdateSicOperationRules.ValidateEntry(new UpdateSicEntry(), null, false, 0, errors);
        Assert.IsNotEmpty(errors);
    }

    [TestMethod]
    public void ValidateEntry_AcceptsCompleteRow()
    {
        var errors = new List<UpdateSicOperationRules.UpdateSicValidationIssue>();
        UpdateSicOperationRules.ValidateEntry(new UpdateSicEntry
        {
            SourceRoot = @"C:\Sistemas\SIC",
            Version = "146",
            DestinationRoot = @"C:\projetos\SIC\sic\desenvolvimento",
            FileName = "SIC",
            RenameWithVersion = false
        }, null, false, 0, errors);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void ValidateEntry_UsesSingleDestinationWhenEnabled()
    {
        var single = @"C:\projetos\SIC\sic\desenvolvimento";
        var errors = new List<UpdateSicOperationRules.UpdateSicValidationIssue>();
        UpdateSicOperationRules.ValidateEntry(new UpdateSicEntry
        {
            SourceRoot = @"C:\Sistemas\SIC",
            Version = "146",
            DestinationRoot = "",
            FileName = "SIC"
        }, single, true, 0, errors);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void ValidateEntry_RequiresRootedPaths()
    {
        var errors = new List<UpdateSicOperationRules.UpdateSicValidationIssue>();
        UpdateSicOperationRules.ValidateEntry(new UpdateSicEntry
        {
            SourceRoot = "Sistemas\\SIC",
            Version = "146",
            DestinationRoot = "C:\\destino",
            FileName = "SIC"
        }, null, false, 0, errors);
        Assert.IsNotEmpty(errors);
        StringAssert.Contains(string.Join("\n", errors.Select(e => e.Message)), "completo");
    }

    [TestMethod]
    public void ResolveExecutableZip_CombinesRootAndVersionAndFileName()
    {
        using var temp = new TempDirectory();
        var dir = Directory.CreateDirectory(Path.Combine(temp.Path, "146"));
        var zip = Path.Combine(dir.FullName, "SIC.exe.zip");
        File.WriteAllText(zip, "dummy");

        var resolved = UpdateSicOperationRules.ResolveExecutableZip(temp.Path, "146", "SIC");
        Assert.AreEqual(Path.GetFullPath(zip), resolved);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TrazBaseTests_" + Guid.NewGuid().ToString("N"));
        public TempDirectory() { System.IO.Directory.CreateDirectory(Path); }
        public void Dispose()
        {
            try { if (System.IO.Directory.Exists(Path)) System.IO.Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }
}
