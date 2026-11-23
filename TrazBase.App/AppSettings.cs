using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace TrazBase.App;

internal enum BaseFolderSortOrder
{
    Nome,
    DataModificacao
}

internal enum StartScreen
{
    Inicio,
    Transferir,
    AlterarBase,
    RedefinirBase,
    RestaurarBase,
    AtualizarSic,
    // Mantidos por compatibilidade com settings.json existentes; não aparecem mais na lista de opções.
    Configuracao,
    ComandosArqEmp,
    ComandosArqSist,
    ConfigurarSic,
    Atalhos,
    CopiarSql
}

internal sealed class AppSettings
{
    /// <summary>Versão que introduziu o tutorial guiado.</summary>
    public const string TutorialIntroVersion = "0.9.6";

    public string SolicitationRoot { get; set; } = "";
    public string BasesRoot { get; set; } = "";
    public string SupportRoot { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScriptArqEmp { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScriptArqSist { get; set; }

    public string ArqEmpCommands { get; set; } = "";
    public string ArqSistCommands { get; set; } = "";
    public bool CommandScriptsMigrated { get; set; }
    public bool DarkMode { get; set; }
    public bool MinimizeToTrayOnClose { get; set; }
    public bool StartInBackground { get; set; }
    public bool StartWithWindows { get; set; }
    public string? ProtectedFirebirdPassword { get; set; }
    public string? ProtectedSicPassword { get; set; }
    public string? ProtectedSicDynamicPassword { get; set; }
    public string TempPath { get; set; } = "";
    /// <summary>Quando ligado, o valor EMPRESA gravado no Registro HKCU\SIC\CONEXAO aponta para a pasta de bases de apoio (SupportRoot), em vez da pasta da base/ARQ00.</summary>
    public bool UseSupportRootAsCompany { get; set; }

    /// <summary>Quando ligado, a transferência de bases conecta a uma VPN configurada no Windows antes de acessar a pasta de origem.</summary>
    public bool UseVpn { get; set; }
    /// <summary>Nome da conexão VPN configurada no Windows (rasphone).</summary>
    public string VpnConnectionName { get; set; } = "";
    /// <summary>Quando ligado, usa o usuário/senha informados; desmarcado, usa as credenciais salvas no perfil do Windows.</summary>
    public bool VpnUseManualCredentials { get; set; }
    public string VpnUsername { get; set; } = "";
    public string? ProtectedVpnPassword { get; set; }
    /// <summary>Quantas tentativas de reconexão são feitas quando a VPN cai durante a cópia.</summary>
    public int VpnReconnectAttempts { get; set; } = 3;
    /// <summary>Quando ligado, executa o script abaixo assim que a VPN for conectada pelo aplicativo.</summary>
    public bool VpnConnectScriptEnabled { get; set; }
    /// <summary>Script (conteúdo de .bat/.cmd) executado quando a VPN é conectada pelo aplicativo — ex.: mapear unidades de rede.</summary>
    public string VpnConnectScript { get; set; } = "";

    public bool ForceMultiBase { get; set; }
    public bool RenameSlFolderToClientName { get; set; }
    public int BaseCount { get; set; } = 1;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StartScreen DefaultScreen { get; set; } = StartScreen.Inicio;
    /// <summary>Ordenação escolhida na tela Alterar base (persistida entre aberturas).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BaseFolderSortOrder ChangeBaseSortOrder { get; set; } = BaseFolderSortOrder.Nome;
    /// <summary>Última pasta selecionada pelo usuário na tela Alterar base.</summary>
    public string? LastSelectedBaseFolder { get; set; }
    /// <summary>Marca que a migração para a tela Início já foi aplicada (evita sobrescrever uma escolha explícita do usuário).</summary>
    public bool HomeScreenMigrationApplied { get; set; }
    public string? LastChangelogVersion { get; set; }

    /// <summary>Usuário já concluiu ou pulou o tutorial guiado.</summary>
    public bool TutorialCompleted { get; set; }

    /// <summary>Versão na qual o tutorial foi exibido pela última vez (evita repetir em updates futuros).</summary>
    public string? TutorialVersionShown { get; set; }

    /// <summary>True quando o arquivo de configuração não existia antes do carregamento (instalação nova).</summary>
    [JsonIgnore]
    public bool IsFreshInstall { get; set; }

    // Configurações da ação "Atualizar SIC" (separadas das demais).
    public string UpdateSicSourceRoot { get; set; } = "";
    public string UpdateSicDestinationRoot { get; set; } = "";
    public string UpdateSicSicVersion { get; set; } = "";
    public bool SicRenameWithVersion { get; set; }

    // Nova configuração por executável (tabela na tela Atualizar SIC).
    public bool UpdateSicSingleDestinationEnabled { get; set; }
    public string UpdateSicSingleDestinationRoot { get; set; } = "";
    public List<UpdateSicEntry> UpdateSicEntries { get; set; } = new();

    // Atalhos de pasta (tela Atalhos de pasta).
    public List<FolderShortcut> FolderShortcuts { get; set; } = new();

    // SQLs salvos na tela "Copiar SQL" (nome + comando), também listados na bandeja.
    public List<SqlShortcut> SqlShortcuts { get; set; } = new();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrazBase", "settings.json");

    public static AppSettings Load()
    {
        // Detectado ANTES de qualquer leitura/escrita: arquivo inexistente => instalação nova.
        var isFirstRun = !File.Exists(FilePath);

        AppSettings? settings = null;
        try
        {
            if (File.Exists(FilePath))
                settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
        }
        catch { }

        settings ??= new AppSettings();
        MigrateLegacySicUpdateIfMissing(settings);
        if (!settings.HomeScreenMigrationApplied)
        {
            // Instalações existentes abriam na Transferir; passam a abrir na nova tela Início.
            // A marca persistida impede que uma escolha posterior do usuário seja sobrescrita.
            if (!isFirstRun && settings.DefaultScreen == StartScreen.Transferir)
                settings.DefaultScreen = StartScreen.Inicio;
            settings.HomeScreenMigrationApplied = true;
        }
        settings.IsFreshInstall = isFirstRun;

        // Instalação existente (update) nunca deve exibir o tutorial automaticamente,
        // exceto na versão que o introduziu — controlada por TutorialVersionShown no MainForm.
        if (!isFirstRun)
            settings.TutorialCompleted = true;

        return settings;
    }

    /// <summary>
    /// Migra a antiga configuração "Atualizar SIC" (origem/destino/versão únicos + checkbox global
    /// "Renomeia com a versão") para a tabela por executável. Mantém intactas as chaves legadas para
    /// a versão de rollback continuar abrindo; a nova tela passa a usar <see cref="UpdateSicEntries"/>.
    /// </summary>
    private static void MigrateLegacySicUpdateIfMissing(AppSettings settings)
    {
        if (settings.UpdateSicEntries.Count > 0) return;

        var source = (settings.UpdateSicSourceRoot ?? "").Trim();
        if (source.Length == 0) return; // sem config antiga => começa do zero

        var destination = (settings.UpdateSicDestinationRoot ?? "").Trim();
        var version = (settings.UpdateSicSicVersion ?? "").Trim();
        var rename = settings.SicRenameWithVersion;

        string[] DefaultFiles =
        [
            "OSAtualizaSIC",
            "SIC",
            "Prj_Obra",
            "Prj_PCP_FA"
        ];

        foreach (var fileName in DefaultFiles)
        {
            settings.UpdateSicEntries.Add(new UpdateSicEntry
            {
                SourceRoot = source,
                Version = version,
                DestinationRoot = destination,
                FileName = fileName,
                RenameWithVersion = rename
            });
        }
    }

    /// <summary>
    /// Define se o tutorial deve aparecer nesta inicialização.
    /// Usado tanto pelo MainForm quanto pelo Program (que precisa abrir a janela mesmo com "segundo plano" ligado).
    /// </summary>
    public bool ShouldShowTutorial(Version currentVersion)
    {
        var current = currentVersion.ToString(3);
        return (IsFreshInstall && !TutorialCompleted)
            || (string.Equals(current, TutorialIntroVersion, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(TutorialVersionShown, TutorialIntroVersion, StringComparison.OrdinalIgnoreCase));
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public string? GetFirebirdPassword()
    {
        if (string.IsNullOrWhiteSpace(ProtectedFirebirdPassword)) return null;
        try
        {
            var encrypted = Convert.FromBase64String(ProtectedFirebirdPassword);
            var clear = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear);
        }
        catch
        {
            ProtectedFirebirdPassword = null;
            return null;
        }
    }

    public string? GetSicPassword()
    {
        if (string.IsNullOrWhiteSpace(ProtectedSicPassword)) return null;
        try
        {
            var encrypted = Convert.FromBase64String(ProtectedSicPassword);
            var clear = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear);
        }
        catch
        {
            ProtectedSicPassword = null;
            return null;
        }
    }

    public string? GetSicDynamicPassword()
    {
        if (string.IsNullOrWhiteSpace(ProtectedSicDynamicPassword)) return null;
        try
        {
            var encrypted = Convert.FromBase64String(ProtectedSicDynamicPassword);
            var clear = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear);
        }
        catch
        {
            ProtectedSicDynamicPassword = null;
            return null;
        }
    }

    public string? GetVpnPassword()
    {
        if (string.IsNullOrWhiteSpace(ProtectedVpnPassword)) return null;
        try
        {
            var encrypted = Convert.FromBase64String(ProtectedVpnPassword);
            var clear = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear);
        }
        catch
        {
            ProtectedVpnPassword = null;
            return null;
        }
    }

    public void SetFirebirdPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ProtectedFirebirdPassword = null;
            return;
        }

        var clear = Encoding.UTF8.GetBytes(password);
        try
        {
            var encrypted = ProtectedData.Protect(clear, optionalEntropy: null, DataProtectionScope.CurrentUser);
            ProtectedFirebirdPassword = Convert.ToBase64String(encrypted);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    public void SetSicPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ProtectedSicPassword = null;
            return;
        }

        var clear = Encoding.UTF8.GetBytes(password);
        try
        {
            var encrypted = ProtectedData.Protect(clear, optionalEntropy: null, DataProtectionScope.CurrentUser);
            ProtectedSicPassword = Convert.ToBase64String(encrypted);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    public void SetSicDynamicPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ProtectedSicDynamicPassword = null;
            return;
        }

        var clear = Encoding.UTF8.GetBytes(password);
        try
        {
            var encrypted = ProtectedData.Protect(clear, optionalEntropy: null, DataProtectionScope.CurrentUser);
            ProtectedSicDynamicPassword = Convert.ToBase64String(encrypted);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    public void SetVpnPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ProtectedVpnPassword = null;
            return;
        }

        var clear = Encoding.UTF8.GetBytes(password);
        try
        {
            var encrypted = ProtectedData.Protect(clear, optionalEntropy: null, DataProtectionScope.CurrentUser);
            ProtectedVpnPassword = Convert.ToBase64String(encrypted);
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
}
