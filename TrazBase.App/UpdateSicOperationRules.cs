namespace TrazBase.App;

/// <summary>
/// Regras puras da ação "Atualizar SIC": lista dos executáveis a baixar e onde ficam origem/destino.
/// </summary>
internal static class UpdateSicOperationRules
{
    private static readonly string[] ExecutablesToUpdate =
    [
        "OSAtualizaSIC.exe.zip",
        "SIC.exe.zip",
        "Prj_Obra.exe.zip",
        "Prj_PCP_FA.exe.zip",
    ];

    internal static IReadOnlyList<string> Executables => ExecutablesToUpdate;

    internal static string GetSourceRoot(string sourceRoot, string version)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot))
            throw new ArgumentException("A pasta de origem dos executáveis não foi configurada.", nameof(sourceRoot));
        var root = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("A versão do SIC não foi informada.", nameof(version));
        var trimmed = version.Trim();
        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmed.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("A versão do SIC contém caracteres inválidos.", nameof(version));
        return Path.Combine(root, trimmed);
    }

    internal static string GetVersionOrDefault(string version) => string.IsNullOrWhiteSpace(version) ? "s126" : version.Trim();

    // ---------- Fluxo por executável (substitui a "Atualizar SIC" única) ----------

    internal sealed class UpdateSicValidationIssue
    {
        public int Row { get; init; }
        public string Message { get; init; } = "";
    }

    internal static string NormalizeBaseFileName(string? raw)
    {
        if (raw is null) return "";
        var name = raw.Trim();
        const char space = ' ';
        // aceita digitação direta; limpa apenas caracteres de nome de arquivo proibidos
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch == space) continue;
            if (Array.IndexOf(invalid, ch) < 0) builder.Append(ch);
        }
        return builder.ToString();
    }

    internal static string ResolveExecutableZip(string sourceRoot, string version, string baseFileName)
    {
        var sourceFolder = GetSourceRoot(sourceRoot, version);
        var name = NormalizeBaseFileName(baseFileName);
        if (name.Length == 0)
            throw new ArgumentException("O nome do arquivo a baixar não foi informado.", nameof(baseFileName));

        var fileName = name.EndsWith(".exe.zip", StringComparison.OrdinalIgnoreCase)
            ? name
            : name + ".exe.zip";
        var candidate = Path.Combine(sourceFolder, fileName);
        if (!File.Exists(candidate))
        {
            // tolerância: o operador digitou com ".zip" mas o arquivo é ".exe.zip"
            var alt = Path.Combine(sourceFolder, name + ".zip");
            if (File.Exists(alt)) return alt;
        }
        return candidate;
    }

    internal static bool TryResolveExecutableZip(string sourceRoot, string version, string baseFileName, out string zipPath, out string error)
    {
        zipPath = "";
        error = "";
        try { zipPath = ResolveExecutableZip(sourceRoot, version, baseFileName); }
        catch (Exception ex) { error = ex.Message; return false; }
        return true;
    }

    /// <summary>Valida uma linha da configuração de atualização. Linha com índice &lt; 0: ignorar.</summary>
    internal static void ValidateEntry(UpdateSicEntry entry, string? singleDestination, bool destinationOnlyGlobal, int row, ICollection<UpdateSicValidationIssue> errors)
    {
        var line = row + 1;
        var source = (entry.SourceRoot ?? "").Trim();
        var version = (entry.Version ?? "").Trim();
        var file = NormalizeBaseFileName(entry.FileName);
        var dest = destinationOnlyGlobal ? (singleDestination ?? "").Trim() : (entry.DestinationRoot ?? "").Trim();

        if (source.Length == 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: a pasta de origem não foi informada." });
        else if (!Path.IsPathRooted(source)) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: a pasta de origem deve ser um caminho completo (com unidade, ex.: C:\\...)." });
        if (version.Length == 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: a versão/nome da pasta não foi informada (ex.: 146)." });
        else if (version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || version.Contains("..", StringComparison.Ordinal) || version.IndexOfAny(['/', '\\']) >= 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: a pasta de versão '{version}' contém caracteres inválidos." });
        if (file.Length == 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: o nome do arquivo a baixar não foi informado (ex.: SIC)." });
        else if (file.IndexOfAny(['/', '\\']) >= 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: o nome do arquivo '{file}' não deve conter separadores de pasta." });
        if (dest.Length == 0) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: o destino não foi informado." });
        else if (!Path.IsPathRooted(dest)) errors.Add(new UpdateSicValidationIssue { Row = row, Message = $"Linha {line}: o destino deve ser um caminho completo (com unidade)." });
    }

    internal static string GetDestinationZip(string destinationRoot, string zipFileName) =>
        Path.Combine(Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), zipFileName);
}
