namespace TrazBase.App;

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
