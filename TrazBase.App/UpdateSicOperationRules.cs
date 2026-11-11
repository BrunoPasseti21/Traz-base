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

    internal static string GetDestinationZip(string destinationRoot, string zipFileName) =>
        Path.Combine(Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), zipFileName);
}
