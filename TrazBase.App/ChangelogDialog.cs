namespace TrazBase.App;

internal sealed class ChangelogDialog : Form
{
    private const int ClientWidth = 660;
    private const int ClientHeight = 500;

    public ChangelogDialog(IWin32Window owner, Version version)
    {
        var notes = Changelog.GetNotesText(version);
        var dark = owner is MainForm mainForm && mainForm.BackColor.R < 70;

        var background = dark ? Color.FromArgb(34, 42, 55) : Color.White;
        var headerColor = dark ? Color.FromArgb(28, 35, 47) : Color.FromArgb(31, 111, 235);
        var foreground = dark ? Color.FromArgb(232, 237, 245) : Color.FromArgb(29, 42, 61);
        var border = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(208, 217, 230);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        BackColor = border;
        Padding = new Padding(1);
        Font = UiTypography.Body();
        ClientSize = new Size(ClientWidth, ClientHeight);
        Text = $"Novidades — Versão {version.ToString(3)}";

        var screen = Screen.FromHandle(owner.Handle);
        Location = new Point(
            screen.WorkingArea.Left + Math.Max(0, (screen.WorkingArea.Width - ClientSize.Width) / 2),
            screen.WorkingArea.Top + Math.Max(0, (screen.WorkingArea.Height - ClientSize.Height) / 2));

        var surface = new Panel { Dock = DockStyle.Fill, BackColor = background };
        Controls.Add(surface);

        var dialogWidth = ClientSize.Width - 2;
        var dialogHeight = ClientSize.Height - 2;

        var header = new Panel { Location = Point.Empty, Size = new Size(dialogWidth, 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = headerColor, Cursor = Cursors.Arrow };
        surface.Controls.Add(header);
        var titleLeft = new Label { Text = $"Novidades — versão {version.ToString(3)}", ForeColor = Color.White, BackColor = Color.Transparent, Font = UiTypography.BodySemibold(10f), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Location = new Point(18, 0), Size = new Size(dialogWidth - 80, 44), Cursor = Cursors.SizeAll };
        header.Controls.Add(titleLeft);
        DraggableWindow.Wire(header, this);
        DraggableWindow.Wire(titleLeft, this);

        var close = new Button
        {
            Text = "×",
            Size = new Size(42, 44),
            Location = new Point(dialogWidth - 43, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = headerColor,
            Font = UiTypography.Body(15f),
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        close.FlatAppearance.BorderSize = 0;
        close.BackColor = headerColor;
        close.FlatAppearance.MouseOverBackColor = headerColor;
        close.FlatAppearance.MouseDownBackColor = headerColor;
        close.MouseEnter += (_, _) => close.BackColor = Color.FromArgb(0xC4, 0x2B, 0x1C);
        close.MouseLeave += (_, _) => close.BackColor = headerColor;
        close.MouseDown += (_, _) => close.BackColor = Color.FromArgb(0xA1, 0x1F, 0x15);
        close.MouseUp += (_, _) => close.BackColor = Color.FromArgb(0xC4, 0x2B, 0x1C);
        close.Click += (_, _) => Close();
        header.Controls.Add(close);

        var bodyHeight = dialogHeight - 44 - 62;
        var contentPanel = new Panel { Location = new Point(0, 44), Size = new Size(dialogWidth, bodyHeight), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = background };
        surface.Controls.Add(contentPanel);

        var content = new RichTextBox
        {
            Location = new Point(18, 14),
            Size = new Size(dialogWidth - 36, bodyHeight - 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = true,
            BackColor = Color.FromArgb(18, 25, 38),
            ForeColor = Color.FromArgb(220, 229, 242),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 10f),
            Text = notes
        };
        contentPanel.Controls.Add(content);

        var buttonPanel = new Panel { Location = new Point(0, dialogHeight - 62), Size = new Size(dialogWidth, 62), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = background };
        surface.Controls.Add(buttonPanel);

        var ok = new Button
        {
            Text = "Entendi",
            Size = new Size(180, 36),
            Location = new Point(dialogWidth - 180 - 18, 13),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(31, 111, 235),
            ForeColor = Color.White,
            FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(45, 125, 245), MouseDownBackColor = Color.FromArgb(25, 94, 205) },
            Font = UiTypography.BodySemibold(10f)
        };
        ok.Click += (_, _) => Close();
        ok.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 125, 245);
        ok.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 94, 205);
        buttonPanel.Controls.Add(ok);

        AcceptButton = ok;
        ActiveControl = ok;
    }
}
