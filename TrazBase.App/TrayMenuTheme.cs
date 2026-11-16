namespace TrazBase.App;

/// <summary>Paleta do menu de contexto da bandeja, acompanhando o tema do aplicativo.</summary>
internal static class TrayMenuTheme
{
    public static Color Background => ThemeState.IsDark ? Color.FromArgb(34, 42, 55) : Color.White;
    public static Color Text => ThemeState.IsDark ? Color.FromArgb(232, 237, 245) : Color.FromArgb(29, 42, 61);
    public static Color Border => ThemeState.IsDark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(208, 217, 230);
    public static Color Selected => ThemeState.IsDark ? Color.FromArgb(48, 67, 94) : Color.FromArgb(231, 240, 255);
    public static Color InputBackground => ThemeState.IsDark ? Color.FromArgb(43, 53, 69) : Color.FromArgb(249, 251, 254);
    public static Color InputForeground => ThemeState.IsDark ? Color.FromArgb(220, 229, 242) : Color.FromArgb(29, 42, 61);
}

/// <summary>Renderer do menu da bandeja: além da paleta do aplicativo, garante que a seta de submenu seja desenhada sempre com a mesma cor do texto do item (e não com SystemColors.ControlText, que fica invisível no tema escuro).</summary>
internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    public TrayMenuRenderer() : base(new TrayMenuColorTable())
    {
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        if (e.Item is not null)
            e.ArrowColor = e.Item.Enabled
                ? e.Item.ForeColor
                : Color.FromArgb(128, e.Item.ForeColor);
        base.OnRenderArrow(e);
    }
}

/// <summary>Item de submenu do menu da bandeja: sempre reconhecido como submenu para pintar a seta "›", mesmo antes de o DropDown ser populado (o Windows Forms só pinta a seta quando HasDropDownItems é verdadeiro — ou seja, após a primeira abertura, quando os itens já existem).</summary>
internal sealed class SubmenuMenuItem : ToolStripMenuItem
{
    public SubmenuMenuItem(string text) : base(text)
    {
    }

    public override bool HasDropDownItems => true;
}

/// <summary>Tabela de cores do ProfessionalColorTable limitada à paleta do aplicativo (claro/escuro).</summary>
internal sealed class TrayMenuColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => TrayMenuTheme.Background;
    public override Color ToolStripBorder => TrayMenuTheme.Border;
    public override Color MenuBorder => TrayMenuTheme.Border;
    public override Color MenuItemBorder => TrayMenuTheme.Border;
    public override Color MenuItemSelected => TrayMenuTheme.Selected;
    public override Color MenuItemSelectedGradientBegin => TrayMenuTheme.Selected;
    public override Color MenuItemSelectedGradientEnd => TrayMenuTheme.Selected;
    public override Color MenuItemPressedGradientBegin => TrayMenuTheme.Selected;
    public override Color MenuItemPressedGradientMiddle => TrayMenuTheme.Selected;
    public override Color MenuItemPressedGradientEnd => TrayMenuTheme.Selected;
    public override Color CheckBackground => TrayMenuTheme.Selected;
    public override Color CheckSelectedBackground => TrayMenuTheme.Selected;
    public override Color CheckPressedBackground => TrayMenuTheme.Selected;
    public override Color SeparatorDark => TrayMenuTheme.Border;
    public override Color SeparatorLight => TrayMenuTheme.Border;
    public override Color ImageMarginGradientBegin => TrayMenuTheme.Background;
    public override Color ImageMarginGradientMiddle => TrayMenuTheme.Background;
    public override Color ImageMarginGradientEnd => TrayMenuTheme.Background;
    public override Color ImageMarginRevealedGradientBegin => TrayMenuTheme.Background;
    public override Color ImageMarginRevealedGradientEnd => TrayMenuTheme.Background;
    public override Color OverflowButtonGradientBegin => TrayMenuTheme.Background;
    public override Color OverflowButtonGradientMiddle => TrayMenuTheme.Background;
    public override Color OverflowButtonGradientEnd => TrayMenuTheme.Background;
    public override Color StatusStripGradientBegin => TrayMenuTheme.Background;
    public override Color StatusStripGradientEnd => TrayMenuTheme.Background;
    public override Color ToolStripPanelGradientBegin => TrayMenuTheme.Background;
    public override Color ToolStripPanelGradientEnd => TrayMenuTheme.Background;
    public override Color GripDark => TrayMenuTheme.Background;
    public override Color GripLight => TrayMenuTheme.Background;
}
