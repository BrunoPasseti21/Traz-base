namespace TrazBase.App;

/// <summary>
/// Uma linha da configuração de atualização do SIC: qual arquivo baixar (SIC.exe.zip, Prj_PCP_FA.exe.zip...),
/// de onde (origem + versão), para onde e se o executável deve ser renomeado com a versão.
/// </summary>
internal sealed class UpdateSicEntry
{
    /// <summary>Pasta-base (raiz) de onde vem o .zip — ex.: "...\Sistemas\SIC".</summary>
    public string SourceRoot { get; set; } = "";

    /// <summary>Subpasta de versão dentro da origem — ex.: "146", "s145".</summary>
    public string Version { get; set; } = "";

    /// <summary>Destino desta linha; ignorado quando existir destino único configurado.</summary>
    public string DestinationRoot { get; set; } = "";

    /// <summary>Nome do arquivo a baixar, sem a extensão (ex.: "SIC", "Prj_PCP_FA"). ".exe.zip" é acrescentado automaticamente.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Se true, após extrair, os .exes do passo deste executável são renomeados com "_&lt;versão&gt;".</summary>
    public bool RenameWithVersion { get; set; }
}
