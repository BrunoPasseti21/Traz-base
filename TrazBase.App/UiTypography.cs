using System.Drawing.Text;

namespace TrazBase.App;

/// <summary>
/// Centraliza a tipografia da interface para manter pesos, tamanhos e renderização consistentes.
/// </summary>
internal static class UiTypography
{
    private static readonly HashSet<string> InstalledFamilies = new(
        new InstalledFontCollection().Families.Select(family => family.Name),
        StringComparer.OrdinalIgnoreCase);

    private static readonly string TextFamily = SelectFamily(
        "Segoe UI Variable Text", "Segoe UI");

    private static readonly string TextSemiboldFamily = SelectFamily(
        "Segoe UI Variable Text Semibold", "Segoe UI Semibold", "Segoe UI");

    private static readonly string DisplayFamily = SelectFamily(
        "Segoe UI Variable Display", "Segoe UI");

    public static Font Body(float size = 10f) => new(TextFamily, size, FontStyle.Regular);

    public static Font BodySemibold(float size = 10f) => new(TextSemiboldFamily, size, FontStyle.Regular);

    public static Font Display(float size) => new(DisplayFamily, size, FontStyle.Regular);

    public static Font DisplaySemibold(float size) => new(TextSemiboldFamily, size, FontStyle.Regular);

    public static Font Icons(float size) => new("Segoe UI Symbol", size, FontStyle.Regular);

    private static string SelectFamily(params string[] candidates) =>
        candidates.FirstOrDefault(InstalledFamilies.Contains) ?? SystemFonts.MessageBoxFont?.Name ?? "Arial";
}
