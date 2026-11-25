namespace SupraKit.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.ThreadException += (_, e) =>
        {
            AppLogger.Fatal("UI", "Erro inesperado na interface", e.Exception);
            try
            {
                System.Windows.Forms.MessageBox.Show(
                    "Ocorreu um erro inesperado:\n\n" + e.Exception,
                    "SupraKit — Erro inesperado",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error,
                    System.Windows.Forms.MessageBoxDefaultButton.Button1);
            }
            catch { /* o app continua rodando mesmo com falha na caixa */ }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            AppLogger.Fatal("Domínio", "Exceção não tratada", e.ExceptionObject as Exception);
        };

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        var primaryInstanceMutex = SingleInstance.TryAcquirePrimaryInstance();
        if (primaryInstanceMutex is null)
        {
            // Já há uma instância em execução: pede para ela vir para frente e encerra.
            SingleInstance.NotifyPrimaryInstance();
            return;
        }
        try
        {
            var settings = AppSettings.Load();
            ApplicationConfiguration.Initialize();
            ToastNotifier.Initialize();
            Application.SetColorMode(settings.DarkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
            var form = new MainForm();
            // Na primeira abertura (ou na versão que introduziu o tutorial), a preferência
            // "segundo plano" é ignorada: a janela precisa aparecer para exibir o tour.
            var tutorialPending = settings.ShouldShowTutorial(
                typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0));
            if (settings.StartInBackground && !tutorialPending)
            {
                // Arranque oculto: a janela nunca é exibida; a aplicação vive na bandeja (opção "Iniciar o SupraKit em segundo plano").
                // Loop de mensagens vazio: os hotkeys, o ícone da bandeja e o watcher de instância única mantêm o app vivo.
                form.StartInBackground();
                Application.Run();
            }
            else
            {
                Application.Run(form);
            }
        }
        finally
        {
            primaryInstanceMutex.Dispose();
        }
    }
}
