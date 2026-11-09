namespace TrazBase.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = AppSettings.Load();
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(settings.DarkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
        Application.Run(new MainForm());
    }
}
