using TrazBase.App;

namespace TrazBase.Tests;

[TestClass]
public sealed class MultiBaseOperationRulesTests
{
    [TestMethod]
    [DataRow("ARQ01", 1)]
    [DataRow("arq05", 5)]
    [DataRow("ARQ99", 99)]
    [DataRow("ARQ00", 0)]
    [DataRow("ARQ10", 10)]
    [DataRow("ARQ1", null)]
    [DataRow("ARQ100", null)]
    [DataRow("ARQX1", null)]
    [DataRow("ARQ1A", null)]
    [DataRow("BKP", null)]
    [DataRow("ARQUIVO01", null)]
    [DataRow("", null)]
    public void MatchDatabaseFolderName_RecognizesOnlyArqWithTwoDigits(string folderName, object? expected)
    {
        var result = MultiBaseOperationRules.MatchDatabaseFolderName(folderName);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void FindDatabaseFolders_ReturnsOnlyArqFoldersOrderedByNumber()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ05"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "BKP"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ100")); // três dígitos — não é ARQ## de 2 posições
        Directory.CreateDirectory(Path.Combine(temp.Path, "arq99"));   // sem distinção de maiúsculas

        var folders = MultiBaseOperationRules.FindDatabaseFolders(temp.Path);

        CollectionAssert.AreEqual(new[] { 1, 5, 99 }, folders.Select(folder => folder.Number).ToArray());
        Assert.AreEqual(Path.Combine(temp.Path, "ARQ01"), folders[0].DirectoryPath);
    }

    [TestMethod]
    public void FindDatabaseFolders_ReturnsEmptyForMissingDirectory()
    {
        Assert.IsEmpty(MultiBaseOperationRules.FindDatabaseFolders(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }

    [TestMethod]
    public void IsMultiBase_DetectsArqFoldersAtTopLevel()
    {
        using var temp = new TempDirectory();
        Assert.IsFalse(MultiBaseOperationRules.IsMultiBase(temp.Path));

        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01"));
        Assert.IsTrue(MultiBaseOperationRules.IsMultiBase(temp.Path));
    }

    [TestMethod]
    public void GetArqSistPath_IsArqSistInsideArq01()
    {
        Assert.AreEqual(Path.Combine(Path.Combine(@"C:\bases\34700", "ARQ01"), "ARQSIST.FDB"), MultiBaseOperationRules.GetArqSistPath(@"C:\bases\34700"));
    }

    [TestMethod]
    public void GetBaseKeyWrites_CreatesBaseKeyPerArqFolderExceptArq00()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ00"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ05"));

        var writes = MultiBaseOperationRules.GetBaseKeyWrites(temp.Path);

        Assert.AreEqual(2, writes.Count);
        Assert.AreEqual(("BASE1", MultiBaseOperationRules.WithTrailingSeparator(Path.Combine(temp.Path, "ARQ01"))), writes[0]);
        Assert.AreEqual(("BASE5", MultiBaseOperationRules.WithTrailingSeparator(Path.Combine(temp.Path, "ARQ05"))), writes[1]);
    }

    [TestMethod]
    public void GetBaseKeyDeletions_RemovesBaseKeysWithoutArqFolder()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ05"));

        var deletions = MultiBaseOperationRules.GetBaseKeyDeletions(temp.Path);

        CollectionAssert.DoesNotContain(deletions.ToArray(), "BASE1");
        CollectionAssert.DoesNotContain(deletions.ToArray(), "BASE5");
        CollectionAssert.Contains(deletions.ToArray(), "BASE2");
        CollectionAssert.Contains(deletions.ToArray(), "BASE3");
        CollectionAssert.Contains(deletions.ToArray(), "BASE4");
        CollectionAssert.Contains(deletions.ToArray(), "BASE6");
        CollectionAssert.Contains(deletions.ToArray(), "BASE99");
        Assert.HasCount(97, deletions); // 99 chaves possíveis menos as duas que existem
    }

    [TestMethod]
    public void WithTrailingSeparator_NormalizesAndAppendsSeparator()
    {
        var path = MultiBaseOperationRules.WithTrailingSeparator(@"C:\Bases\34700\ARQ01" + Path.DirectorySeparatorChar);
        Assert.IsTrue(path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
        Assert.AreEqual(Path.GetFullPath(@"C:\Bases\34700\ARQ01") + Path.DirectorySeparatorChar, path);
    }

    [TestMethod]
    public void GetArqSistDatabases_ReturnsArqSistPerArqFolderExceptArq00()
    {
        using var temp = new TempDirectory();
        var arq01 = Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01")).FullName;
        var arq05 = Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ05")).FullName;
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ07")); // sem ARQSIST — não entra na lista
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ00")); // ignorada
        File.WriteAllText(Path.Combine(arq01, "ARQSIST.FDB"), "x");
        File.WriteAllText(Path.Combine(arq05, "ARQSIST.FDB"), "x");

        var databases = MultiBaseOperationRules.GetArqSistDatabases(temp.Path);

        CollectionAssert.AreEqual(
            new[] { Path.Combine(arq01, "ARQSIST.FDB"), Path.Combine(arq05, "ARQSIST.FDB") },
            databases.ToArray());
    }

    [TestMethod]
    public void GetFolderNamesWithoutArqSist_ReturnsNamesMissingArqSist()
    {
        using var temp = new TempDirectory();
        var arq01 = Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01")).FullName;
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ07")); // sem ARQSIST
        File.WriteAllText(Path.Combine(arq01, "ARQSIST.FDB"), "x");

        var missing = MultiBaseOperationRules.GetFolderNamesWithoutArqSist(temp.Path);

        CollectionAssert.AreEquivalent(new[] { "ARQ07" }, missing.ToArray());
    }

    [TestMethod]
    public void FindFilesOutsideManagedFolders_FindsNestedButExcludesArq00AndArq01()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, "sub"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ00"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ01"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "ARQ05"));
        File.WriteAllText(Path.Combine(temp.Path, "ARQEMP.FDB"), "raiz");
        File.WriteAllText(Path.Combine(temp.Path, "sub", "ARQOCOR.FDB"), "aninhado");
        File.WriteAllText(Path.Combine(temp.Path, "ARQ00", "ARQEMP.FDB"), "em arq00");
        File.WriteAllText(Path.Combine(temp.Path, "ARQ01", "ARQSIST.FDB"), "em arq01");
        File.WriteAllText(Path.Combine(temp.Path, "ARQ05", "ARQEMP.FDB"), "da base arq05");
        File.WriteAllText(Path.Combine(temp.Path, "ARQ05", "ARQSIST.FDB"), "da base arq05");

        var empFiles = MultiBaseOperationRules.FindFilesOutsideManagedFolders(temp.Path, "ARQEMP.FDB");
        var ocorFiles = MultiBaseOperationRules.FindFilesOutsideManagedFolders(temp.Path, "ARQOCOR.FDB");
        var sistFiles = MultiBaseOperationRules.FindFilesOutsideManagedFolders(temp.Path, "ARQSIST.FDB");

        Assert.HasCount(1, empFiles); // raiz
        Assert.AreEqual(Path.Combine(temp.Path, "ARQEMP.FDB"), empFiles[0]);
        Assert.HasCount(1, ocorFiles); // aninhado em "sub"
        Assert.AreEqual(Path.Combine(temp.Path, "sub", "ARQOCOR.FDB"), ocorFiles[0]);
        Assert.IsEmpty(sistFiles); // ARQ01 e ARQ05 são pastas ARQ##, excluídas
    }

    [TestMethod]
    public void FindFilesOutsideManagedFolders_OrdersResultsOrdinalIgnoreCase()
    {
        using var temp = new TempDirectory();
        var b = Directory.CreateDirectory(Path.Combine(temp.Path, "B")).FullName;
        var a = Directory.CreateDirectory(Path.Combine(temp.Path, "a")).FullName;
        File.WriteAllText(Path.Combine(b, "x.fdb"), "x");
        File.WriteAllText(Path.Combine(a, "x.fdb"), "x");

        var files = MultiBaseOperationRules.FindFilesOutsideManagedFolders(temp.Path, "x.fdb");

        CollectionAssert.AreEqual(
            new[] { Path.Combine(a, "x.fdb"), Path.Combine(b, "x.fdb") },
            files.ToArray());
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
