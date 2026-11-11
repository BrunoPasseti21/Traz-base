using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class UpdateSicOperationRulesTests
{
    [TestMethod]
    public void Executables_ContainsAllFourExpectedZipFiles()
    {
        CollectionAssert.AreEqual(
            new[] { "OSAtualizaSIC.exe.zip", "SIC.exe.zip", "Prj_Obra.exe.zip", "Prj_PCP_FA.exe.zip" },
            UpdateSicOperationRules.Executables.ToArray());
    }

    [TestMethod]
    public void GetSourceRoot_CombinesRootAndVersion()
    {
        var result = UpdateSicOperationRules.GetSourceRoot(@"C:\Sistemas\SIC", "s126");

        Assert.AreEqual(@"C:\Sistemas\SIC\s126", result);
    }

    [TestMethod]
    public void GetSourceRoot_AcceptsRootWithTrailingSeparatorAndTrims()
    {
        var result = UpdateSicOperationRules.GetSourceRoot(@"C:\Sistemas\SIC\", "  s130  ");

        Assert.AreEqual(@"C:\Sistemas\SIC\s130", result);
    }

    [TestMethod]
    public void GetSourceRoot_RejectsVersionWithPathTraversal()
    {
        Assert.ThrowsExactly<ArgumentException>(() => UpdateSicOperationRules.GetSourceRoot(@"C:\Sistemas\SIC", "..\\s126"));
    }

    [TestMethod]
    public void GetSourceRoot_RejectsEmptyInputs()
    {
        Assert.ThrowsExactly<ArgumentException>(() => UpdateSicOperationRules.GetSourceRoot("", "s126"));
        Assert.ThrowsExactly<ArgumentException>(() => UpdateSicOperationRules.GetSourceRoot(@"C:\SIC", ""));
    }

    [TestMethod]
    public void GetDestinationZip_CombinesDestinationAndFileName()
    {
        Assert.AreEqual(@"C:\projetos\SIC\sic\desenvolvimento\SIC.exe.zip", UpdateSicOperationRules.GetDestinationZip(@"C:\projetos\SIC\sic\desenvolvimento", "SIC.exe.zip"));
    }

    [TestMethod]
    public void GetVersionOrDefault_UsesConfiguredValueOrFallback()
    {
        Assert.AreEqual("s130", UpdateSicOperationRules.GetVersionOrDefault("  s130  "));
        Assert.AreEqual("s126", UpdateSicOperationRules.GetVersionOrDefault(""));
        Assert.AreEqual("s126", UpdateSicOperationRules.GetVersionOrDefault("   "));
    }
}
