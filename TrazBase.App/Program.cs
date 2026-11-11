namespace TrazBase.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.ThreadException += (_, e) =>
        {
            try
            {
                System.Windows.Forms.MessageBox.Show(
                    "Ocorreu um erro inesperado:\n\n" + e.Exception,
                    "Traz Base — Erro inesperado",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error,
                    System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
            catch { /* o app continua rodando mesmo com falha na caixa */ }
        };

        var settings = AppSettings.Load();
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(settings.DarkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
        Application.Run(new MainForm());
    }
}
