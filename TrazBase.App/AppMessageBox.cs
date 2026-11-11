namespace TrazBase.App;

// Mantém a mesma chamada usada pela aplicação, mas exibe sempre a janela própria.
internal static class MessageBox
{
    public static DialogResult Show(
        IWin32Window owner,
        string text,
        string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1) =>
        AppMessageBox.Show(owner, text, caption, buttons, icon, defaultButton);
}

internal static class AppMessageBox
{
    /// <summary>
    /// Disparado quando qualquer diálogo próprio é exibido (legenda, texto). Usado para notificar o usuário quando a janela está minimizada.
    /// </summary>
    internal static event Action<string, string>? DialogShown;

    internal static void RaiseDialogShown(string caption, string body) => DialogShown?.Invoke(caption, body);

    public static DialogResult ShowFolderConflict(IWin32Window owner, string text)
    {
        (string Label, DialogResult Result, bool Primary)[] choices =
        [
            ("Cancelar", DialogResult.Cancel, false),
            ("Criar backup", DialogResult.Yes, true),
            ("Substituir", DialogResult.No, false)
        ];
        using var dialog = new AppMessageDialog(owner, text, "Pasta de destino existente", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, choices);
        return dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Pergunta para qual base um .FBK de nome não reconhecido deve ser restaurado.
    /// Retorna "ARQEMP", "ARQOCOR" ou "ARQSIST"; null quando o operador escolhe pular o arquivo.
    /// </summary>
    public static string? ShowRestoreDestinationChoice(IWin32Window owner, string fileName)
    {
        (string Label, DialogResult Result, bool Primary)[] choices =
        [
            ("ARQEMP", DialogResult.Yes, true),
            ("ARQOCOR", DialogResult.No, false),
            ("ARQSIST", DialogResult.Retry, false),
            ("Pular", DialogResult.Cancel, false)
        ];
        using var dialog = new AppMessageDialog(owner,
            $"Não foi possível identificar o conteúdo de:\n{fileName}\n\nPara qual base deve ser restaurado?",
            "Identificar base", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1, choices);
        return dialog.ShowDialog(owner) switch
        {
            DialogResult.Yes => "ARQEMP",
            DialogResult.No => "ARQOCOR",
            DialogResult.Retry => "ARQSIST",
            _ => null
        };
    }

    public static DialogResult Show(
        IWin32Window owner,
        string text,
        string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
    {
        using var dialog = new AppMessageDialog(owner, text, caption, buttons, icon, defaultButton);
        return dialog.ShowDialog(owner);
    }
}

internal sealed class AppMessageDialog : Form
{
    private readonly MessageBoxButtons _buttons;
    private readonly string _body;
    private bool _dragging;
    private Point _dragOffset;

    public AppMessageDialog(
        IWin32Window owner,
        string text,
        string caption,
        MessageBoxButtons buttons,
        MessageBoxIcon icon,
        MessageBoxDefaultButton defaultButton,
        (string Label, DialogResult Result, bool Primary)[]? customButtons = null)
    {
        _body = text;
        _buttons = buttons;
        var ownerControl = owner as Control;
        var dark = ownerControl is not null && ownerControl.BackColor.R < 70;
        var background = dark ? Color.FromArgb(34, 42, 55) : Color.White;
        var headerColor = dark ? Color.FromArgb(28, 35, 47) : Color.FromArgb(31, 111, 235);
        var foreground = dark ? Color.FromArgb(232, 237, 245) : Color.FromArgb(29, 42, 61);
        var border = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(208, 217, 230);

        var messageFont = UiTypography.Body(10f);
        var (glyph, glyphColor) = GetIcon(icon);
        var textLeft = glyph.Length > 0 ? 78 : 24;
        var textWidth = 560 - textLeft - 24;
        var measured = TextRenderer.MeasureText(text, messageFont, new Size(textWidth, 1000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        var messageHeight = Math.Clamp(measured.Height + 8, 42, 300);
        var messageAreaHeight = Math.Max(messageHeight + 28, 72);
        ClientSize = new Size(560, 44 + messageAreaHeight + 62 + 2);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        var screen = ownerControl is not null ? Screen.FromControl(ownerControl) : Screen.FromHandle(owner?.Handle ?? IntPtr.Zero);
        Location = new Point(
            screen.WorkingArea.Left + Math.Max(0, (screen.WorkingArea.Width - ClientSize.Width) / 2),
            screen.WorkingArea.Top + Math.Max(0, (screen.WorkingArea.Height - ClientSize.Height) / 2));
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = border;
        Padding = new Padding(1);
        Font = UiTypography.Body();
        Text = caption;

        var surface = new Panel { Dock = DockStyle.Fill, BackColor = background };
        Controls.Add(surface);

        var dialogWidth = ClientSize.Width - 2;
        var dialogHeight = ClientSize.Height - 2;
        var header = new Panel { Location = Point.Empty, Size = new Size(dialogWidth, 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = headerColor, Cursor = Cursors.Arrow };
        surface.Controls.Add(header);
        var captionLabel = new Label
        {
            Text = caption,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = UiTypography.BodySemibold(10f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Location = new Point(18, 0),
            Size = new Size(ClientSize.Width - 70, 44),
            Cursor = Cursors.Arrow
        };
        header.Controls.Add(captionLabel);
        WireDrag(header);
        WireDrag(captionLabel);

        var close = new Button
        {
            Text = "×",
            Size = new Size(42, 44),
            Location = new Point(ClientSize.Width - 43, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = headerColor,
            Font = UiTypography.Body(15f),
            TabStop = false
        };
        close.FlatAppearance.BorderSize = 0;
        // idêntico ao botão de fechar da janela do Windows (hover vermelho #C42B1C, pressionar #A11F15)
        close.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xC4, 0x2B, 0x1C);
        close.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xA1, 0x1F, 0x15);
        close.Click += (_, _) => CloseWithDefaultCancelResult();
        header.Controls.Add(close);

        var buttonPanel = new Panel { Location = new Point(0, dialogHeight - 62), Size = new Size(dialogWidth, 62), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = background };
        surface.Controls.Add(buttonPanel);

        var messageArea = new Panel { Location = new Point(0, 44), Size = new Size(dialogWidth, messageAreaHeight), BackColor = background };
        surface.Controls.Add(messageArea);

        if (glyph.Length > 0)
        {
            var iconBadge = new Panel
            {
                Location = new Point(22, (messageAreaHeight - 40) / 2),
                Size = new Size(40, 40),
                BackColor = Color.FromArgb(24, glyphColor)
            };
            messageArea.Controls.Add(iconBadge);
            iconBadge.Controls.Add(new Label
            {
                Text = glyph,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = glyphColor,
                Font = UiTypography.BodySemibold(17f)
            });
        }

        var body = new Label
        {
            Text = text,
            Location = new Point(textLeft, (messageAreaHeight - messageHeight) / 2),
            Size = new Size(textWidth, messageHeight),
            ForeColor = foreground,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
            Font = messageFont,
            UseMnemonic = false
        };
        messageArea.Controls.Add(body);

        var buttonSpecs = customButtons ?? GetButtons(buttons);
        const int buttonHeight = 32;
        const int buttonGap = 8;
        var buttonWidths = buttonSpecs
            .Select(spec => Math.Max(88, TextRenderer.MeasureText(spec.Label, UiTypography.BodySemibold(9f)).Width + 28))
            .ToArray();
        var buttonX = buttonPanel.ClientSize.Width - 18;
        for (var index = 0; index < buttonSpecs.Length; index++)
        {
            var (label, result, primary) = buttonSpecs[index];
            buttonX -= buttonWidths[index];
            var button = new Button
            {
                Text = label,
                DialogResult = result,
                Size = new Size(buttonWidths[index], buttonHeight),
                Location = new Point(buttonX, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Color.FromArgb(31, 111, 235) : (dark ? Color.FromArgb(43, 53, 69) : Color.White),
                ForeColor = primary ? Color.White : foreground,
                Font = UiTypography.BodySemibold(9f)
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.MouseOverBackColor = primary
                ? Color.FromArgb(45, 125, 245)
                : (dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(244, 247, 251));
            buttonPanel.Controls.Add(button);
            buttonX -= buttonGap;

            var requestedDefault = (int)defaultButton - 1;
            if (index == Math.Clamp(requestedDefault, 0, buttonSpecs.Length - 1)) AcceptButton = button;
        }

        CancelButton = buttonPanel.Controls.OfType<Button>()
            .FirstOrDefault(button => button.DialogResult == DialogResult.Cancel);

        if (CancelButton is null)
            KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Escape) return;
                CloseWithDefaultCancelResult();
                e.Handled = true;
            };

        ActiveControl = AcceptButton as Control;
    }

    private void WireDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragOffset = new Point(Cursor.Position.X - Left, Cursor.Position.Y - Top);
            control.Capture = true;
        };
        control.MouseMove += (_, e) =>
        {
            if (!_dragging) return;
            Location = new Point(Cursor.Position.X - _dragOffset.X, Cursor.Position.Y - _dragOffset.Y);
        };
        control.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = false;
            control.Capture = false;
        };
        control.MouseLeave += (_, _) => { _dragging = false; control.Capture = false; };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (Text is not null) AppMessageBox.RaiseDialogShown(Text, GetMessagePreview(_body));
    }

    private static string GetMessagePreview(string text)
    {
        var firstLine = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
        return firstLine.Length <= 120 ? firstLine : firstLine[..120] + "…";
    }

    private void CloseWithDefaultCancelResult()
    {
        DialogResult = _buttons switch
        {
            MessageBoxButtons.YesNo => DialogResult.No,
            MessageBoxButtons.AbortRetryIgnore => DialogResult.Ignore,
            _ => DialogResult.Cancel
        };
        Close();
    }

    private static (string Glyph, Color Color) GetIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Error => ("×", Color.FromArgb(220, 76, 86)),
        MessageBoxIcon.Warning => ("!", Color.FromArgb(230, 166, 50)),
        MessageBoxIcon.Question => ("?", Color.FromArgb(31, 111, 235)),
        MessageBoxIcon.Information => ("i", Color.FromArgb(31, 111, 235)),
        _ => ("", Color.Transparent)
    };

    private static (string Label, DialogResult Result, bool Primary)[] GetButtons(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OKCancel => [("OK", DialogResult.OK, true), ("Cancelar", DialogResult.Cancel, false)],
        MessageBoxButtons.YesNo => [("Sim", DialogResult.Yes, true), ("Não", DialogResult.No, false)],
        MessageBoxButtons.YesNoCancel => [("Sim", DialogResult.Yes, true), ("Não", DialogResult.No, false), ("Cancelar", DialogResult.Cancel, false)],
        MessageBoxButtons.RetryCancel => [("Tentar novamente", DialogResult.Retry, true), ("Cancelar", DialogResult.Cancel, false)],
        MessageBoxButtons.AbortRetryIgnore => [("Abortar", DialogResult.Abort, false), ("Tentar novamente", DialogResult.Retry, false), ("Ignorar", DialogResult.Ignore, true)],
        _ => [("OK", DialogResult.OK, true)]
    };
}
