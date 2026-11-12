namespace TrazBase.App;

internal sealed class ThemedProgressBar : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private int _value;
    private int _marqueeOffset;
    private bool _isMarquee;

    internal Color TrackColor = Color.FromArgb(38, 47, 62);
    internal Color FillColor = Color.FromArgb(31, 111, 235);
    internal Color BorderColor = Color.FromArgb(57, 68, 84);

    internal void SetValue(int value)
    {
        var next = Math.Clamp(value, 0, 100);
        if (_value == next) return;
        _value = next;
        Invalidate();
    }

    internal void SetMarquee(bool value)
    {
        if (_isMarquee == value) return;
        _isMarquee = value;
        if (value) _timer.Start();
        else _timer.Stop();
        Invalidate();
    }

    public ThemedProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = 16;
        _timer.Tick += (_, _) =>
        {
            _marqueeOffset = (_marqueeOffset + Math.Max(2, ClientSize.Width / 35)) % Math.Max(1, ClientSize.Width);
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var bounds = ClientRectangle;
        if (bounds.Width < 3 || bounds.Height < 3) return;
        bounds.Inflate(-1, -1);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var track = new SolidBrush(TrackColor);
        using var border = new Pen(BorderColor);
        e.Graphics.FillRectangle(track, bounds);

        var fillWidth = _isMarquee ? Math.Max(20, bounds.Width / 3) : (int)(bounds.Width * _value / 100.0);
        var fillX = _isMarquee ? bounds.Left + _marqueeOffset - fillWidth : bounds.Left;
        var fillBounds = Rectangle.Intersect(bounds, new Rectangle(fillX, bounds.Top, fillWidth, bounds.Height));
        if (fillBounds.Width > 0)
        {
            using var fill = new SolidBrush(FillColor);
            e.Graphics.FillRectangle(fill, fillBounds);
        }
        e.Graphics.DrawRectangle(border, bounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}

internal interface IThemedScrollingControl
{
    void RefreshNativeTheme();
}

/// <summary>
/// Botão de ação/fluxo com cantos realmente arredondados (desenho próprio).
/// O nativo do Windows 11 desenha uma moldura retangular plana que vaza
/// sobre o preenchimento do botão; aqui o caminho é recortado tanto na
/// pintura quanto na área de clique (Region), evitando o sangramento nos cantos.
/// </summary>
internal class RoundedButton : Button
{
    private bool _pointerOver;
    private bool _pressed;

    /// <summary>Raio dos cantos em pixels. 0 deixa o botão retangular (usado no botão de fechar).</summary>
    [System.ComponentModel.DefaultValue(8)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int CornerRadius { get; set; } = 8;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            SetStyle(ControlStyles.ApplyThemingImplicitly, false);
            return cp;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        using var path = CreateRoundRect(ClientRectangle, Math.Max(0, CornerRadius));
        Region = path is null ? null : new Region(path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath? CreateRoundRect(Rectangle bounds, int radius)
    {
        if (bounds.Width < 1 || bounds.Height < 1) return null;
        if (radius <= 0)
        {
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddRectangle(bounds);
            return p;
        }

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
        var path2 = new System.Drawing.Drawing2D.GraphicsPath();
        path2.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path2.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path2.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path2.AddArc(arc, 90, 90);
        path2.CloseFigure();
        return path2;
    }

    protected override void OnMouseEnter(EventArgs e) { _pointerOver = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _pointerOver = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    private static Color Blend(Color baseColor, Color target, float amount)
    {
        var r = (int)Math.Round(baseColor.R + (target.R - baseColor.R) * amount);
        var g = (int)Math.Round(baseColor.G + (target.G - baseColor.G) * amount);
        var b = (int)Math.Round(baseColor.B + (target.B - baseColor.B) * amount);
        return Color.FromArgb(baseColor.A, r, g, b);
    }

    private static float Luminance(Color color) =>
        (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255f;

    private Color HoverColor()
    {
        if (FlatAppearance.MouseOverBackColor != Color.Empty) return FlatAppearance.MouseOverBackColor;
        return Blend(BackColor, Luminance(BackColor) < 0.5f ? Color.White : Color.Black,
            Luminance(BackColor) < 0.5f ? 0.16f : 0.12f);
    }

    private Color PressedColor()
    {
        if (FlatAppearance.MouseDownBackColor != Color.Empty) return FlatAppearance.MouseDownBackColor;
        return Blend(BackColor, Luminance(BackColor) < 0.5f ? Color.White : Color.Black,
            Luminance(BackColor) < 0.5f ? 0.28f : 0.22f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var fill = _pressed ? PressedColor()
            : _pointerOver ? HoverColor()
            : BackColor;
        var bounds = ClientRectangle;
        if (bounds.Width < 2 || bounds.Height < 2) { base.OnPaint(e); return; }
        var drawBounds = Rectangle.Inflate(bounds, -1, -1);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(24, 30, 40));

        var radius = CornerRadius <= 0 ? 0 : Math.Min(CornerRadius, Math.Min(drawBounds.Width, drawBounds.Height) / 2);
        using var path = CreateRoundRect(drawBounds, radius);
        if (path is not null)
        {
            using var fillBrush = new SolidBrush(fill);
            e.Graphics.FillPath(fillBrush, path);
            if (FlatAppearance.BorderSize > 0 && FlatAppearance.BorderColor != Color.Empty)
            {
                using var borderPen = new Pen(FlatAppearance.BorderColor, Math.Max(1, FlatAppearance.BorderSize))
                {
                    Alignment = System.Drawing.Drawing2D.PenAlignment.Inset
                };
                e.Graphics.DrawPath(borderPen, path);
            }
        }

        if (!Enabled)
        {
            using var disabledFill = new SolidBrush(Color.FromArgb(80, 120, 128, 143));
            if (path is not null) e.Graphics.FillPath(disabledFill, path);
        }

        var textColor = Enabled ? ForeColor : SystemColors.GrayText;
        var textBounds = Rectangle.Inflate(bounds, -Padding.Horizontal, -Padding.Vertical);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        flags |= TextAlign switch
        {
            ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        if (_pressed && FlatAppearance.BorderSize == 0)
            textBounds.Offset(0, 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, textColor, flags);
    }
}

internal sealed class PlainNavigationButton : Button
{
    private bool _pointerOver;
    private bool _pressed;

    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, false);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            return base.CreateParams;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _pointerOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _pointerOver = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _pressed = e.Button == MouseButtons.Left;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var color = _pressed && FlatAppearance.MouseDownBackColor != Color.Empty
            ? FlatAppearance.MouseDownBackColor
            : _pointerOver && FlatAppearance.MouseOverBackColor != Color.Empty
                ? FlatAppearance.MouseOverBackColor
                : BackColor;
        using (var background = new SolidBrush(color)) e.Graphics.FillRectangle(background, ClientRectangle);

        var image = Image;
        if (image is not null)
        {
            var imageX = ImageAlign == ContentAlignment.MiddleCenter
                ? (ClientSize.Width - image.Width) / 2
                : Padding.Left;
            var imageY = (ClientSize.Height - image.Height) / 2;
            e.Graphics.DrawImage(image, imageX, imageY, image.Width, image.Height);
        }

        if (string.IsNullOrEmpty(Text)) return;
        var textLeft = Padding.Left;
        var textWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal);
        if (image is not null && ImageAlign != ContentAlignment.MiddleCenter)
        {
            textLeft += image.Width + 10;
            textWidth = Math.Max(0, ClientSize.Width - textLeft - Padding.Right);
        }
        var textBounds = new Rectangle(textLeft, Padding.Top, textWidth, Math.Max(0, ClientSize.Height - Padding.Vertical));
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        flags |= TextAlign switch
        {
            ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Enabled ? ForeColor : SystemColors.GrayText, flags);
    }
}

internal sealed class ThemedListView : ListView, IThemedScrollingControl
{
    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            return base.CreateParams;
        }
    }

    public void RefreshNativeTheme() => RecreateHandle();
}

internal sealed class ThemedTextBox : TextBox, IThemedScrollingControl
{
    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            return base.CreateParams;
        }
    }

    public void RefreshNativeTheme() => RecreateHandle();
}

internal sealed class ThemedRichTextBox : RichTextBox, IThemedScrollingControl
{
    protected override CreateParams CreateParams
    {
        get
        {
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            return base.CreateParams;
        }
    }

    public void RefreshNativeTheme() => RecreateHandle();
}
