namespace SupraKit.App;

/// <summary>
/// Um atalho de pasta exibido em "Atalhos de pasta": nome livre, pasta e quais
/// arquivos devem aparecer nas listagens filtradas (.exe, .bat ou todos os arquivos).
/// </summary>
internal sealed class FolderShortcut
{
    /// <summary>Nome exibido no atalho — ex.: "Bases", "SIC".</summary>
    public string Name { get; set; } = "";

    /// <summary>Caminho completo da pasta associada.</summary>
    public string Path { get; set; } = "";

    /// <summary>Verdadeiro para listar todos os arquivos (e pastas, quando listado em árvore).</summary>
    public bool ShowAllFiles { get; set; } = true;

    /// <summary>Exibir arquivos .exe nas listagens filtradas.</summary>
    public bool ShowExeFiles { get; set; }

    /// <summary>Exibir arquivos .bat nas listagens filtradas.</summary>
    public bool ShowBatFiles { get; set; }

    /// <summary>Resumo legível do filtro atual.</summary>
    public string FilterLabel => ShowAllFiles
        ? "Todos os arquivos"
        : (ShowExeFiles && ShowBatFiles ? ".exe e .bat"
            : ShowExeFiles ? "Apenas .exe"
            : ShowBatFiles ? "Apenas .bat"
            : "Nenhum arquivo");

    /// <summary>Verifica se um item (arquivo ou pasta) pertence ao filtro configurado.</summary>
    public bool MatchesFile(string name)
    {
        if (ShowAllFiles) return true;
        var extension = System.IO.Path.GetExtension(name);
        return (ShowExeFiles && extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            || (ShowBatFiles && extension.Equals(".bat", StringComparison.OrdinalIgnoreCase));
    }
}
