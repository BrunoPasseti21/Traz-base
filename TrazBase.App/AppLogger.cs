using System;
using System.IO;
using System.Text;

namespace TrazBase.App;

/// <summary>
/// Logger de erros do aplicativo. Grava em %APPDATA%\TrazBase\logs\TrazBase.log com horário,
/// contexto, mensagem e stack trace da exceção. Nunca lança exceção: falha de log é silenciosa.
/// </summary>
internal static class AppLogger
{
    private static readonly object Gate = new();
    private static string? _path;

    /// <summary>Caminho completo do arquivo de log (cria a pasta sob demanda).</summary>
    public static string FilePath => _path ??= ComputePath();

    private static string ComputePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrazBase", "logs");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "TrazBase.log");
    }

    /// <summary>Indica se há um arquivo de log existente (para informar o operador).</summary>
    public static bool HasErrorsLogged()
    {
        try { return File.Exists(FilePath) && new FileInfo(FilePath).Length > 0; }
        catch { return false; }
    }

    public static void Error(string context, string message) => Write("ERRO", context, message, null);

    public static void Error(string context, string message, Exception? exception) =>
        Write("ERRO", context, message, exception);

    public static void Fatal(string context, string message, Exception? exception) =>
        Write("FATAL", context, message, exception);

    private static void Write(string level, string context, string message, Exception? exception)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ")
              .Append(level).Append(" | ").Append(context).Append(" | ").AppendLine(message);

            if (exception is not null)
                sb.Append("  ").AppendLine(exception.ToString());

            lock (Gate)
            {
                RotateIfNeeded();
                File.AppendAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Log é best-effort: nunca deve quebrar a operação que o chamou.
        }
    }

    private static void RotateIfNeeded()
    {
        try
        {
            var file = new FileInfo(FilePath);
            if (!file.Exists || file.Length < 2 * 1024 * 1024) return; // 2 MB

            var dir = file.Directory!.FullName;
            var backup = Path.Combine(dir, $"TrazBase-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(file.FullName, backup);
        }
        catch { }
    }
}
