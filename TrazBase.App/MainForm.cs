using Microsoft.Win32;
using System.IO.Compression;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace TrazBase.App;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Width = 220 };
    private readonly CheckBox _savePassword = new() { Text = "Salvar senha", AutoSize = true, ForeColor = Color.FromArgb(29, 42, 61), Anchor = AnchorStyles.Left };
    private readonly CheckBox _darkMode = new() { Text = "Usar modo escuro", AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly TextBox _sl = new() { Width = 180, PlaceholderText = "Ex.: 123456" };
    private readonly ThemedTextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 25, 38), ForeColor = Color.FromArgb(220, 229, 242), BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10f) };
    private readonly Label _status = new() { Text = "", AutoSize = true, ForeColor = Color.FromArgb(91, 110, 132) };
    private readonly Label _versionLabel = new() { Text = "", AutoSize = true, Visible = false, ForeColor = Color.FromArgb(0, 120, 255), Font = UiTypography.Body(14f), Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private readonly Panel _progress = new() { Dock = DockStyle.None, BackColor = Color.FromArgb(222, 228, 237), BorderStyle = BorderStyle.FixedSingle, Visible = false, Width = 650, Height = 20 };
    private readonly Panel _progressFill = new() { Dock = DockStyle.None, BackColor = Color.FromArgb(31, 111, 235), Location = new Point(0, 0), Width = 0, Height = 18 };
    private int _progressPercent;
    private readonly Button _start = new() { Text = "Trazer e preparar base", Width = 220, Height = 42, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Button _cancel = new() { Text = "Cancelar operação", Width = 170, Height = 42, Visible = false, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Panel _progressFooter = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly TableLayoutPanel _shell = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    private readonly PlainNavigationButton _menuToggle = new() { Text = "", Image = SvgIconLoader.Load("menu.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 54, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _transferNav = new() { Text = "", Image = SvgIconLoader.Load("Transferir.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _changeBaseNav = new() { Text = "", Image = SvgIconLoader.Load("Trocar-Base.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _settingsNav = new() { Text = "", Image = SvgIconLoader.Load("Configuracao.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _arqEmpNav = new() { Text = "", Image = SvgIconLoader.Load("SQL.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _arqSistNav = new() { Text = "", Image = SvgIconLoader.Load("SQL.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private TableLayoutPanel _transferView = null!;
    private Control _settingsView = null!;
    private Control _changeBaseView = null!;
    private Control _arqEmpView = null!;
    private Control _arqSistView = null!;
    private Panel _settingsFieldsHost = null!;
    private readonly ThemedListView _baseFolders = new() { Dock = DockStyle.Fill, View = View.List, BorderStyle = BorderStyle.None, FullRowSelect = true, HideSelection = false, MultiSelect = false };
    private readonly Label _baseFoldersRootLabel = new() { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TextBox _selectedBasePath = new() { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(249, 251, 254), Dock = DockStyle.Fill };
    private readonly Button _changeBaseButton = new() { Text = "Trocar base", Width = 118, Height = 30, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Button _openBaseFolderButton = new() { Text = "Abrir pasta", Width = 118, Height = 30, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly TextBox _arqEmpDatabase = new();
    private readonly TextBox _arqSistDatabase = new();
    private readonly RichTextBox _arqEmpEditor = CreateSqlEditor();
    private readonly RichTextBox _arqSistEditor = CreateSqlEditor();
    private readonly RichTextBox _arqEmpOutput = CreateSqlOutput();
    private readonly RichTextBox _arqSistOutput = CreateSqlOutput();
    private readonly System.Windows.Forms.Timer _commandSaveTimer = new() { Interval = 700 };
    private bool _menuExpanded;

    private static readonly Color Ink = Color.FromArgb(29, 42, 61);
    private static readonly Color Muted = Color.FromArgb(97, 113, 134);
    private static readonly Color Surface = Color.White;

    public MainForm()
    {
        MigrateLegacySqlScripts();
        Text = "Traz Base | Solicitações";
        Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Imagens", "icone", "icone_supra.ico"));
        MinimumSize = new Size(1020, 760);
        Size = new Size(1180, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(244, 247, 251);
        Font = UiTypography.Body();

        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _shell.Controls.Add(_sidebar, 0, 0);
        _shell.Controls.Add(_contentHost, 1, 0);
        _sidebar.Controls.Add(_settingsNav);
        _sidebar.Controls.Add(_arqSistNav);
        _sidebar.Controls.Add(_arqEmpNav);
        _sidebar.Controls.Add(_changeBaseNav);
        _sidebar.Controls.Add(_transferNav);
        _sidebar.Controls.Add(_menuToggle);
        _transferView = BuildTransferView();
        _settingsView = BuildSettingsView();
        _changeBaseView = BuildChangeBaseView();
        _arqEmpView = BuildCommandView("Comandos ARQEMP", _arqEmpDatabase, _arqEmpEditor, _arqEmpOutput, "ARQEMP");
        _arqSistView = BuildCommandView("Comandos ARQSIST", _arqSistDatabase, _arqSistEditor, _arqSistOutput, "ARQSIST");
        _arqEmpDatabase.Text = _settings.ArqEmpCommandDatabase;
        _arqSistDatabase.Text = _settings.ArqSistCommandDatabase;
        _arqEmpEditor.Text = _settings.ArqEmpCommands;
        _arqSistEditor.Text = _settings.ArqSistCommands;
        HighlightSql(_arqEmpEditor);
        HighlightSql(_arqSistEditor);
        _contentHost.Controls.Add(_settingsView);
        _contentHost.Controls.Add(_changeBaseView);
        _contentHost.Controls.Add(_arqEmpView);
        _contentHost.Controls.Add(_arqSistView);
        _contentHost.Controls.Add(_transferView);
        Controls.Add(_shell);
        SetMenuExpanded(false);
        ShowView(settings: false);
        _darkMode.Checked = _settings.DarkMode;
        _darkMode.CheckedChanged += (_, _) =>
        {
            _settings.DarkMode = _darkMode.Checked;
            Application.SetColorMode(_settings.DarkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
            RefreshNativeScrollingThemes();
            ApplyTheme();
            try { PersistSettings(); }
            catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar a preferência de tema: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        HandleCreated += (_, _) => ApplyTitleBarTheme();
        ApplyTheme();
        _password.Text = _settings.GetFirebirdPassword() ?? "";
        _savePassword.Checked = !string.IsNullOrWhiteSpace(_settings.ProtectedFirebirdPassword);
        _savePassword.CheckedChanged += (_, _) => SavePasswordPreference();
        _password.Leave += (_, _) => { if (_savePassword.Checked) SavePasswordPreference(); };
        AcceptButton = _start;
        var passwordTip = new ToolTip();
        passwordTip.SetToolTip(_savePassword, "A senha será protegida pelo Windows para sua conta de usuário.");
        _start.Click += async (_, _) => await StartFlowAsync();
        _cancel.Click += (_, _) => { if (_running) { _runCancellation?.Cancel(); _cancel.Enabled = false; _status.Text = "Cancelando…"; } };
        _menuToggle.Click += (_, _) => SetMenuExpanded(!_menuExpanded);
        _transferNav.Click += (_, _) => ShowView(settings: false);
        _settingsNav.Click += (_, _) => ShowView(settings: true);
        _changeBaseNav.Click += (_, _) => ShowChangeBaseView();
        _baseFolders.SelectedIndexChanged += (_, _) => _selectedBasePath.Text = _baseFolders.SelectedItems.Count == 0 ? "" : _baseFolders.SelectedItems[0].Tag as string ?? "";
        _baseFolders.DoubleClick += async (_, _) => await ChangeSelectedBaseAsync();
        _changeBaseButton.Click += async (_, _) => await ChangeSelectedBaseAsync();
        _openBaseFolderButton.Click += (_, _) => OpenSelectedBaseFolder();
        _arqEmpNav.Click += (_, _) => ShowCommandView(arqEmp: true);
        _arqSistNav.Click += (_, _) => ShowCommandView(arqEmp: false);
        _commandSaveTimer.Tick += (_, _) =>
        {
            _commandSaveTimer.Stop();
            SaveCommandSettings();
        };
        FormClosing += (sender, e) =>
        {
            if (_running)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Use o botão Cancelar e aguarde o encerramento da etapa atual.", "Operação em andamento", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _commandSaveTimer.Stop();
                try
                {
                    PersistSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Não foi possível salvar as configurações: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };
    }

    private bool _running;
    private CancellationTokenSource? _runCancellation;

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label { Text = "TRAZ BASE", Font = UiTypography.BodySemibold(), ForeColor = Color.FromArgb(31, 111, 235), AutoSize = true, Location = new Point(0, 2) });
        panel.Controls.Add(new Label { Text = "Preparar base por solicitação", Font = UiTypography.DisplaySemibold(21), ForeColor = Ink, AutoSize = true, Location = new Point(0, 28) });
        panel.Controls.Add(new Label { Text = "Cópia, extração e configuração Firebird em um único fluxo", Font = UiTypography.Body(), ForeColor = Muted, AutoSize = true, Location = new Point(1, 72) });
        return panel;
    }

    private Control BuildHeader(string title, string subtitle)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label { Text = "TRAZ BASE", Font = UiTypography.BodySemibold(), ForeColor = Color.FromArgb(31, 111, 235), AutoSize = true, Location = new Point(0, 2) });
        panel.Controls.Add(new Label { Text = title, Font = UiTypography.DisplaySemibold(21), ForeColor = Ink, AutoSize = true, Location = new Point(0, 28) });
        panel.Controls.Add(new Label { Text = subtitle, Font = UiTypography.Body(), ForeColor = Muted, AutoSize = true, Location = new Point(1, 72) });
        return panel;
    }

    private TableLayoutPanel BuildTransferView()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(26, 20, 26, 20) };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        page.Controls.Add(BuildHeader("Transferir", "Cópia, extração e configuração Firebird em um único fluxo"), 0, 0);
        page.Controls.Add(BuildActionPanel(), 0, 1);
        page.Controls.Add(BuildLogPanel(), 0, 2);
        page.Controls.Add(BuildProgressFooter(), 0, 3);
        return page;
    }

    private Control BuildSettingsView()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(26, 20, 26, 20) };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(BuildHeader("Configurações", "Pastas e conexão usadas na preparação das bases"), 0, 0);
        page.Controls.Add(BuildSettingsPanel(), 0, 1);
        return page;
    }

    private Control BuildChangeBaseView()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(26, 20, 26, 20) };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        page.Controls.Add(BuildHeader("Alterar base", "Selecione uma pasta para apontar o Registro para essa base"), 0, 0);

        page.Controls.Add(_baseFoldersRootLabel, 0, 1);
        var listCard = Card();
        _baseFolders.Font = UiTypography.Body(11f);
        _baseFolders.ItemSelectionChanged += (_, _) => _selectedBasePath.Text = _baseFolders.SelectedItems.Count == 0 ? "" : _baseFolders.SelectedItems[0].Tag as string ?? "";
        listCard.Controls.Add(_baseFolders);
        page.Controls.Add(listCard, 0, 2);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        bottom.Controls.Add(_selectedBasePath, 0, 0);
        _changeBaseButton.FlatAppearance.BorderSize = 0;
        _openBaseFolderButton.FlatAppearance.BorderColor = Color.FromArgb(208, 217, 230);
        _openBaseFolderButton.FlatAppearance.BorderSize = 1;
        bottom.Controls.Add(_changeBaseButton, 1, 0);
        bottom.Controls.Add(_openBaseFolderButton, 2, 0);
        page.Controls.Add(bottom, 0, 3);
        RefreshBaseFolders();
        return page;
    }

    private void RefreshBaseFolders()
    {
        _baseFolders.Items.Clear();
        var root = _settings.BasesRoot?.Trim();
        _baseFoldersRootLabel.Text = "Pastas encontradas em: " + (string.IsNullOrWhiteSpace(root) ? "(pasta local não configurada)" : root);
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
        foreach (var folder in Directory.GetDirectories(root).OrderBy(folder => Path.GetFileName(folder), StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new ListViewItem(Path.GetFileName(folder)) { Tag = folder };
            _baseFolders.Items.Add(item);
        }
    }

    private async Task ChangeSelectedBaseAsync()
    {
        var folder = _selectedBasePath.Text.Trim();
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "Selecione uma pasta de base existente na lista.", "Base não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _changeBaseButton.Enabled = false;
        try
        {
            await UpdateRegistryAsync(folder);
            _status.Text = "Base alterada para: " + folder;
            MessageBox.Show(this, $"O Registro foi atualizado para a base:\n{folder}", "Base alterada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível alterar a base no Registro:\n{ex.Message}", "Falha ao alterar base", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _changeBaseButton.Enabled = true; }
    }

    private void OpenSelectedBaseFolder()
    {
        var folder = _selectedBasePath.Text.Trim();
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "Selecione uma pasta de base existente na lista.", "Base não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, $"Não foi possível abrir a pasta:\n{ex.Message}", "Falha ao abrir pasta", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ShowChangeBaseView()
    {
        _transferView.Visible = false;
        _settingsView.Visible = false;
        _changeBaseView.Visible = true;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.FromArgb(231, 240, 255);
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = _changeBaseButton;
        RefreshBaseFolders();
        ApplyNavigationColors();
    }

    private Control BuildCommandView(string title, TextBox databasePath, RichTextBox editor, RichTextBox output, string databaseName)
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(26, 20, 26, 20)
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        page.Controls.Add(BuildHeader(title, $"Editor SQL para uma base {databaseName}.FDB"), 0, 0);

        var databaseCard = Card();
        var databaseLabel = new Label { Text = "Base Firebird (.FDB)", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f), Location = new Point(14, 14) };
        databasePath.Location = new Point(160, 9);
        var browse = new Button
        {
            Text = "Procurar",
            Size = new Size(110, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(31, 111, 235),
            ForeColor = Color.White,
            Font = UiTypography.BodySemibold()
        };
        browse.FlatAppearance.BorderSize = 0;
        browse.Location = new Point(databaseCard.ClientSize.Width - browse.Width - 18, 8);
        databaseCard.Resize += (_, _) => browse.Left = databaseCard.ClientSize.Width - browse.Width - 18;
        browse.Click += (_, _) => BrowseCommandDatabase(databasePath);
        databaseCard.Controls.Add(databaseLabel);
        databasePath.Size = new Size(Math.Max(120, databaseCard.ClientSize.Width - 160 - browse.Width - 44), 32);
        databasePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        databaseCard.Resize += (_, _) => databasePath.Width = Math.Max(120, databaseCard.ClientSize.Width - 160 - browse.Width - 44);
        databaseCard.Controls.Add(databasePath);
        databaseCard.Controls.Add(browse);
        page.Controls.Add(databaseCard, 0, 1);

        page.Controls.Add(BuildEditorCard("Comandos SQL", editor), 0, 2);
        editor.TextChanged += (_, _) =>
        {
            HighlightSql(editor);
            QueueCommandSettingsSave();
        };
        databasePath.TextChanged += (_, _) => QueueCommandSettingsSave();
        page.Controls.Add(BuildEditorCard("Resultado do teste", output), 0, 3);

        var actions = new Panel { Dock = DockStyle.Fill };
        var testButton = new Button
        {
            Text = "Testar comando",
            Size = new Size(180, 40),
            Location = new Point(0, 4),
            BackColor = Color.FromArgb(31, 111, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiTypography.BodySemibold()
        };
        testButton.FlatAppearance.BorderSize = 0;
        testButton.Click += async (_, _) => await TestSqlCommandAsync(databaseName, databasePath, editor, output, testButton);
        actions.Controls.Add(testButton);
        page.Controls.Add(actions, 0, 4);
        return page;
    }

    private Control BuildEditorCard(string title, Control editor)
    {
        var card = Card();
        var heading = new Label { Text = title, AutoSize = true, Font = UiTypography.BodySemibold(11), ForeColor = Ink, Location = new Point(16, 10) };
        var editorHost = new Panel
        {
            Location = new Point(16, 38),
            Size = new Size(Math.Max(100, card.ClientSize.Width - 50), Math.Max(60, card.ClientSize.Height - 56)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        editorHost.Controls.Add(editor);
        editorHost.Resize += (_, _) => editor.Size = editorHost.ClientSize;
        editor.Dock = DockStyle.Fill;
        card.Controls.Add(heading);
        card.Controls.Add(editorHost);
        card.Resize += (_, _) => editorHost.Size = new Size(Math.Max(100, card.ClientSize.Width - 50), Math.Max(60, card.ClientSize.Height - 56));
        return card;
    }

    private static ThemedRichTextBox CreateSqlEditor() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        AcceptsTab = true,
        WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both,
        BackColor = Color.FromArgb(18, 25, 38),
        ForeColor = Color.FromArgb(220, 229, 242),
        BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 11f)
    };

    private static ThemedRichTextBox CreateSqlOutput() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both,
        BackColor = Color.FromArgb(18, 25, 38),
        ForeColor = Color.FromArgb(220, 229, 242),
        BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 10f)
    };

    private static void HighlightSql(RichTextBox editor)
    {
        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;
        editor.SuspendLayout();
        editor.SelectAll();
        editor.SelectionColor = Color.FromArgb(220, 229, 242);
        const string pattern = """(?<comment>--[^\r\n]*|/\*[\s\S]*?\*/)|(?<string>'(?:''|[^'])*'|"(?:""|[^"])*")|(?<keyword>\b(?:SELECT|FROM|WHERE|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|ALTER|DROP|TABLE|VIEW|INDEX|TRIGGER|PROCEDURE|EXECUTE|BLOCK|RETURNS|AS|BEGIN|END|IF|THEN|ELSE|AND|OR|NOT|NULL|IS|IN|JOIN|LEFT|RIGHT|INNER|OUTER|ON|GROUP|BY|ORDER|HAVING|UNION|ALL|DISTINCT|COMMIT|ROLLBACK|GRANT|REVOKE|PRIMARY|KEY|FOREIGN|REFERENCES|CONSTRAINT|DEFAULT|DATABASE|PLAN|ROWS|FIRST|SKIP|CASE|WHEN|CAST|COUNT|SUM|AVG|MIN|MAX|STARTING|WITH|CONTAINING|LIKE|BETWEEN|EXISTS|RETURNING|SUSPEND|DECLARE|VARIABLE|EXCEPTION|MESSAGE|FOR|DO|WHILE|LIST|ASC|DESC|CURRENT_DATE|CURRENT_TIME|CURRENT_TIMESTAMP)\b)|(?<number>\b\d+(?:\.\d+)?\b)""";
        foreach (Match match in Regex.Matches(editor.Text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var color = match.Groups["comment"].Success ? Color.FromArgb(106, 153, 85)
                : match.Groups["string"].Success ? Color.FromArgb(206, 145, 120)
                : match.Groups["keyword"].Success ? Color.FromArgb(86, 156, 214)
                : Color.FromArgb(181, 206, 168);
            editor.Select(match.Index, match.Length);
            editor.SelectionColor = color;
        }
        editor.Select(selectionStart, selectionLength);
        editor.SelectionColor = Color.FromArgb(220, 229, 242);
        editor.ResumeLayout();
    }

    private void BrowseCommandDatabase(TextBox pathField)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            FileName = pathField.Text,
            Filter = "Bases Firebird (*.fdb;*.gdb)|*.fdb;*.gdb|Todos os arquivos (*.*)|*.*",
            Title = "Selecione a base Firebird"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) pathField.Text = dialog.FileName;
    }

    private async Task TestSqlCommandAsync(string databaseName, TextBox databasePath, RichTextBox editor, RichTextBox output, Button testButton)
    {
        var database = databasePath.Text.Trim();
        var sql = editor.Text.Trim();
        if (database.Length == 0 || !File.Exists(database))
        {
            MessageBox.Show(this, "Selecione um arquivo de base Firebird existente.", "Base não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (sql.Length == 0)
        {
            MessageBox.Show(this, "Digite um ou mais comandos SQL para testar.", "Comando vazio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            editor.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_password.Text))
        {
            MessageBox.Show(this, "Informe a senha Firebird na tela Configuração antes de testar comandos.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show(this,
            $"O SQL será executado diretamente em {databaseName} na base:\n{database}\n\nComandos de alteração podem modificar os dados. Deseja continuar?",
            "Confirmar execução SQL", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        testButton.Enabled = false;
        output.Clear();
        AppendCommandOutput(output, $"Conectando a {database}...");
        try
        {
            var connectionString = BuildFirebirdConnectionString(database);
            await Task.Run(() =>
            {
                using var connection = new FbConnection(connectionString);
                connection.Open();
                var script = new FbScript(sql);
                script.Parse();
                var batch = new FbBatchExecution(connection);
                batch.AppendSqlStatements(script);
                batch.Execute();
            });
            AppendCommandOutput(output, "Comando(s) executado(s) com sucesso.");
        }
        catch (Exception ex)
        {
            AppendCommandOutput(output, "Falha ao executar o SQL:");
            AppendCommandOutput(output, ex.Message);
        }
        finally { testButton.Enabled = true; }
    }

    private static void AppendCommandOutput(RichTextBox output, string message)
    {
        output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        output.SelectionStart = output.TextLength;
        output.ScrollToCaret();
    }

    private void SetMenuExpanded(bool expanded)
    {
        _menuExpanded = expanded;
        _shell.ColumnStyles[0].Width = expanded ? 300 : 68;
        _menuToggle.Text = expanded ? "Menu" : "";
        _transferNav.Text = expanded ? "Transferir" : "";
        _changeBaseNav.Text = expanded ? "Alterar base" : "";
        _settingsNav.Text = expanded ? "Configuração" : "";
        _arqEmpNav.Text = expanded ? "Comandos ARQEMP" : "";
        _arqSistNav.Text = expanded ? "Comandos ARQSIST" : "";
        _menuToggle.ImageAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
        _menuToggle.TextAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
        _menuToggle.Padding = expanded ? new Padding(18, 0, 4, 0) : Padding.Empty;
        _menuToggle.FlatAppearance.BorderSize = 0;
        _menuToggle.Font = UiTypography.Body(12f);
        foreach (var button in new[] { _transferNav, _changeBaseNav, _settingsNav, _arqEmpNav, _arqSistNav })
        {
            button.Font = UiTypography.Body(12f);
            button.ImageAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            button.TextAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            button.Padding = expanded ? new Padding(18, 0, 4, 0) : Padding.Empty;
            button.FlatAppearance.BorderSize = 0;
        }
    }

    private void ShowView(bool settings)
    {
        _transferView.Visible = !settings;
        _settingsView.Visible = settings;
        _changeBaseView.Visible = false;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _transferNav.BackColor = settings ? Color.White : Color.FromArgb(231, 240, 255);
        _changeBaseNav.BackColor = Color.White;
        _settingsNav.BackColor = settings ? Color.FromArgb(231, 240, 255) : Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = settings ? null : _start;
        ApplyNavigationColors();
    }

    private void ShowCommandView(bool arqEmp)
    {
        _transferView.Visible = false;
        _settingsView.Visible = false;
        _changeBaseView.Visible = false;
        _arqEmpView.Visible = arqEmp;
        _arqSistView.Visible = !arqEmp;
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.White;
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = arqEmp ? Color.FromArgb(231, 240, 255) : Color.White;
        _arqSistNav.BackColor = arqEmp ? Color.White : Color.FromArgb(231, 240, 255);
        AcceptButton = null;
        ApplyNavigationColors();
    }

    private Control BuildSettingsPanel()
    {
        var card = Card();
        var title = new Label { Text = "Configuração", Font = UiTypography.BodySemibold(12), ForeColor = Ink, AutoSize = true, Location = new Point(18, 13) };
        card.Controls.Add(title);
        var host = _settingsFieldsHost = new Panel
        {
            Location = new Point(18, 45),
            Size = new Size(Math.Max(100, card.ClientSize.Width - 54), 192),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = Padding.Empty
        };
        var labels = new[]
        {
            new Label { Text = "Origem das solicitações", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Pasta local das bases", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Pasta de bases de apoio", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Senha Firebird", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) }
        };
        var keys = new[] { nameof(AppSettings.SolicitationRoot), nameof(AppSettings.BasesRoot), nameof(AppSettings.SupportRoot) };
        var pathFields = keys.Select(key => new TextBox
        {
            Text = GetSetting(key),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(249, 251, 254),
            Height = 30
        }).ToArray();
        var browseButtons = keys.Select(key =>
        {
            var button = new Button
            {
                Text = "Procurar",
                Size = new Size(120, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Ink,
                Font = UiTypography.Body(9f),
                Tag = key
            };
            button.Click += (sender, _) => Browse((string)((Button)sender!).Tag!, "folder");
            button.FlatAppearance.BorderSize = 1;
            return button;
        }).ToArray();

        foreach (var keyIndex in Enumerable.Range(0, keys.Length))
        {
            _fields[keys[keyIndex]] = pathFields[keyIndex];
            host.Controls.Add(labels[keyIndex]);
            host.Controls.Add(pathFields[keyIndex]);
            host.Controls.Add(browseButtons[keyIndex]);
        }
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.BackColor = Color.FromArgb(249, 251, 254);
        _password.Margin = Padding.Empty;
        _savePassword.Margin = Padding.Empty;
        host.Controls.Add(labels[3]);
        host.Controls.Add(_password);
        host.Controls.Add(_savePassword);
        _darkMode.Location = new Point(0, 151);
        host.Controls.Add(_darkMode);

        void LayoutSettingsRows()
        {
            const int fieldLeft = 180;
            const int buttonWidth = 120;
            const int rightInset = 18;
            for (var i = 0; i < labels.Length; i++)
            {
                var y = i * 36;
                labels[i].Location = new Point(0, y + 7);
                if (i < pathFields.Length)
                {
                    pathFields[i].Location = new Point(fieldLeft, y);
                    pathFields[i].Width = Math.Max(100, host.ClientSize.Width - fieldLeft - buttonWidth - rightInset - 8);
                    pathFields[i].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    browseButtons[i].Location = new Point(host.ClientSize.Width - buttonWidth - rightInset, y + 1);
                    browseButtons[i].Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }
                else
                {
                    _password.Location = new Point(fieldLeft, y);
                    _password.Width = Math.Max(100, host.ClientSize.Width - fieldLeft - _savePassword.Width - rightInset - 18);
                    _password.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    _password.Height = 30;
                    _savePassword.Location = new Point(host.ClientSize.Width - _savePassword.Width - rightInset, y + 6);
                    _savePassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }
            }
        }

        card.Resize += (_, _) => host.Width = Math.Max(100, card.ClientSize.Width - 54);
        host.Resize += (_, _) => LayoutSettingsRows();
        LayoutSettingsRows();
        card.Controls.Add(host);
        return card;
    }

    private Control BuildActionPanel()
    {
        var card = Card();
        var label = new Label { Text = "Número da solicitação (SL)", ForeColor = Muted, AutoSize = true, Font = UiTypography.Body(9f), Location = new Point(18, 17) };
        card.Controls.Add(label);
        _sl.Location = new Point(18, 42);
        _sl.Font = UiTypography.BodySemibold(11);
        card.Controls.Add(_sl);
        _start.Location = new Point(Math.Max(_sl.Right + 20, label.Right + 18), 31);
        _start.FlatAppearance.BorderSize = 0;
        card.Controls.Add(_start);
        _cancel.Location = new Point(450, 31);
        _cancel.FlatAppearance.BorderSize = 0;
        card.Controls.Add(_cancel);
        _versionLabel.Location = new Point(card.ClientSize.Width - _versionLabel.Width - 18, 24);
        card.Resize += (_, _) => _versionLabel.Left = card.ClientSize.Width - _versionLabel.Width - 18;
        card.Controls.Add(_versionLabel);
        return card;
    }

    private Control BuildProgressFooter()
    {
        _status.Location = new Point(2, 0);
        _progress.Location = new Point(2, 24);
        _progress.Size = new Size(650, 20);
        _progress.Controls.Add(_progressFill);
        _progress.Resize += (_, _) => UpdateProgressFill();
        _progressFooter.Resize += (_, _) => _progress.Width = Math.Max(100, _progressFooter.ClientSize.Width - 4);
        _progressFooter.Controls.Add(_status);
        _progressFooter.Controls.Add(_progress);
        return _progressFooter;
    }

    private Control BuildLogPanel()
    {
        var card = Card();
        var heading = new Panel { Dock = DockStyle.Top, Height = 42 };
        heading.Controls.Add(new Label { Text = "Andamento da operação", Font = UiTypography.BodySemibold(11), ForeColor = Ink, AutoSize = true, Location = new Point(18, 12) });
        var clear = new Button { Text = "Limpar", Size = new Size(108, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(0, 5), BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => _log.Clear();
        heading.Resize += (_, _) => clear.Location = new Point(Math.Max(18, heading.ClientSize.Width - clear.Width - 18), 5);
        heading.Controls.Add(clear);
        card.Controls.Add(_log);
        card.Controls.Add(heading);
        return card;
    }

    private static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(16), BorderStyle = BorderStyle.FixedSingle };

    private void ApplyTheme()
    {
        var dark = _settings.DarkMode;
        var background = dark ? Color.FromArgb(24, 30, 40) : Color.FromArgb(244, 247, 251);
        var surface = dark ? Color.FromArgb(34, 42, 55) : Color.White;
        var text = dark ? Color.FromArgb(232, 237, 245) : Ink;
        var muted = dark ? Color.FromArgb(166, 180, 199) : Muted;
        var input = dark ? Color.FromArgb(43, 53, 69) : Color.FromArgb(249, 251, 254);

        BackColor = background;
        ApplyTitleBarTheme();
        foreach (var control in EnumerateControls(this))
        {
            if (control == _log || control == _arqEmpEditor || control == _arqSistEditor || control == _arqEmpOutput || control == _arqSistOutput)
                continue; // SQL editors and the log retain their high-contrast palette.
            if (control is Panel panel)
            {
                if (panel == _progress) panel.BackColor = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(222, 228, 237);
                else if (panel == _progressFill) panel.BackColor = Color.FromArgb(31, 111, 235);
                else if (panel == _sidebar) panel.BackColor = dark ? Color.FromArgb(28, 35, 47) : Color.White;
                else if (panel == _settingsFieldsHost) panel.BackColor = surface;
                else panel.BackColor = panel.BorderStyle == BorderStyle.None ? background : surface;
            }
            else if (control is TableLayoutPanel || control is FlowLayoutPanel) control.BackColor = background;

            if (control is Label label)
            {
                label.ForeColor = label.Text == "TRAZ BASE" || label == _versionLabel
                    ? Color.FromArgb(31, 111, 235)
                    : label.Text is "Preparar base por solicitação" or "Transferir" or "Configurações" or "Comandos ARQEMP" or "Comandos ARQSIST"
                        ? text : muted;
                label.BackColor = Color.Transparent;
            }
            else if (control is ListView listView)
            {
                listView.BackColor = surface;
                listView.ForeColor = text;
            }
            else if (control is TextBox textBox) { textBox.BackColor = input; textBox.ForeColor = text; }
            else if (control is CheckBox checkBox) { checkBox.BackColor = Color.Transparent; checkBox.ForeColor = text; }
            else if (control is Button button)
            {
                var isNavigation = button == _menuToggle || button == _transferNav || button == _changeBaseNav || button == _settingsNav || button == _arqEmpNav || button == _arqSistNav;
                var isSecondaryAction = button == _openBaseFolderButton;
                button.ForeColor = isNavigation || isSecondaryAction ? text : Color.White;
                if (isNavigation)
                {
                    button.BackColor = dark ? Color.FromArgb(28, 35, 47) : Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.BorderColor = button.BackColor;
                    button.FlatAppearance.MouseOverBackColor = dark ? Color.FromArgb(38, 47, 62) : Color.FromArgb(244, 247, 251);
                    button.FlatAppearance.MouseDownBackColor = dark ? Color.FromArgb(48, 67, 94) : Color.FromArgb(231, 240, 255);
                }
                else if (isSecondaryAction)
                {
                    button.BackColor = surface;
                    button.FlatAppearance.BorderColor = dark ? Color.FromArgb(83, 98, 119) : Color.FromArgb(208, 217, 230);
                }
            }
        }
        ApplyNavigationColors();
        HighlightSql(_arqEmpEditor);
        HighlightSql(_arqSistEditor);
    }

    private void RefreshNativeScrollingThemes()
    {
        foreach (var control in EnumerateControls(this).OfType<IThemedScrollingControl>())
            control.RefreshNativeTheme();
    }

    private void ApplyNavigationColors()
    {
        var inactive = _settings.DarkMode ? Color.FromArgb(28, 35, 47) : Color.White;
        var active = _settings.DarkMode ? Color.FromArgb(48, 67, 94) : Color.FromArgb(231, 240, 255);
        foreach (var button in new[] { _menuToggle, _transferNav, _changeBaseNav, _settingsNav, _arqEmpNav, _arqSistNav }) button.BackColor = inactive;
        if (_transferView.Visible) _transferNav.BackColor = active;
        else if (_changeBaseView.Visible) _changeBaseNav.BackColor = active;
        else if (_settingsView.Visible) _settingsNav.BackColor = active;
        else if (_arqEmpView.Visible) _arqEmpNav.BackColor = active;
        else if (_arqSistView.Visible) _arqSistNav.BackColor = active;
    }

    private static IEnumerable<Control> EnumerateControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in EnumerateControls(child)) yield return descendant;
        }
    }

    private void ApplyTitleBarTheme()
    {
        if (!IsHandleCreated) return;
        var dark = _settings.DarkMode;
        uint enabled = dark ? 1u : 0u;
        const int useImmersiveDarkMode = 20;
        const int useImmersiveDarkModeLegacy = 19;
        if (DwmSetWindowAttribute(Handle, useImmersiveDarkMode, ref enabled, sizeof(uint)) != 0)
            _ = DwmSetWindowAttribute(Handle, useImmersiveDarkModeLegacy, ref enabled, sizeof(uint));

        var caption = ToColorRef(dark ? Color.FromArgb(24, 30, 40) : Color.FromArgb(244, 247, 251));
        var foreground = ToColorRef(dark ? Color.FromArgb(232, 237, 245) : Color.FromArgb(29, 42, 61));
        var border = ToColorRef(dark ? Color.FromArgb(24, 30, 40) : Color.FromArgb(220, 228, 238));
        _ = DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(uint)); // DWMWA_CAPTION_COLOR (Windows 11+)
        _ = DwmSetWindowAttribute(Handle, 36, ref foreground, sizeof(uint)); // DWMWA_TEXT_COLOR (Windows 11+)
        _ = DwmSetWindowAttribute(Handle, 34, ref border, sizeof(uint)); // DWMWA_BORDER_COLOR (Windows 11+)
    }

    private static uint ToColorRef(Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint value, int valueSize);

    private string GetSetting(string key) => key switch
    {
        nameof(AppSettings.SolicitationRoot) => _settings.SolicitationRoot,
        nameof(AppSettings.BasesRoot) => _settings.BasesRoot,
        nameof(AppSettings.SupportRoot) => _settings.SupportRoot,
        _ => ""
    };

    private void Browse(string key, string kind)
    {
        if (kind == "folder")
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = _fields[key].Text, ShowNewFolderButton = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) _fields[key].Text = dialog.SelectedPath;
        }
    }

    private void ReadFields()
    {
        _settings.SolicitationRoot = _fields[nameof(AppSettings.SolicitationRoot)].Text.Trim();
        _settings.BasesRoot = _fields[nameof(AppSettings.BasesRoot)].Text.Trim();
        _settings.SupportRoot = _fields[nameof(AppSettings.SupportRoot)].Text.Trim();
        _settings.ArqEmpCommandDatabase = _arqEmpDatabase.Text.Trim();
        _settings.ArqSistCommandDatabase = _arqSistDatabase.Text.Trim();
        _settings.ArqEmpCommands = _arqEmpEditor.Text;
        _settings.ArqSistCommands = _arqSistEditor.Text;
    }

    private void PersistSettings()
    {
        ReadFields();
        _settings.SetFirebirdPassword(_savePassword.Checked ? _password.Text : null);
        _settings.Save();
    }

    private void QueueCommandSettingsSave()
    {
        _commandSaveTimer.Stop();
        _commandSaveTimer.Start();
    }

    private void SaveCommandSettings()
    {
        _settings.ArqEmpCommandDatabase = _arqEmpDatabase.Text.Trim();
        _settings.ArqSistCommandDatabase = _arqSistDatabase.Text.Trim();
        _settings.ArqEmpCommands = _arqEmpEditor.Text;
        _settings.ArqSistCommands = _arqSistEditor.Text;
        try { _settings.Save(); }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível salvar os comandos SQL: {ex.Message}", "Erro ao salvar comandos", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MigrateLegacySqlScripts()
    {
        if (_settings.CommandScriptsMigrated) return;
        try
        {
            var oldArqEmpScript = _settings.ScriptArqEmp;
            var oldArqSistScript = _settings.ScriptArqSist;
            if (string.IsNullOrWhiteSpace(_settings.ArqEmpCommands) && !string.IsNullOrWhiteSpace(oldArqEmpScript) && File.Exists(oldArqEmpScript))
                _settings.ArqEmpCommands = File.ReadAllText(oldArqEmpScript);
            if (string.IsNullOrWhiteSpace(_settings.ArqSistCommands) && !string.IsNullOrWhiteSpace(oldArqSistScript) && File.Exists(oldArqSistScript))
                _settings.ArqSistCommands = File.ReadAllText(oldArqSistScript);
            _settings.ScriptArqEmp = null;
            _settings.ScriptArqSist = null;
            _settings.CommandScriptsMigrated = true;
            _settings.Save();
        }
        catch
        {
            // A migração será tentada novamente na próxima abertura.
        }
    }

    private void SavePasswordPreference()
    {
        if (_savePassword.Checked && string.IsNullOrWhiteSpace(_password.Text))
        {
            MessageBox.Show(this, "Digite a senha Firebird antes de marcar a opção para salvá-la.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _savePassword.Checked = false;
            _password.Focus();
            return;
        }

        try { PersistSettings(); }
        catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar a senha protegida: {ex.Message}", "Erro ao salvar senha", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task StartFlowAsync()
    {
        if (_running) return;
        ReadFields();
        var sl = _sl.Text.Trim();
        if (sl.Length == 0 || sl.Any(c => !char.IsAsciiDigit(c))) { MessageBox.Show(this, "Informe o número da SL usando somente dígitos.", "SL inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_settings.BasesRoot) || string.IsNullOrWhiteSpace(_settings.SupportRoot)) { MessageBox.Show(this, "Configure a pasta local das bases e a pasta de bases de apoio.", "Configuração incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!Directory.Exists(_settings.SolicitationRoot)) { MessageBox.Show(this, "A pasta de origem das solicitações não está acessível.", "Origem indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!Directory.Exists(_settings.BasesRoot) || !Directory.Exists(_settings.SupportRoot)) { MessageBox.Show(this, "A pasta local das bases e a pasta de apoio precisam existir e estar acessíveis.", "Pastas indisponíveis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_password.Text)) { MessageBox.Show(this, "Informe a senha Firebird para continuar.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { PersistSettings(); }
        catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar a configuração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var source = Path.GetFullPath(Path.Combine(_settings.SolicitationRoot, sl));
        var destination = Path.GetFullPath(Path.Combine(_settings.BasesRoot, sl));
        var baseRoot = Path.GetFullPath(_settings.BasesRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, "O destino calculado está fora da pasta local configurada.", "Destino inválido", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        if (!Directory.Exists(source)) { MessageBox.Show(this, $"A pasta de origem não foi encontrada:\n{source}", "SL não encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (Directory.Exists(destination))
        {
            var answer = MessageBox.Show(this, $"A pasta de destino já existe:\n{destination}\n\nPara evitar perda de dados, ela será renomeada para uma cópia de segurança antes da nova cópia. Continuar?", "Confirmar substituição", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        _running = true;
        _versionLabel.Text = "";
        _versionLabel.Visible = false;
        _runCancellation = new CancellationTokenSource();
        SetBusy(true);
        try
        {
            SetStatus("Iniciando transferência…");
            Log($"Iniciando preparação da SL {sl}");
            if (Directory.Exists(destination))
            {
                var backup = destination + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                Directory.Move(destination, backup);
                Log($"Pasta anterior preservada em: {backup}");
            }
            Directory.CreateDirectory(destination);
            SetStatus("Transferindo arquivos…");
            Log("Iniciando cópia nativa do .NET.");
            await CopyDirectoryAsync(source, destination, _runCancellation.Token);
            Log("Cópia da solicitação concluída.");
            SetStatus("Transferência concluída. Preparando a base…");

            await ExtractArchivesAsync(destination, _runCancellation.Token);
            MoveDatabasesToRoot(destination, _runCancellation.Token);
            await EnsureRequiredDatabasesAsync(destination, _runCancellation.Token);
            await RunSqlScriptsAsync(destination, _runCancellation.Token);
            await UpdateRegistryAsync(destination);
            await ShowVersionAsync(destination, _runCancellation.Token);
            Log("Fluxo concluído com sucesso.");
            _status.Text = "Concluído";
            MessageBox.Show(this, $"A base da SL {sl} foi preparada e selecionada.", "Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) { Log("Operação cancelada."); _status.Text = "Cancelado"; }
        catch (Exception ex) { Log("ERRO: " + ex.Message); _status.Text = "Falha na operação"; MessageBox.Show(this, ex.Message, "Falha ao preparar a base", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _runCancellation?.Dispose(); _runCancellation = null; _running = false; SetBusy(false); }
    }

    private async Task ExtractArchivesAsync(string root, CancellationToken token)
    {
        var rars = Directory.GetFiles(root, "*.rar", SearchOption.AllDirectories);
        if (rars.Length > 0)
        {
            var selected = SelectArchives("Selecione os arquivos RAR para extrair", rars);
            token.ThrowIfCancellationRequested();
            if (selected.Length > 0)
            {
                foreach (var rar in selected)
                    await ExtractRarArchiveAsync(rar, root, token);
            }
        }
        var zips = Directory.GetFiles(root, "*.zip", SearchOption.AllDirectories);
        if (zips.Length > 0)
        {
            var selected = SelectArchives("Selecione os arquivos ZIP para extrair", zips);
            token.ThrowIfCancellationRequested();
            foreach (var zip in selected)
            {
                Log("Extraindo ZIP: " + Path.GetFileName(zip));
                SetArchiveProgress(0, "Extraindo ZIP: " + Path.GetFileName(zip));
                using var archive = ZipFile.OpenRead(zip);
                foreach (var entry in archive.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    var target = GetSafeArchiveEntryPath(root, entry.FullName);
                    if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    await using var input = entry.Open();
                    await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await input.CopyToAsync(output, 1024 * 1024, token);
                }
                SetArchiveProgress(100, "Extração ZIP concluída");
            }
        }
    }

    private async Task ExtractRarArchiveAsync(string archivePath, string destinationRoot, CancellationToken token)
    {
        Log("Extraindo RAR com SharpCompress: " + Path.GetFileName(archivePath));
        SetArchiveProgress(0, "Extraindo RAR: " + Path.GetFileName(archivePath));
        await using var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var extractionProgress = new Progress<ProgressReport>(report =>
        {
            var percent = (int)Math.Clamp(Math.Round(report.PercentComplete ?? 0), 0d, 100d);
            var entryName = string.IsNullOrWhiteSpace(report.EntryPath) ? Path.GetFileName(archivePath) : Path.GetFileName(report.EntryPath);
            SetArchiveProgress(percent, $"Extraindo RAR: {entryName} ({percent}%)");
        });
        var readerOptions = ReaderOptions.ForExternalStream.WithProgress(extractionProgress);
        await using var reader = await ReaderFactory.OpenAsyncReader(archiveStream, readerOptions, token);
        while (await reader.MoveToNextEntryAsync(token))
        {
            token.ThrowIfCancellationRequested();
            var entry = reader.Entry;
            var entryKey = entry.Key ?? throw new InvalidOperationException("O arquivo RAR contém uma entrada sem nome.");
            SetStatus("Extraindo RAR: " + Path.GetFileName(entryKey));
            var outputPath = GetSafeArchiveEntryPath(destinationRoot, entryKey);
            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(outputPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await reader.WriteEntryToAsync(output, token);
        }
        SetArchiveProgress(100, "Extração RAR concluída");
    }

    private void SetArchiveProgress(int percent, string status)
    {
        _progressPercent = Math.Clamp(percent, 0, 100);
        UpdateProgressFill();
        SetStatus(status);
    }

    private static string GetSafeArchiveEntryPath(string destinationRoot, string entryKey)
    {
        var normalized = entryKey.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized) || normalized.Split(Path.DirectorySeparatorChar).Any(part => part == ".."))
            throw new InvalidOperationException("O arquivo compactado contém um caminho de saída inválido.");

        var root = Path.GetFullPath(destinationRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var output = Path.GetFullPath(Path.Combine(root, normalized));
        if (!output.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O arquivo compactado contém um caminho fora da pasta de destino.");
        return output;
    }

    private string[] SelectArchives(string title, string[] files)
    {
        if (files.Length == 1) return files;
        using var dialog = new Form { Text = title, Size = new Size(650, 440), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog, BackColor = Color.White };
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, Font = Font };
        foreach (var file in files) list.Items.Add(file, true);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var ok = new Button { Text = "Extrair selecionados", DialogResult = DialogResult.OK, Width = 150 };
        var cancel = new Button { Text = "Pular extração", DialogResult = DialogResult.Cancel, Width = 110 };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        dialog.Controls.Add(list); dialog.Controls.Add(buttons); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return Array.Empty<string>();
        return list.CheckedItems.Cast<string>().ToArray();
    }

    private async Task CopyDirectoryAsync(string source, string destination, CancellationToken token)
    {
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        var directories = Directory.GetDirectories(source, "*", SearchOption.AllDirectories);
        foreach (var directory in directories)
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        long totalBytes = 0;
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            totalBytes += new FileInfo(file).Length;
        }

        long bytesCopied = 0;
        var filesCopied = 0;
        var lastPercent = -1;
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await CopyFileAsync(file, target, token, delta =>
            {
                bytesCopied += delta;
                var percent = totalBytes == 0 ? 100 : (int)Math.Min(100, bytesCopied * 100d / totalBytes);
                if (percent != lastPercent || delta == 0)
                {
                    lastPercent = percent;
                    SetTransferProgress(percent, bytesCopied, totalBytes, filesCopied, files.Length);
                }
            });
            filesCopied++;
            SetTransferProgress(totalBytes == 0 ? 100 : (int)Math.Min(100, bytesCopied * 100d / totalBytes), bytesCopied, totalBytes, filesCopied, files.Length);
            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(file));
            File.SetAttributes(target, File.GetAttributes(file));
        }
        SetTransferProgress(100, totalBytes, totalBytes, files.Length, files.Length);
    }

    private static async Task CopyFileAsync(string source, string destination, CancellationToken token, Action<int>? copied = null)
    {
        const int bufferSize = 1024 * 1024;
        var buffer = new byte[bufferSize];
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
            if (read == 0) break;
            await output.WriteAsync(buffer.AsMemory(0, read), token);
            copied?.Invoke(read);
        }
    }

    private void SetTransferProgress(int percent, long bytesCopied, long totalBytes, int filesCopied, int totalFiles)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => SetTransferProgress(percent, bytesCopied, totalBytes, filesCopied, totalFiles))); return; }
        _progressPercent = Math.Clamp(percent, 0, 100);
        UpdateProgressFill();
        var copiedMb = bytesCopied / 1024d / 1024d;
        var totalMb = totalBytes / 1024d / 1024d;
        _status.Text = $"Transferindo: {percent}%  |  {copiedMb:N1} / {totalMb:N1} MB  |  {filesCopied} / {totalFiles} arquivos";
    }

    private void UpdateProgressFill()
    {
        if (InvokeRequired) { BeginInvoke(new Action(UpdateProgressFill)); return; }
        var availableWidth = Math.Max(0, _progress.ClientSize.Width);
        _progressFill.Width = (int)Math.Round(availableWidth * _progressPercent / 100d);
        _progressFill.Height = Math.Max(0, _progress.ClientSize.Height);
    }

    private void MoveDatabasesToRoot(string root, CancellationToken token)
    {
        foreach (var file in Directory.GetFiles(root, "*.fdb", SearchOption.AllDirectories).Where(f => !string.Equals(Path.GetDirectoryName(f), root, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            token.ThrowIfCancellationRequested();
            var target = Path.Combine(root, Path.GetFileName(file));
            if (File.Exists(target)) throw new IOException($"Já existe {Path.GetFileName(target)} na raiz e também em uma subpasta. Resolva o conflito antes de continuar.");
            File.Move(file, target);
            Log("Base movida para raiz: " + Path.GetFileName(target));
        }
    }

    private async Task EnsureRequiredDatabasesAsync(string root, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var systemDb = Path.Combine(root, "ARQSIST.FDB");
        if (!File.Exists(systemDb))
        {
            using var dialog = new OpenFileDialog { InitialDirectory = root, Title = "Selecione o arquivo que será usado como ARQSIST.FDB", Filter = "Firebird database (*.fdb)|*.fdb|Todos os arquivos (*.*)|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) throw new OperationCanceledException("É necessário selecionar o arquivo ARQSIST.");
            await CopyFileAsync(dialog.FileName, systemDb, token);
            Log("ARQSIST.FDB criado a partir do arquivo selecionado.");
        }
        foreach (var name in new[] { "ARQEMP.FDB", "ARQOCOR.FDB" })
        {
            var target = Path.Combine(root, name);
            var source = Path.Combine(_settings.SupportRoot, name);
            if (!File.Exists(target))
            {
                if (!File.Exists(source)) throw new FileNotFoundException($"Arquivo obrigatório não encontrado na base nem na pasta de apoio: {name}", source);
                await CopyFileAsync(source, target, token);
                Log($"Arquivo de apoio copiado: {name}");
            }
        }
        if (!File.Exists(Path.Combine(root, "ARQEMP.FDB")) || !File.Exists(systemDb)) throw new InvalidOperationException("A validação das bases obrigatórias falhou.");
    }

    private async Task RunSqlScriptsAsync(string root, CancellationToken token)
    {
        await RunSqlIfConfiguredAsync(_arqEmpEditor.Text, Path.Combine(root, "ARQEMP.FDB"), "Comandos ARQEMP", token);
        await RunSqlIfConfiguredAsync(_arqSistEditor.Text, Path.Combine(root, "ARQSIST.FDB"), "Comandos ARQSIST", token);
    }

    private async Task RunSqlIfConfiguredAsync(string commands, string database, string label, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(commands)) { Log($"{label}: ignorados (nenhum comando informado)."); return; }
        token.ThrowIfCancellationRequested();
        SetStatus($"Conectando a {Path.GetFileName(database)}…");
        Log($"Conectando diretamente a {database}");
        var connectionString = BuildFirebirdConnectionString(database);
        await using var connection = new FbConnection(connectionString);
        await connection.OpenAsync(token);

        var parsedScript = new FbScript(commands);
        parsedScript.Parse();
        var batch = new FbBatchExecution(connection);
        batch.AppendSqlStatements(parsedScript);

        SetStatus($"Executando {label} diretamente no banco…");
        Log($"Executando {label} pela conexão ADO.NET.");
        using var cancelRegistration = token.Register(() =>
        {
            try { connection.Close(); } catch { }
        });
        try { await Task.Run(() => batch.Execute(), CancellationToken.None); }
        catch (Exception) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        token.ThrowIfCancellationRequested();
        Log($"{label} concluído.");
    }

    private string BuildFirebirdConnectionString(string databasePath)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = "localhost",
            Database = Path.GetFullPath(databasePath),
            UserID = "SYSDBA",
            Password = _password.Text,
            Dialect = 3,
            Charset = "UTF8",
            Pooling = false,
            ServerType = FbServerType.Default
        };
        return builder.ToString();
    }

    private async Task UpdateRegistryAsync(string root)
    {
        Log("Atualizando HKCU\\SIC\\CONEXAO...");
        using var key = Registry.CurrentUser.CreateSubKey(@"SIC\CONEXAO", true) ?? throw new InvalidOperationException("Não foi possível abrir HKCU\\SIC\\CONEXAO.");
        var basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var name in new[] { "BASE1", "BASE2", "BASE3", "BASE4", "EMPRESA" }) key.SetValue(name, basePath, RegistryValueKind.String);
        key.SetValue("SERVIDOR", "LOCALHOST", RegistryValueKind.String);
        key.SetValue("TEMP", Path.GetTempPath(), RegistryValueKind.String);
        Log("Registro atualizado.");
        await Task.CompletedTask;
    }

    private async Task ShowVersionAsync(string root, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var database = Path.Combine(root, "ARQSIST.FDB");
        SetStatus("Consultando versão de ARQSIST.FDB…");
        Log($"Consultando a versão diretamente em {database}");
        await using var connection = new FbConnection(BuildFirebirdConnectionString(database));
        await connection.OpenAsync(token);
        await using var command = new FbCommand("SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE", connection)
        {
            CommandTimeout = 0
        };
        await using var reader = await command.ExecuteReaderAsync(token);
        var version = await reader.ReadAsync(token) ? Convert.ToString(reader["VERSAO"]) : "(sem registro de versão)";
        _versionLabel.Text = "Versão atual: " + version;
        _versionLabel.Visible = true;
    }

    private void SetBusy(bool busy)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => SetBusy(busy))); return; }
        _transferView.SuspendLayout();
        _transferView.RowStyles[3].Height = busy ? 56 : 0;
        _transferView.ResumeLayout(true);
        _start.Enabled = !busy;
        _sl.Enabled = !busy;
        _settingsNav.Enabled = !busy;
        _transferNav.Enabled = !busy;
        _arqEmpNav.Enabled = !busy;
        _arqSistNav.Enabled = !busy;
        _cancel.Visible = busy;
        _cancel.Enabled = busy;
        _progressFooter.Visible = busy;
        _progress.Visible = busy;
        if (busy)
        {
            _progressPercent = 0;
            UpdateProgressFill();
            SetStatus("Preparando operação…");
        }
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => SetStatus(text))); return; }
        _status.Text = text;
    }

    private void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => Log(message))); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }
}
