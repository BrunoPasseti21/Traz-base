using System.Text.RegularExpressions;

namespace TrazBase.App;

internal sealed record DatabaseFolder(int Number, string DirectoryPath);

/// <summary>
/// Regras puras do tratamento multi-base: quando a SL recebida traz pastas
/// ARQ## (dois dígitos), cada pasta vira uma base no Registro (BASE&lt;n&gt;),
/// e a pasta ARQ00 guarda ARQEMP.FDB/ARQOCOR.FDB (EMPRESA).
/// </summary>
internal static class MultiBaseOperationRules
{
    private static readonly Regex FolderPattern = new(@"^ARQ(\d{2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal const string CompanyFolderName = "ARQ00";
    internal const string CompanyDatabaseEmp = "ARQEMP.FDB";
    internal const string CompanyDatabaseOcor = "ARQOCOR.FDB";
    internal const string SystemDatabase = "ARQSIST.FDB";

    /// <summary>
    /// Se o nome for uma pasta ARQ##, retorna o número (0–99); caso contrário, null.
    /// </summary>
    internal static int? MatchDatabaseFolderName(string? folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return null;
        var match = FolderPattern.Match(folderName);
        return match.Success && int.TryParse(match.Groups[1].Value, out var number) ? number : null;
    }

    /// <summary>
    /// Lista as pastas ARQ## (número, caminho completo) diretamente sob <paramref name="root"/>, ordenadas por número.
    /// </summary>
    internal static IReadOnlyList<DatabaseFolder> FindDatabaseFolders(string root)
    {
        if (!Directory.Exists(root)) return Array.Empty<DatabaseFolder>();

        var folders = new List<DatabaseFolder>();
        foreach (var directory in Directory.GetDirectories(root))
        {
            if (MatchDatabaseFolderName(Path.GetFileName(directory)) is { } number)
                folders.Add(new DatabaseFolder(number, directory));
        }
        return folders.OrderBy(folder => folder.Number).ToList();
    }

    internal static bool IsMultiBase(string root) => FindDatabaseFolders(root).Count > 0;

    /// <summary>
    /// Encontra recursivamente um arquivo, excluindo os que já estão dentro de pastas ARQ## (incluindo ARQ00/ARQ01).
    /// </summary>
    internal static IReadOnlyList<string> FindFilesOutsideManagedFolders(string root, string fileName)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var managed = FindDatabaseFolders(normalizedRoot).Select(folder => folder.DirectoryPath).ToList();
        foreach (var reserved in new[] { GetCompanyDirectory(normalizedRoot), Path.Combine(normalizedRoot, "ARQ01") })
            if (!managed.Contains(reserved, StringComparer.OrdinalIgnoreCase)) managed.Add(reserved);
        return Directory.GetFiles(normalizedRoot, fileName, SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Where(path => managed.All(directory => !IsPathUnder(path, directory)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsPathUnder(string path, string directory)
    {
        var dir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetCompanyDirectory(string root) => Path.GetFullPath(Path.Combine(root, CompanyFolderName));

    internal static string GetArqSistPath(string root) => Path.Combine(Path.Combine(root, "ARQ01"), SystemDatabase);

    /// <summary>
    /// Caminhos das ARQSIST.FDB existentes nas pastas ARQ## (exceto ARQ00) — candidatos à execução dos comandos ARQSIST.
    /// </summary>
    internal static IReadOnlyList<string> GetArqSistDatabases(string root) =>
        FindDatabaseFolders(root)
            .Where(folder => folder.Number != 0)
            .Select(folder => Path.Combine(folder.DirectoryPath, SystemDatabase))
            .Where(File.Exists)
            .ToList();

    /// <summary>
    /// Nomes das pastas ARQ## (exceto ARQ00) que não têm ARQSIST.FDB — usadas para registrar avisos no log.
    /// </summary>
    internal static IReadOnlyList<string> GetFolderNamesWithoutArqSist(string root) =>
        FindDatabaseFolders(root)
            .Where(folder => folder.Number != 0 && !File.Exists(Path.Combine(folder.DirectoryPath, SystemDatabase)))
            .Select(folder => Path.GetFileName(folder.DirectoryPath))
            .ToList();

    internal static string WithTrailingSeparator(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

    /// <summary>
    /// Chaves BASE&lt;n&gt; (1–99) a gravar, uma por pasta ARQ## encontrada (ARQ00 fica de fora; ela alimenta EMPRESA).
    /// </summary>
    internal static IReadOnlyList<(string Key, string Value)> GetBaseKeyWrites(string root) =>
        FindDatabaseFolders(root)
            .Where(folder => folder.Number != 0)
            .Select(folder => ("BASE" + folder.Number, WithTrailingSeparator(folder.DirectoryPath)))
            .ToList();

    /// <summary>
    /// Chaves BASE&lt;n&gt; (1–99) a remover: todas as que não têm pasta ARQ## correspondente na SL.
    /// </summary>
    internal static IReadOnlyList<string> GetBaseKeyDeletions(string root)
    {
        var present = FindDatabaseFolders(root).Select(folder => folder.Number).ToHashSet();
        var deletions = new List<string>();
        for (var number = 1; number <= 99; number++)
            if (!present.Contains(number)) deletions.Add("BASE" + number);
        return deletions;
    }
}
