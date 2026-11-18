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
    /// Localiza as pastas locais que combinam com o nome informado (SL, nome simples ou <c>&lt;nome&gt;_&lt;SL&gt;</c>).
    /// O nome precisa ser um único segmento de pasta válido: sem separadores de path, sem ".." e sem
    /// caracteres inválidos para o sistema de arquivos. Correspondência: pastas cujo nome COMEÇA com o texto
    /// digitado (sem diferenciar maiúsculas/minúsculas) — ex.: digitar "AL" lista "ALLES" e "ALLES_34742";
    /// e, quando o último segmento do texto for só dígitos, também pastas terminadas em <c>_&lt;dígitos&gt;</c>
    /// (p. ex. digitar "123456" acha "JOAO_123456"). Retorna lista vazia quando nada é encontrado ou o nome é
    /// inválido; a pasta exata não tem precedência — várias correspondências devem ser apresentadas ao operador.
    /// </summary>
    internal static IReadOnlyList<string> FindLocalBaseDirectoriesByName(string basesRoot, string folderName)
    {
        if (string.IsNullOrWhiteSpace(basesRoot))
            throw new ArgumentException("A pasta local das bases não foi configurada.", nameof(basesRoot));

        folderName = folderName?.Trim() ?? string.Empty;
        if (folderName.Length == 0) return [];
        if (folderName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, ':']) >= 0) return [];
        if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return [];
        if (folderName.Contains("..", StringComparison.Ordinal)) return [];

        var root = Path.GetFullPath(basesRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(root)) return [];

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in Directory.GetDirectories(root))
        {
            if (Path.GetFileName(dir).StartsWith(folderName, StringComparison.OrdinalIgnoreCase))
                matches.Add(dir);
        }

        var parts = folderName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var tail = parts.Length == 0 ? folderName : parts[^1];
        if (tail.Length > 0 && tail.All(char.IsAsciiDigit))
        {
            var suffix = "_" + tail;
            foreach (var dir in Directory.GetDirectories(root))
            {
                if (Path.GetFileName(dir).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    matches.Add(dir);
            }
        }

        return matches
            .OrderBy(dir => Path.GetFileName(dir), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Localiza uma pasta local pelo nome informado (SL, nome simples ou <c>&lt;nome&gt;_&lt;SL&gt;</c>).
    /// Retorna a primeira correspondência ordenada (compatibilidade); use FindLocalBaseDirectoriesByName
    /// para detectar múltiplas correspondências e deixar o operador escolher.
    /// </summary>
    internal static string? FindLocalBaseDirectoryByName(string basesRoot, string folderName)
    {
        var matches = FindLocalBaseDirectoriesByName(basesRoot, folderName);
        return matches.Count == 0 ? null : matches[0];
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
