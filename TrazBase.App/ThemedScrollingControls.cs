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
