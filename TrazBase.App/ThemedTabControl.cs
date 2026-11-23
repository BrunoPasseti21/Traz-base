namespace TrazBase.App;

/// <summary>
/// TabControl com desenho próprio, seguindo a paleta do app (claro/escuro).
/// As abas usam largura fixa e o texto acompanha o tema; a página ativa tem
/// fundo igual ao da superfície do aplicativo.
/// </summary>
internal sealed class ThemedTabControl : TabControl
{
    private static readonly Color Ink = Color.FromArgb(29, 42, 61);
    private static readonly Color Muted = Color.FromArgb(97, 113, 134);
    private static readonly Color Surface = Color.White;
    private static readonly Color Accent = Color.FromArgb(31, 111, 235);
    private static readonly Color DarkInk = Color.FromArgb(232, 237, 245);
    private static readonly Color DarkMuted = Color.FromArgb(166, 180, 199);
    private static readonly Color DarkSurface = Color.FromArgb(34, 42, 55);
    private static readonly Color DarkBackground = Color.FromArgb(24, 30, 40);
    private static readonly Color DarkTabIdle = Color.FromArgb(30, 37, 50);

    public ThemedTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Alignment = TabAlignment.Top;
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(140, 36);
        Padding = new Point(4, 0);
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Background);

    private Color Background => ThemeState.IsDark ? DarkBackground : Color.FromArgb(244, 247, 251);
    private Color TabIdle => ThemeState.IsDark ? DarkTabIdle : Color.FromArgb(236, 240, 246);
    private Color TabActive => ThemeState.IsDark ? DarkSurface : Surface;
    private Color TextActive => ThemeState.IsDark ? DarkInk : Ink;
    private Color TextIdle => ThemeState.IsDark ? DarkMuted : Muted;
    private Color LineColor => ThemeState.IsDark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(208, 217, 230);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Background);

        // Linha que separa as abas das páginas.
        var stripBottom = ItemSize.Height - 1;
        using var line = new Pen(LineColor);
        g.DrawLine(line, 0, stripBottom, ClientRectangle.Right, stripBottom);

        for (var i = 0; i < TabPages.Count; i++)
        {
            var bounds = GetTabRect(i);
            var selected = i == SelectedIndex;
            using var back = new SolidBrush(selected ? TabActive : TabIdle);
            g.FillRectangle(back, bounds);

            if (selected)
            {
                using var accent = new SolidBrush(Accent);
                g.FillRectangle(accent, new Rectangle(bounds.Left + 2, stripBottom - 2, bounds.Width - 4, 3));
            }

            var textColor = selected ? TextActive : TextIdle;
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, TabPages[i].Text, selected ? UiTypography.BodySemibold(9f) : UiTypography.Body(9f),
                bounds, textColor, flags);

            if (selected && Focused)
                ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(bounds, -4, -4));
        }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        // Todo o desenho é feito em OnPaint; nada aqui.
    }
}
