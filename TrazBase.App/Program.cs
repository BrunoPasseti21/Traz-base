namespace TrazBase.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = AppSettings.Load();
        Application.SetCompatibleTextRenderingDefault(true);
        Application.SetColorMode(settings.DarkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
