using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace TrazBase.App;

/// <summary>
/// Resultado de uma tentativa de conexão VPN.
/// </summary>
internal readonly record struct VpnConnectionResult(bool Success, string Message)
{
    public static VpnConnectionResult Ok(string message) => new(true, message);
    public static VpnConnectionResult Fail(string message) => new(false, message);
}

/// <summary>
/// Gerencia a conexão com a VPN configurada no Windows (rasapi32) usada
/// para acessar a pasta de origem das solicitações durante a transferência.
/// Não cria conexões novas: apenas dispara a conexão já configurada pelo usuário.
/// </summary>
internal static class VpnConnection
{
    private const int ERROR_SUCCESS = 0;
    private const int ERROR_BUFFER_TOO_SMALL = 603;

    private const int RAS_MaxEntryName = 256;
    private const int RAS_MaxPhoneNumber = 128;
    private const int UNLEN = 256;
    private const int PWLEN = 256;
    private const int DNLEN = 15;

    #region P/Invoke rasapi32

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int RasEnumConnections(byte[]? lprasconn, ref int lpcb, out int lpcConnections);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int RasGetEntryDialParamsW(string? lpszPhoneBook, ref RasDialParams lprasdialparams, out bool lpfPasswordRetrieved);

    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int RasDialW(IntPtr lpRasDialExtensions, string? lpszPhoneBook, ref RasDialParams lprasdialparams, uint dwNotifierType, IntPtr lpvNotifierType, out IntPtr lphRasConn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    private struct RasDialParams
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxEntryName + 1)] public string SzEntryName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxPhoneNumber + 1)] public string SzPhoneNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxPhoneNumber + 1)] public string SzCallbackNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UNLEN + 1)] public string SzUserName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PWLEN + 1)] public string SzPassword;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DNLEN + 1)] public string SzDomain;
        public uint dwSubEntry;
        public ulong dwCallbackId;
        public uint dwIfIndex;
        public IntPtr szEncPassword;
    }

    #endregion

    private static string RasDialPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "rasdial.exe");

    /// <summary>Normaliza a lista de nomes (remove vazios e duplicatas sem distinção de maiúsculas/minúsculas, ordenado).</summary>
    internal static IReadOnlyList<string> NormalizeNames(IEnumerable<string> names) =>
        new SortedSet<string>(
            names.Select(name => (name ?? "").Trim()).Where(name => name.Length > 0),
            StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>True quando a conexão <paramref name="name"/> está na lista de conexões ativas (comparação sem distinção de maiúsculas/minúsculas).</summary>
    internal static bool ConnectionNameMatches(IEnumerable<string>? activeConnections, string? name)
    {
        var target = (name ?? "").Trim();
        if (target.Length == 0 || activeConnections is null) return false;
        return activeConnections.Any(candidate =>
            string.Equals((candidate ?? "").Trim(), target, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lista as conexões de RAS (VPN e discadas) que existem no Windows.
    /// Lê o phonebook do usuário (rasphone.pbk), que é o arquivo onde as conexões ficam gravadas.
    /// </summary>
    public static List<string> ListConnections()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pbk in PhonebookPaths())
        {
            foreach (var name in ReadPhonebookNames(pbk)) names.Add(name);
        }

        return NormalizeNames(names).ToList();
    }

    private static IEnumerable<string> PhonebookPaths()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "Network", "Connections", "Pbk", "rasphone.pbk");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft", "Network", "Connections", "Pbk", "rasphone.pbk");
    }

    private static IEnumerable<string> ReadPhonebookNames(string pbkPath)
    {
        if (!File.Exists(pbkPath)) yield break;

        // O phonebook é um arquivo de seções INI: cada seção "[NomeDaConexão]" é uma conexão.
        using var reader = new StreamReader(pbkPath, Encoding.Default);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length >= 3 && trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']')
            {
                var name = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (name.Length > 0) yield return name;
            }
        }
    }

    private static string? ResolvePhonebook(string connectionName)
    {
        var target = connectionName.Trim();
        foreach (var pbk in PhonebookPaths())
        {
            if (!File.Exists(pbk)) continue;
            var names = ReadPhonebookNames(pbk);
            if (names.Any(n => string.Equals(n, target, StringComparison.OrdinalIgnoreCase)))
                return pbk;
        }

        return null;
    }

    /// <summary>Nomes das conexões de acesso remoto ativas no momento.</summary>
    public static List<string> ListActiveConnections()
    {
        var names = new List<string>();
        try
        {
            var cb = 0;
            var count = 0;
            var result = RasEnumConnections(null, ref cb, out count);
            if (result != ERROR_BUFFER_TOO_SMALL || cb <= 0) return names;

            var perEntry = count > 0 ? cb / count : cb;
            if (perEntry <= 0) return names;

            var buffer = new byte[cb];
            for (var offset = 0; offset < cb; offset += perEntry)
                BitConverter.GetBytes(perEntry).CopyTo(buffer, offset); // dwSize por RASCONN

            result = RasEnumConnections(buffer, ref cb, out count);
            if (result != ERROR_SUCCESS) return names;

            for (var offset = 0; offset < cb; offset += perEntry)
            {
                // szEntryName começa em offset + 12 (x64: DWORD dwSize + HANDLE hrasconn).
                var sb = new StringBuilder();
                for (var p = offset + 12; p + 1 < offset + perEntry && p + 1 < buffer.Length; p += 2)
                {
                    var unit = buffer[p] | (buffer[p + 1] << 8);
                    if (unit == 0) break;
                    sb.Append((char)unit);
                }
                var name = sb.ToString().Trim();
                if (name.Length > 0) names.Add(name);
            }
        }
        catch
        {
            // Em caso de falha, assume desconectado para permitir a tentativa de conexão.
        }

        return names;
    }

    /// <summary>Verdadeiro quando a conexão informada está ativa.</summary>
    public static bool IsConnected(string? name) => ConnectionNameMatches(ListActiveConnections(), name);

    /// <summary>
    /// Conecta a VPN pelo nome da conexão configurada no Windows.
    /// Com <paramref name="useManualCredentials"/> desligado, a API RAS do Windows é usada para ler as credenciais
    /// gravadas no phonebook (o mesmo caminho da tela de VPN do Windows). Se a leitura falhar, cai para o rasdial.exe.
    /// Ligado, usuário e senha informados são passados para a conexão.
    /// </summary>
    public static async Task<VpnConnectionResult> ConnectAsync(
        string name,
        bool useManualCredentials,
        string? userName,
        string? password,
        CancellationToken token)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0)
            return VpnConnectionResult.Fail("Informe o nome da conexão VPN na Configuração.");

        // 1) Tenta a API RAS (RasGetEntryDialParams + RasDial) — replica a conexão manual.
        var apiResult = TryConnectWithApiAsync(name, useManualCredentials, userName, password);
        if (apiResult is not null) return apiResult.Value;

        // 2) Fallback — rasdial.exe do Windows (útil quando a API RAS não encontra o entry).
        var arguments = new List<string> { name };
        if (useManualCredentials)
        {
            var user = (userName ?? "").Trim();
            if (user.Length == 0)
                return VpnConnectionResult.Fail("Informe o usuário da VPN ou desmarque 'Informar usuário e senha manualmente'.");
            arguments.Add(user);
            arguments.Add(password ?? "");
        }

        return await RunRasAsync(arguments, token, TimeSpan.FromSeconds(180)).ConfigureAwait(false);
    }

    /// <summary>Usa a API RAS do Windows (RasGetEntryDialParams + RasDial). Retorna null quando a API não tem a conexão no phonebook.</summary>
    private static VpnConnectionResult? TryConnectWithApiAsync(string name, bool manual, string? userName, string? password)
    {
        var pbk = ResolvePhonebook(name);
        if (pbk is null) return null; // entry não está no rasphone.pbk — fallback

        var param = new RasDialParams
        {
            dwSize = System.Runtime.InteropServices.Marshal.SizeOf<RasDialParams>(),
            SzEntryName = name,
            SzPhoneNumber = new string('\0', RAS_MaxPhoneNumber + 1),
            SzCallbackNumber = new string('\0', RAS_MaxPhoneNumber + 1),
            SzUserName = manual ? (userName ?? "").PadRight(UNLEN + 1, '\0')[..(UNLEN + 1)] : new string('\0', UNLEN + 1),
            SzPassword = manual ? (password ?? "").PadRight(PWLEN + 1, '\0')[..(PWLEN + 1)] : new string('\0', PWLEN + 1),
            SzDomain = new string('\0', DNLEN + 1)
        };

        bool pwdRetrived = false;
        var getResult = RasGetEntryDialParamsW(pbk, ref param, out pwdRetrived);
        if (getResult != ERROR_SUCCESS)
        {
            // Se não conseguiu ler os parâmetros (ex.: senha não salva), tenta conectar com o que temos —
            // o Windows pode usar outras formas de autenticação (EAP, SSO, certificados).
        }

        var dialResult = RasDialW(IntPtr.Zero, pbk, ref param, 0, IntPtr.Zero, out var hrasconn);
        if (dialResult == ERROR_SUCCESS)
            return VpnConnectionResult.Ok(manual
                ? $"Conectado à VPN '{name}' usando o usuário informado."
                : $"Conectado à VPN '{name}' com as credenciais salvas no Windows.");

        return VpnConnectionResult.Fail(FormatRasError(dialResult, name));
    }

    private static string FormatRasError(int error, string entryName)
    {
        var detail = error switch
        {
            645 => "Erro interno de autenticação (645). As credenciais salvas não puderam ser usadas.",
            651 => $"O modem ou a conexão '{entryName}' falhou (651).",
            800 => "Não foi possível encontrar o servidor VPN (800). Verifique o IP configurado.",
            807 => "O servidor VPN rejeitou a conexão (807).",
            809 => "Falha ao autenticar (809). Verifique o usuário e a senha.",
            720 => "Erro 720 — o protocolo RAS não pôde ser negociado.",
            _ => $"O Windows retornou o erro {error}."
        };

        return detail;
    }

    /// <summary>Desconecta a VPN pelo nome da conexão. Falhas são ignoradas (melhor esforço).</summary>
    public static async Task DisconnectAsync(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return;
        try
        {
            await RunRasAsync([name, "/disconnect"], CancellationToken.None, TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
        catch
        {
            // Melhor esforço: desconectar não deve interromper o fluxo.
        }
    }

    private static async Task<VpnConnectionResult> RunRasAsync(IReadOnlyList<string> arguments, CancellationToken token, TimeSpan timeout)
    {
        var start = new ProcessStartInfo
        {
            FileName = RasDialPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return VpnConnectionResult.Fail($"Não foi possível iniciar o utilitário do Windows (rasdial): {ex.Message}");
        }

        var output = new List<string>();
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
            linked.CancelAfter(timeout);
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested) throw; // cancelamento do operador
                // Tempo esgotado: mata o rasdial e trata como falha.
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                return VpnConnectionResult.Fail("Tempo esgotado esperando o Windows conectar a VPN.");
            }

            try { output.Add((await stdoutTask.ConfigureAwait(false)).Trim()); } catch { }
            try { output.Add((await stderrTask.ConfigureAwait(false)).Trim()); } catch { }
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        catch (Exception ex)
        {
            return VpnConnectionResult.Fail($"Não foi possível executar o utilitário do Windows (rasdial): {ex.Message}");
        }

        var message = string.Join(Environment.NewLine, output.Where(line => line.Length > 0));
        if (message.Length == 0) message = $"rasdial encerrou com o código {process.ExitCode}.";

        return process.ExitCode == 0
            ? VpnConnectionResult.Ok(message)
            : VpnConnectionResult.Fail(message);
    }
}
