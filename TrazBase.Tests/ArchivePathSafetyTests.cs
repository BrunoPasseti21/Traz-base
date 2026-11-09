using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class ArchivePathSafetyTests
{
    [TestMethod]
    public void GetSafeEntryPath_AcceptsSimpleRelativeEntry()
    {
        using var temp = new TempDirectory();

        var result = ArchivePathSafety.GetSafeEntryPath(temp.Path, "base.fdb");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "base.fdb")), result);
    }

    [TestMethod]
    public void GetSafeEntryPath_PreservesSubdirectoryStructure()
    {
        using var temp = new TempDirectory();

        var result = ArchivePathSafety.GetSafeEntryPath(temp.Path, "sub/pasta/arquivo.fdb");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "sub", "pasta", "arquivo.fdb")), result);
    }

    [TestMethod]
    public void GetSafeEntryPath_NormalizesBackslashes()
    {
        using var temp = new TempDirectory();

        var result = ArchivePathSafety.GetSafeEntryPath(temp.Path, @"sub\pasta\arquivo.fdb");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(temp.Path, "sub", "pasta", "arquivo.fdb")), result);
    }

    [TestMethod]
    public void GetSafeEntryPath_RejectsParentDirectoryTraversal()
    {
        using var temp = new TempDirectory();

        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, "../evil.fdb"));
        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, "sub/../../evil.fdb"));
        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, @"sub\..\..\evil.fdb"));
    }

    [TestMethod]
    public void GetSafeEntryPath_RejectsAbsolutePaths()
    {
        using var temp = new TempDirectory();

        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, @"C:\Windows\evil.fdb"));
        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, @"\\servidor\compartilhamento\evil.fdb"));
        Assert.ThrowsExactly<InvalidOperationException>(() => ArchivePathSafety.GetSafeEntryPath(temp.Path, @"\evil.fdb"));
    }

    [TestMethod]
    public void GetSafeEntryPath_ResultStaysInsideDestinationRoot()
    {
        using var temp = new TempDirectory();

        var result = ArchivePathSafety.GetSafeEntryPath(temp.Path, "a/b/c.txt");

        var root = Path.GetFullPath(temp.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.IsTrue(result.StartsWith(root, StringComparison.OrdinalIgnoreCase));
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
