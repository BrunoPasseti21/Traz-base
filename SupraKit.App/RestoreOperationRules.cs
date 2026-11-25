namespace SupraKit.App;

internal sealed record RestorePlanItem(string FbkPath, string? Database, string DestinationPath);

internal static class RestoreOperationRules
{
    private static readonly string[] KnownDatabases = ["ARQEMP", "ARQOCOR", "ARQSIST"];

    internal static string? MatchKnownDatabase(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var candidate in KnownDatabases)
            if (name.Contains(candidate, StringComparison.OrdinalIgnoreCase)) return candidate;
        return null;
    }

    internal static string GetBackupDirectory(string baseDirectory) => Path.Combine(baseDirectory, "BKP");

    /// <summary>
    /// Retorna a subpasta BKP quando ela existir; caso contrário, a própria pasta selecionada.
    /// </summary>
    internal static string ResolveBackupDirectory(string baseDirectory)
    {
        var backup = GetBackupDirectory(baseDirectory);
        return Directory.Exists(backup) ? backup : baseDirectory;
    }

    internal static string GetDestinationPath(string fbkPath, string database)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(fbkPath))
            ?? throw new InvalidOperationException("O caminho do arquivo .FBK não tem pasta de destino válida.");
        return Path.Combine(directory, database + ".FDB");
    }

    /// <summary>
    /// Monta o plano de restauração: associa cada .FBK ao banco conhecido pelo nome do arquivo.
    /// Arquivos sem correspondência ficam com <see cref="RestorePlanItem.Database"/> nulo; o destinatário
    /// precisa ser perguntado ao operador ou o arquivo pulado.
    /// </summary>
    internal static IReadOnlyList<RestorePlanItem> BuildPlan(IEnumerable<string> fbkFiles) =>
        fbkFiles
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .Select(file =>
            {
                var database = MatchKnownDatabase(file);
                var destination = database is null ? "" : GetDestinationPath(file, database);
                return new RestorePlanItem(file, database, destination);
            })
            .ToList();

    /// <summary>
    /// Lista os .FBK da pasta selecionada/BKP e, em estruturas multi-base, também os que estão
    /// dentro das pastas ARQ## (cada um identificado para receber sua própria aba de log).
    /// </summary>
    internal static IReadOnlyList<string> FindFbkFiles(string folder, string backupDirectory)
    {
        var files = new List<string>(Directory.GetFiles(backupDirectory, "*.fbk"));
        if (MultiBaseOperationRules.IsMultiBase(folder) || MultiBaseOperationRules.IsMultiBase(backupDirectory))
        {
            try
            {
                foreach (var file in Directory.GetFiles(folder, "*.fbk", SearchOption.AllDirectories))
                    if (FindArqFolderName(file, folder) is not null
                        && !files.Contains(file, StringComparer.OrdinalIgnoreCase))
                        files.Add(file);
            }
            catch { /* pastas inacessíveis são ignoradas; os .FBK da BKP continuam válidos */ }
        }
        return files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Retorna o nome da pasta ARQ## que contém o arquivo (até a raiz da base).</summary>
    internal static string? FindArqFolderName(string path, string root)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        while (!string.IsNullOrEmpty(directory))
        {
            var name = Path.GetFileName(directory);
            if (MultiBaseOperationRules.MatchDatabaseFolderName(name) is not null)
                return name!.ToUpperInvariant();
            if (string.Equals(directory, rootFull, StringComparison.OrdinalIgnoreCase)) break;
            var parent = Path.GetDirectoryName(directory);
            if (string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase)) break;
            directory = parent;
        }
        return null;
    }

    /// <summary>Nome da aba de log: "ARQSIST"/"ARQEMP"/"ARQOCOR", ou "ARQ01 - ARQSIST" no multi-base.</summary>
    internal static string GetTabName(RestorePlanItem item, string root)
    {
        var database = item.Database ?? Path.GetFileNameWithoutExtension(item.FbkPath);
        var arqFolder = FindArqFolderName(item.FbkPath, root);
        return arqFolder is null ? database : $"{arqFolder} - {database}";
    }

    /// <summary>
    /// Retorna os destinos que aparecem mais de uma vez no plano (ignorando itens sem destino definido).
    /// </summary>
    internal static IReadOnlyList<string> GetDuplicateDestinations(IEnumerable<RestorePlanItem> plan) =>
        plan.Where(item => item.Database is not null)
            .GroupBy(item => item.DestinationPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(destination => destination, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
