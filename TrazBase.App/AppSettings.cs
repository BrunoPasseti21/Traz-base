using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace TrazBase.App;

internal enum StartScreen
{
    Transferir,
    AlterarBase,
    RedefinirBase,
    RestaurarBase,
    AtualizarSic,
    Configuracao,
    ComandosArqEmp,
    ComandosArqSist
}

internal sealed class AppSettings
{
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
    public string? ProtectedFirebirdPassword { get; set; }
    public string TempPath { get; set; } = "";
    public bool ForceMultiBase { get; set; }
    public int BaseCount { get; set; } = 4;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StartScreen DefaultScreen { get; set; } = StartScreen.Transferir;
    public string? LastChangelogVersion { get; set; }

    // Configurações da ação "Atualizar SIC" (separadas das demais).
    public string UpdateSicSourceRoot { get; set; } = "";
    public string UpdateSicDestinationRoot { get; set; } = "";
    public string UpdateSicSicVersion { get; set; } = "";

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrazBase", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
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
}
