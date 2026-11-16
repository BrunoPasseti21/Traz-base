using System.Runtime.InteropServices;
using System.Security;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace TrazBase.App;

/// <summary>
/// Toast do Windows (Central de Notificações) com a marca "Traz Base".
/// O ícone/nome exibido vêm do atalho do menu Iniciar com o AppUserModelID
/// <see cref="AppUserModelId"/> criado pelo instalador.
/// Em ambientes sem o atalho (ex.: rodando de uma pasta de desenvolvimento),
/// o toast ainda funciona, apenas sem o nome/ícone do app.
/// </summary>
internal static class ToastNotifier
{
    internal const string AppUserModelId = "BrunoPasseti.TrazBase";

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    /// <summary>Registra o AUMID do processo. Deve ser chamada no início do programa, antes de qualquer toast.</summary>
    internal static void Initialize()
    {
        try { _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId); }
        catch { /* best-effort: toast continua disponível, apenas sem a marca */ }
    }

    /// <summary>Mostra um toast do Windows. Retorna false se algo falhar (o chamador decide o fallback; nunca lança).</summary>
    internal static bool TryShow(string title, string body, Action? onActivated)
    {
        try
        {
            var safeTitle = SecurityElement.Escape(title ?? "") ?? "";
            var safeBody = SecurityElement.Escape(
                (body ?? "").Replace("\r\n", " · ").Replace('\n', ' ').Replace('\r', ' ')) ?? "";
            var xml = new XmlDocument();
            xml.LoadXml(
                $"<toast><visual><binding template=\"ToastGeneric\">" +
                $"<text>{safeTitle}</text><text>{safeBody}</text>" +
                $"</binding></visual><audio silent=\"true\"/></toast>");
            var toast = new ToastNotification(xml);
            if (onActivated is not null)
            {
                toast.Activated += (_, _) =>
                {
                    try { onActivated(); } catch { /* o clique no toast nunca deve derrubar o app */ }
                };
            }
            ToastNotificationManager.CreateToastNotifier(AppUserModelId).Show(toast);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
