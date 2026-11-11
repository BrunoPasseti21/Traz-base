namespace TrazBase.App;

internal enum ExistingDestinationAction
{
    Cancel,
    CreateBackup,
    Replace
}

internal static class BaseOperationRules
{
    private static readonly string[] RequiredSupportDatabases = ["ARQEMP.FDB", "ARQOCOR.FDB"];

    internal static string ResolveLocalBaseDirectory(string basesRoot, string solicitationNumber)
    {
        if (string.IsNullOrWhiteSpace(basesRoot))
            throw new ArgumentException("A pasta local das bases não foi configurada.", nameof(basesRoot));
        if (string.IsNullOrWhiteSpace(solicitationNumber) || solicitationNumber.Any(character => !char.IsAsciiDigit(character)))
            throw new ArgumentException("O número da solicitação deve conter somente dígitos.", nameof(solicitationNumber));

        var root = Path.GetFullPath(basesRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var destination = Path.GetFullPath(Path.Combine(root, solicitationNumber));
        if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O destino calculado está fora da pasta local configurada.");

        return destination;
    }

    /// <summary>
    /// Localiza a pasta local de uma base: primeiro a exata (<c>BasesRoot\SL</c>); se não existir,
    /// qualquer subpasta terminada em <c>_&lt;SL&gt;</c> (p. ex. "JOAO_123456", renomeada durante a
    /// transferência). Quando nada existe, devolve a exata, para que o chamador exiba a mensagem
    /// de "pasta não encontrada" para o caminho esperado.
    /// </summary>
    internal static string FindLocalBaseDirectory(string basesRoot, string solicitationNumber)
    {
        var exact = ResolveLocalBaseDirectory(basesRoot, solicitationNumber);
        if (Directory.Exists(exact)) return exact;

        try
        {
            var root = Path.GetFullPath(basesRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root)) return exact;

            var suffix = "_" + solicitationNumber;
            return Directory.GetDirectories(root)
                .Where(dir => Path.GetFileName(dir).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(dir => Path.GetFileName(dir), StringComparer.CurrentCultureIgnoreCase)
                .FirstOrDefault() ?? exact;
        }
        catch
        {
            return exact;
        }
    }

    internal static IReadOnlyList<(string Name, string Source, string Destination)> GetSupportFilesToCopy(
        string localBaseDirectory,
        string supportDirectory)
    {
        var filesToCopy = new List<(string Name, string Source, string Destination)>();
        foreach (var name in RequiredSupportDatabases)
        {
            var destination = Path.Combine(localBaseDirectory, name);
            if (File.Exists(destination)) continue;

            var source = Path.Combine(supportDirectory, name);
            if (!File.Exists(source))
                throw new FileNotFoundException($"{name} não existe na base local nem na pasta de apoio.", source);

            filesToCopy.Add((name, source, destination));
        }

        return filesToCopy;
    }

    internal static ExistingDestinationAction GetDestinationAction(System.Windows.Forms.DialogResult result) => result switch
    {
        System.Windows.Forms.DialogResult.Yes => ExistingDestinationAction.CreateBackup,
        System.Windows.Forms.DialogResult.No => ExistingDestinationAction.Replace,
        _ => ExistingDestinationAction.Cancel
    };
}

internal static class ArchivePathSafety
{
    internal static string GetSafeEntryPath(string destinationRoot, string entryKey)
    {
        var normalized = entryKey.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(part => part == ".."))
            throw new InvalidOperationException("O arquivo compactado contém um caminho de saída inválido.");

        var root = Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var output = Path.GetFullPath(Path.Combine(root, normalized));
        if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O arquivo compactado contém um caminho fora da pasta de destino.");
        return output;
    }
}
