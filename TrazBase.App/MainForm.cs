using Microsoft.Win32;
using System.IO.Compression;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using FirebirdSql.Data.Services;
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
    private readonly CheckBox _forceMultiBase = new() { Text = "Sempre criar estrutura multi-base (pastas ARQ01/ARQ00)", AutoSize = true, Anchor = AnchorStyles.Left };
    private NotifyIcon? _notifyIcon;
    private readonly NumericUpDown _baseCount = new() { Minimum = 1, Maximum = 99, Value = 4, Width = 80, Height = 26, BackColor = Color.FromArgb(249, 251, 254), ForeColor = Color.FromArgb(29, 42, 61), TextAlign = HorizontalAlignment.Center, Anchor = AnchorStyles.Left };
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
    private readonly PlainNavigationButton _resetNav = new() { Text = "", Image = SvgIconLoader.Load("Redefinir.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _restoreNav = new() { Text = "", Image = SvgIconLoader.Load("Restaurar.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _settingsNav = new() { Text = "", Image = SvgIconLoader.Load("Configuracao.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _arqEmpNav = new() { Text = "", Image = SvgIconLoader.Load("SQL.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly PlainNavigationButton _arqSistNav = new() { Text = "", Image = SvgIconLoader.Load("SQL.svg"), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Top, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(29, 42, 61), BackColor = Color.White, Font = UiTypography.Body(12) };
    private readonly Panel _menuFooter = new() { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(6, 4, 6, 6) };
    private readonly Label _appVersionLabel = new() { Dock = DockStyle.Top, Height = 30, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(38, 50, 68), ForeColor = Color.FromArgb(190, 208, 230), Font = UiTypography.BodySemibold(9f) };
    private readonly Label _creatorLabel = new() { Dock = DockStyle.Top, Height = 24, AutoEllipsis = true, Text = "Criado por Bruno Passeti", TextAlign = ContentAlignment.MiddleCenter, Font = UiTypography.Body(8.5f) };
    private readonly Panel _updateRow = new() { Dock = DockStyle.Top, Height = 34, Visible = false };
    private readonly PictureBox _updateAnimation = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.StretchImage, Visible = false, BackColor = Color.Transparent };
    private readonly ThemedProgressBar _updateProgress = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Button _updateButton = new() { Dock = DockStyle.Fill, Image = SvgIconLoader.Load("Atualizar.svg", 16), ImageAlign = ContentAlignment.MiddleCenter, TextImageRelation = TextImageRelation.ImageBeforeText, Text = "", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, Font = UiTypography.BodySemibold(9f) };
    private readonly ToolTip _menuToolTip = new();
    private bool _downloadingUpdate;
    private TableLayoutPanel _transferView = null!;
    private Control _settingsView = null!;
    private Control _changeBaseView = null!;
    private Control _resetView = null!;
    private Control _restoreView = null!;
    private Control _arqEmpView = null!;
    private Control _arqSistView = null!;
    private Panel _settingsFieldsHost = null!;
    private readonly ThemedListView _baseFolders = new() { Dock = DockStyle.Fill, View = View.List, BorderStyle = BorderStyle.None, FullRowSelect = true, HideSelection = false, MultiSelect = false };
    private readonly Label _baseFoldersRootLabel = new() { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TextBox _selectedBasePath = new() { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(249, 251, 254), Dock = DockStyle.Fill };
    private readonly Button _changeBaseButton = new() { Text = "Trocar base", Width = 118, Height = 30, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Button _openBaseFolderButton = new() { Text = "Abrir pasta", Width = 118, Height = 30, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly TextBox _resetSl = new() { Width = 180, PlaceholderText = "Ex.: 123456" };
    private readonly Button _resetButton = new() { Text = "Redefinir base", Width = 150, Height = 36, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Button _resetCancelButton = new() { Text = "Cancelar", Width = 100, Height = 36, Visible = false, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Label _resetRootLabel = new() { AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f), Location = new Point(500, 48), Anchor = AnchorStyles.Top | AnchorStyles.Left };
    private readonly ThemedTextBox _resetLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 25, 38), ForeColor = Color.FromArgb(220, 229, 242), BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10f) };
    private CancellationTokenSource? _resetCancellation;
    private bool _resetRunning;
    private readonly TextBox _restoreFolder = new() { Width = 420, Height = 30, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(249, 251, 254), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _restoreBrowseButton = new() { Text = "Procurar", Size = new Size(110, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, Font = UiTypography.BodySemibold(9f) };
    private readonly Button _restoreButton = new() { Text = "Restaurar bases", Width = 170, Height = 36, BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly Button _restoreCancelButton = new() { Text = "Cancelar", Width = 100, Height = 36, Visible = false, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
    private readonly ThemedTextBox _restoreLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 25, 38), ForeColor = Color.FromArgb(220, 229, 242), BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10f) };
    private CancellationTokenSource? _restoreCancellation;
    private bool _restoreRunning;
    private readonly TextBox _arqEmpDatabase = new();
    private readonly TextBox _arqSistDatabase = new();
    private readonly RichTextBox _arqEmpEditor = CreateSqlEditor();
    private readonly RichTextBox _arqSistEditor = CreateSqlEditor();
    private readonly RichTextBox _arqEmpOutput = CreateSqlOutput();
    private readonly RichTextBox _arqSistOutput = CreateSqlOutput();
    private readonly System.Windows.Forms.Timer _commandSaveTimer = new() { Interval = 700 };
    private readonly System.Windows.Forms.Timer _settingsSaveTimer = new() { Interval = 700 };
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
        _updateRow.Controls.Add(_updateButton);
        _updateRow.Controls.Add(_updateProgress);
        _updateRow.Controls.Add(_updateAnimation);
        _menuFooter.Controls.Add(_updateRow);
        _menuFooter.Controls.Add(_creatorLabel);
        _menuFooter.Controls.Add(_appVersionLabel);
        _sidebar.Controls.Add(_menuFooter);
        _sidebar.Controls.Add(_settingsNav);
        _sidebar.Controls.Add(_arqSistNav);
        _sidebar.Controls.Add(_arqEmpNav);
        _sidebar.Controls.Add(_restoreNav);
        _sidebar.Controls.Add(_resetNav);
        _sidebar.Controls.Add(_changeBaseNav);              
        _sidebar.Controls.Add(_transferNav);
        _sidebar.Controls.Add(_menuToggle);
        _transferView = BuildTransferView();
        _settingsView = BuildSettingsView();
        _changeBaseView = BuildChangeBaseView();
        _resetView = BuildResetView();
        _restoreView = BuildRestoreView();
        _arqEmpView = BuildCommandView("Comandos ARQEMP", _arqEmpDatabase, _arqEmpEditor, _arqEmpOutput, "ARQEMP");
        _arqSistView = BuildCommandView("Comandos ARQSIST", _arqSistDatabase, _arqSistEditor, _arqSistOutput, "ARQSIST");
        // A base selecionada (.FDB) NÃO é persistida: fica apenas na sessão atual.
        _arqEmpEditor.Text = _settings.ArqEmpCommands;
        _arqSistEditor.Text = _settings.ArqSistCommands;
        HighlightSql(_arqEmpEditor);
        HighlightSql(_arqSistEditor);
        _contentHost.Controls.Add(_settingsView);
        _contentHost.Controls.Add(_changeBaseView);
        _contentHost.Controls.Add(_resetView);
        _contentHost.Controls.Add(_restoreView);
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
        _forceMultiBase.Checked = _settings.ForceMultiBase;
        _forceMultiBase.CheckedChanged += (_, _) =>
        {
            _settings.ForceMultiBase = _forceMultiBase.Checked;
            try { PersistSettings(); }
            catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar a preferência de estrutura: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        AppMessageBox.DialogShown += (caption, body) => NotifyIfMinimized(caption, body);
        _baseCount.Value = Math.Clamp(_settings.BaseCount, 1, 99);
        _baseCount.ValueChanged += (_, _) =>
        {
            _settings.BaseCount = Math.Clamp((int)_baseCount.Value, 1, 99);
            try { PersistSettings(); }
            catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar a quantidade de bases: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        _appVersionLabel.Text = GetCurrentVersion().ToString(3);
        Shown += async (_, _) =>
        {
            _sl.Focus(); // ao abrir o sistema, o cursor já fica no campo da SL
            await CheckForUpdatesAsync();
        };
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
        _menuAnimationTimer.Tick += OnMenuAnimationTick;
        _updateButton.Click += async (_, _) => await InstallUpdateAsync();
        _menuToolTip.SetToolTip(_updateButton, "Baixar e instalar a atualização disponível");
        _transferNav.Click += (_, _) => ShowView(settings: false);
        _settingsNav.Click += (_, _) => ShowView(settings: true);
        _changeBaseNav.Click += (_, _) => ShowChangeBaseView();
        _resetNav.Click += (_, _) => ShowResetView();
        _restoreNav.Click += (_, _) => ShowRestoreView();
        _restoreButton.Click += async (_, _) => await RestoreBaseAsync();
        _restoreCancelButton.Click += (_, _) => _restoreCancellation?.Cancel();
        _resetButton.Click += async (_, _) => await ResetBaseAsync();
        _resetCancelButton.Click += (_, _) => _resetCancellation?.Cancel();
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
        _settingsSaveTimer.Tick += (_, _) =>
        {
            _settingsSaveTimer.Stop();
            try { PersistSettings(); }
            catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar as configurações: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        FormClosing += (sender, e) =>
        {
            _menuAnimationTimer.Stop();
            _menuAnimationClock.Stop();
            if (_running || _resetRunning || _restoreRunning)
            {
                e.Cancel = true;
                if (_running) _runCancellation?.Cancel();
                if (_resetRunning) _resetCancellation?.Cancel();
                if (_restoreRunning) _restoreCancellation?.Cancel();
                MessageBox.Show(this, "A operação foi cancelada. Aguarde o encerramento da etapa atual antes de fechar.", "Operação em andamento", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _commandSaveTimer.Stop();
                _settingsSaveTimer.Stop();
                _notifyIcon?.Visible = false;
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

    private static Version GetCurrentVersion() => typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0);

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var update = await GitHubReleaseUpdater.CheckAsync(GetCurrentVersion());
            if (update is null || IsDisposed) return;
            _updateButton.Text = $"Atualização disponível · v{update.Version}";
            _updateButton.Tag = update;
            _updateRow.Visible = true;
            _menuToolTip.SetToolTip(_updateButton, $"Atualização disponível: versão {update.Version}. Clique para instalar.");
            SetMenuExpanded(_menuExpanded);
        }
        catch
        {
            // A indisponibilidade do GitHub não interfere no uso normal da aplicação.
        }
    }

    private async Task InstallUpdateAsync()
    {
        if (_updateButton.Tag is not ReleaseUpdate update) return;
        if (_running)
        {
            MessageBox.Show(this, "Aguarde a operação atual terminar antes de atualizar a aplicação.", "Operação em andamento", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var answer = MessageBox.Show(this,
            $"A versão {update.Version} está disponível. Deseja baixar e iniciar a atualização agora? A aplicação será fechada para instalar a nova versão.",
            "Atualização do Traz Base", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (answer != DialogResult.Yes) return;

        _downloadingUpdate = true;
        _updateProgress.SetValue(0);
        _updateProgress.SetMarquee(true);
        _menuToolTip.SetToolTip(_updateButton, "Baixando a atualização…");
        UpdateDownloadButtonText();
        try
        {
            var progress = new Progress<int>(value =>
            {
                if (IsDisposed) return;
                _updateProgress.SetMarquee(false);
                _updateProgress.SetValue(value);
            });
            var installer = await GitHubReleaseUpdater.DownloadInstallerAsync(update.InstallerUri, progress);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installer)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CLOSEAPPLICATIONS"
            });
            Close();
        }
        catch (Exception ex)
        {
            _downloadingUpdate = false;
            _updateButton.Enabled = true;
            UpdateDownloadButtonText();
            SetUpdateAnimation(null);
            _updateAnimation.Visible = false;
            _updateButton.Visible = true;
            _menuToolTip.SetToolTip(_updateButton, $"Atualização disponível: versão {update.Version}. Clique para instalar.");
            MessageBox.Show(this, $"Não foi possível iniciar a atualização. A notificação continuará disponível.\n\n{ex.Message}", "Falha na atualização", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateDownloadButtonText()
    {
        if (_updateButton.Tag is not ReleaseUpdate update) return;
        _updateRow.Visible = true;
        if (_downloadingUpdate)
        {
            _updateButton.Image?.Dispose();
            _updateButton.Image = null;
        }
        else if (_updateButton.Image is null)
        {
            _updateButton.Image = SvgIconLoader.Load("Atualizar.svg", 16);
        }
        _updateAnimation.Visible = _downloadingUpdate && !_menuExpanded;
        _updateProgress.Visible = _downloadingUpdate && _menuExpanded;
        _updateButton.Visible = !_downloadingUpdate;
        if (_downloadingUpdate && !_menuExpanded)
        {
            SetUpdateAnimation("Baixando.gif");
            _updateAnimation.BringToFront();
        }
        _updateButton.Text = _downloadingUpdate ? "" : (_menuExpanded ? $"Atualização disponível · v{update.Version}" : "");
    }

    private void SetUpdateAnimation(string? fileName)
    {
        var previous = _updateAnimation.Image;
        _updateAnimation.Image = fileName is null
            ? null
            : Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Imagens", fileName));
        previous?.Dispose();
    }

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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 352));
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

    private Control BuildResetView()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(26, 20, 26, 20) };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(BuildHeader("Redefinir base", "Valide e prepare uma base local existente para a SL informada"), 0, 0);

        page.Controls.Add(BuildHelpCard(page, 1,
            "• Usa a pasta local das bases escolhida no menu de configurações.\r\n" +
            "• Valida a existência da ARQEMP e ARQOCOR na pasta; caso não existam, copia da pasta de apoio.\r\n" +
            "• Executa os scripts na ARQEMP e ARQSIST.\r\n" +
            "• Atualiza o Registro.\r\n" +
            "• A redefinição não copia nem substitui a pasta inteira da base."), 0, 1);

        var action = Card();
        action.Controls.Add(new Label { Text = "Número da solicitação (SL)", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f), Location = new Point(18, 12) });
        _resetSl.Location = new Point(18, 42);
        _resetSl.Width = 190;
        _resetSl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _resetButton.Location = new Point(224, 38);
        _resetButton.Size = new Size(190, 36);
        _resetButton.FlatAppearance.BorderSize = 0;
        _resetCancelButton.Location = new Point(424, 38);
        _resetCancelButton.FlatAppearance.BorderColor = Color.FromArgb(208, 217, 230);
        _resetCancelButton.FlatAppearance.BorderSize = 1;
        action.Controls.Add(_resetSl);
        action.Controls.Add(_resetButton);
        action.Controls.Add(_resetCancelButton);
        _resetRootLabel.Location = new Point(434, 48);
        _resetRootLabel.Text = "Pasta local: " + (string.IsNullOrWhiteSpace(_settings.BasesRoot) ? "(não configurada)" : _settings.BasesRoot);
        action.Controls.Add(_resetRootLabel);
        action.Resize += (_, _) => _resetRootLabel.MaximumSize = new Size(Math.Max(100, action.ClientSize.Width - _resetRootLabel.Left - 18), 0);
        page.Controls.Add(action, 0, 2);

        var logCard = Card();
        var heading = new Panel { Dock = DockStyle.Top, Height = 42 };
        heading.Controls.Add(new Label { Text = "Andamento da redefinição", Font = UiTypography.BodySemibold(11), ForeColor = Ink, AutoSize = true, Location = new Point(18, 12) });
        var clear = new Button { Text = "Limpar", Size = new Size(108, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(0, 5), BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => _resetLog.Clear();
        heading.Resize += (_, _) => clear.Location = new Point(Math.Max(18, heading.ClientSize.Width - clear.Width - 18), 5);
        heading.Controls.Add(clear);
        logCard.Controls.Add(_resetLog);
        logCard.Controls.Add(heading);
        page.Controls.Add(logCard, 0, 3);
        return page;
    }

    private Control BuildRestoreView()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(26, 20, 26, 20) };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(BuildHeader("Restaurar base", "Restaure os .FBK da pasta selecionada (ou da subpasta BKP, se existir) para .FDB ao lado deles"), 0, 0);

        page.Controls.Add(BuildHelpCard(page, 1,
            "• Selecione a pasta; os arquivos .FBK são procurados na subpasta BKP — se ela não existir, na própria pasta selecionada.\r\n" +
            "• Arquivos com ARQEMP, ARQOCOR ou ARQSIST no nome viram ARQEMP.FDB, ARQOCOR.FDB ou ARQSIST.FDB na mesma pasta do .FBK.\r\n" +
            "• Arquivos .FBK sem nome conhecido pedem a escolha do destino; \"Pular\" ignora o arquivo.\r\n" +
            "• As três restaurações rodam em paralelo e um .FDB existente é substituído somente após confirmação.\r\n" +
            "• Ao final, você escolhe se deseja apontar o Registro para a pasta restaurada."), 0, 1);

        var action = Card();
        action.Controls.Add(new Label { Text = "Pasta da base", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f), Location = new Point(18, 12) });
        _restoreFolder.Location = new Point(18, 38);
        action.Resize += (_, _) => _restoreFolder.Width = Math.Max(120, action.ClientSize.Width - 18 - _restoreBrowseButton.Width - 24);
        action.Controls.Add(_restoreFolder);
        _restoreBrowseButton.Location = new Point(0, 37);
        _restoreBrowseButton.FlatAppearance.BorderSize = 0;
        _restoreBrowseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 125, 245);
        _restoreBrowseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 94, 205);
        action.Resize += (_, _) => _restoreBrowseButton.Left = action.ClientSize.Width - _restoreBrowseButton.Width - 18;
        _restoreBrowseButton.Click += (_, _) => BrowseRestoreFolder();
        action.Controls.Add(_restoreBrowseButton);
        _restoreButton.Location = new Point(18, 74);
        _restoreButton.FlatAppearance.BorderSize = 0;
        action.Controls.Add(_restoreButton);
        _restoreCancelButton.Location = new Point(204, 74);
        _restoreCancelButton.FlatAppearance.BorderColor = Color.FromArgb(208, 217, 230);
        _restoreCancelButton.FlatAppearance.BorderSize = 1;
        action.Controls.Add(_restoreCancelButton);
        _restoreFolder.Width = 600;
        page.Controls.Add(action, 0, 2);

        var logCard = Card();
        var heading = new Panel { Dock = DockStyle.Top, Height = 42 };
        heading.Controls.Add(new Label { Text = "Andamento da restauração", Font = UiTypography.BodySemibold(11), ForeColor = Ink, AutoSize = true, Location = new Point(18, 12) });
        var clear = new Button { Text = "Limpar", Size = new Size(108, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(0, 5), BackColor = Color.FromArgb(31, 111, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = UiTypography.BodySemibold() };
        clear.FlatAppearance.BorderSize = 0;
        clear.Click += (_, _) => _restoreLog.Clear();
        heading.Resize += (_, _) => clear.Location = new Point(Math.Max(18, heading.ClientSize.Width - clear.Width - 18), 5);
        heading.Controls.Add(clear);
        logCard.Controls.Add(_restoreLog);
        logCard.Controls.Add(heading);
        page.Controls.Add(logCard, 0, 3);
        return page;
    }

    private void BrowseRestoreFolder()
    {
        var seed = _restoreFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(seed)) seed = _settings.BasesRoot;
        using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta da base (com subpasta BKP) ou a pasta com os .FBK", ShowNewFolderButton = false, SelectedPath = seed };
        if (dialog.ShowDialog(this) == DialogResult.OK) _restoreFolder.Text = dialog.SelectedPath;
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

    private void SelectBaseMatchingRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SIC\CONEXAO", writable: false);
            var baseValue = key?.GetValue("BASE1") as string;
            if (string.IsNullOrWhiteSpace(baseValue)) return;
            var normalized = Path.GetFullPath(baseValue.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            for (var i = 0; i < _baseFolders.Items.Count; i++)
            {
                var folder = _baseFolders.Items[i].Tag as string;
                if (folder is not null && string.Equals(Path.GetFullPath(folder), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    _baseFolders.Items[i].Selected = true;
                    _baseFolders.EnsureVisible(i);
                    _selectedBasePath.Text = folder;
                    break;
                }
            }
        }
        catch { /* destaque best-effort; a lista funciona sem ele */ }
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

    private async Task ResetBaseAsync()
    {
        if (_resetRunning) return;
        ReadFields();
        var sl = _resetSl.Text.Trim();
        if (sl.Length == 0 || sl.Any(character => !char.IsAsciiDigit(character)))
        {
            MessageBox.Show(this, "Informe o número da SL usando somente dígitos.", "SL inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_settings.BasesRoot) || string.IsNullOrWhiteSpace(_settings.SupportRoot))
        {
            MessageBox.Show(this, "Configure a pasta local das bases e a pasta de bases de apoio.", "Configuração incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!Directory.Exists(_settings.BasesRoot) || !Directory.Exists(_settings.SupportRoot))
        {
            MessageBox.Show(this, "A pasta local das bases e a pasta de apoio precisam existir e estar acessíveis.", "Pastas indisponíveis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_password.Text))
        {
            MessageBox.Show(this, "Informe a senha Firebird na tela Configuração antes de redefinir a base.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string target;
        try { target = BaseOperationRules.ResolveLocalBaseDirectory(_settings.BasesRoot, sl); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Destino inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!Directory.Exists(target))
        {
            MessageBox.Show(this, $"A pasta local da SL não foi encontrada:\n{target}", "Base não encontrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!File.Exists(Path.Combine(target, "ARQSIST.FDB")))
        {
            MessageBox.Show(this, "A base ARQSIST.FDB não foi encontrada na pasta local da SL. A redefinição exige essa base para executar os scripts.", "ARQSIST não encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _resetRunning = true;
        _resetCancellation = new CancellationTokenSource();
        _resetSl.Enabled = false;
        _resetButton.Enabled = false;
        _resetCancelButton.Visible = true;
        foreach (var navigation in new[] { _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav }) navigation.Enabled = false;
        _resetLog.Clear();
        var token = _resetCancellation.Token;
        try
        {
            AppendResetLog($"Iniciando redefinição da SL {sl} em {target}");
            foreach (var supportFile in BaseOperationRules.GetSupportFilesToCopy(target, _settings.SupportRoot))
            {
                token.ThrowIfCancellationRequested();
                await CopyFileAsync(supportFile.Source, supportFile.Destination, token);
                AppendResetLog($"Arquivo copiado da pasta de apoio: {supportFile.Name}");
            }
            foreach (var databaseName in new[] { "ARQEMP.FDB", "ARQOCOR.FDB" })
                if (File.Exists(Path.Combine(target, databaseName))) AppendResetLog($"Arquivo validado: {databaseName}");

            token.ThrowIfCancellationRequested();
            await RunSqlIfConfiguredAsync(_arqEmpEditor.Text, Path.Combine(target, "ARQEMP.FDB"), "Comandos ARQEMP", token, AppendResetLog);
            await RunSqlIfConfiguredAsync(_arqSistEditor.Text, Path.Combine(target, "ARQSIST.FDB"), "Comandos ARQSIST", token, AppendResetLog);
            token.ThrowIfCancellationRequested();
            await UpdateRegistryAsync(target, AppendResetLog);
            AppendResetLog("Redefinição concluída com sucesso.");
            MessageBox.Show(this, $"A base da SL {sl} foi validada e selecionada no Registro.", "Redefinição concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            AppendResetLog("Redefinição cancelada.");
        }
        catch (Exception ex)
        {
            AppendResetLog("ERRO: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Falha ao redefinir a base", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _resetCancellation.Dispose();
            _resetCancellation = null;
            _resetRunning = false;
            _resetSl.Enabled = true;
            _resetButton.Enabled = true;
            _resetCancelButton.Visible = false;
            foreach (var navigation in new[] { _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav }) navigation.Enabled = true;
        }
    }

    private void AppendResetLog(string message)
    {
        _resetLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _resetLog.SelectionStart = _resetLog.TextLength;
        _resetLog.ScrollToCaret();
    }

    private async Task RestoreBaseAsync()
    {
        if (_restoreRunning) return;
        ReadFields();
        var folder = _restoreFolder.Text.Trim();
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "Selecione uma pasta de base existente.", "Pasta não selecionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var backupDirectory = RestoreOperationRules.ResolveBackupDirectory(folder);
        if (!Directory.Exists(backupDirectory))
        {
            MessageBox.Show(this, $"A pasta não foi encontrada:\n{folder}", "Pasta indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fbkFiles = Directory.GetFiles(backupDirectory, "*.fbk");
        if (fbkFiles.Length == 0)
        {
            MessageBox.Show(this, $"Nenhum arquivo .FBK foi encontrado em:\n{backupDirectory}", "Nada para restaurar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(_password.Text))
        {
            MessageBox.Show(this, "Informe a senha Firebird na tela Configuração antes de restaurar.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var plan = new List<RestorePlanItem>(RestoreOperationRules.BuildPlan(fbkFiles));
        var skipped = new List<string>();
        for (var i = 0; i < plan.Count; i++)
        {
            if (plan[i].Database is not null) continue;
            var fileName = Path.GetFileName(plan[i].FbkPath);
            var choice = AppMessageBox.ShowRestoreDestinationChoice(this, fileName);
            if (choice is null) { skipped.Add(fileName); plan.RemoveAt(i); i--; continue; }
            plan[i] = new RestorePlanItem(plan[i].FbkPath, choice, RestoreOperationRules.GetDestinationPath(plan[i].FbkPath, choice));
        }

        if (plan.Count == 0)
        {
            MessageBox.Show(this, "Nenhum arquivo .FBK foi selecionado para restauração.", "Nada para restaurar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var duplicates = RestoreOperationRules.GetDuplicateDestinations(plan);
        if (duplicates.Count > 0)
        {
            MessageBox.Show(this,
                "Mais de um .FBK será restaurado para o mesmo destino:\n\n" + string.Join("\n", duplicates) + "\n\nRenomeie os arquivos ou ajuste a escolha e tente novamente.",
                "Destino duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = plan.Select(item => item.DestinationPath).Where(File.Exists).ToList();
        if (existing.Count > 0)
        {
            var answer = MessageBox.Show(this,
                "Os arquivos .FDB já existem e serão substituídos:\n\n" + string.Join("\n", existing) + "\n\nDeseja continuar?",
                "Confirmar substituição", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
        }

        _restoreRunning = true;
        _restoreCancellation = new CancellationTokenSource();
        _restoreFolder.Enabled = false;
        _restoreBrowseButton.Enabled = false;
        _restoreButton.Enabled = false;
        _restoreCancelButton.Visible = true;
        foreach (var navigation in new[] { _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav }) navigation.Enabled = false;
        _restoreLog.Clear();
        var token = _restoreCancellation.Token;
        try
        {
            AppendRestoreLog($"Iniciando restauração de {plan.Count} base(s) a partir de {backupDirectory}");
            if (string.Equals(backupDirectory, folder, StringComparison.OrdinalIgnoreCase))
                AppendRestoreLog("Subpasta BKP não encontrada; usando a pasta selecionada.");
            if (skipped.Count > 0) AppendRestoreLog("Ignorados: " + string.Join(", ", skipped));

            var tasks = plan.Select(async item =>
            {
                AppendRestoreLog($"Restaurando {Path.GetFileName(item.FbkPath)} → {item.DestinationPath}");
                try
                {
                    await RestoreDatabaseAsync(item, token, AppendRestoreLog);
                    AppendRestoreLog($"{item.Database}: restauração concluída.");
                    return (Item: item, Error: (string?)null);
                }
                catch (Exception ex) when (ex is OperationCanceledException)
                {
                    AppendRestoreLog($"{item.Database}: cancelada.");
                    return (Item: item, Error: "cancelada");
                }
                catch (Exception ex)
                {
                    AppendRestoreLog($"{item.Database}: ERRO — {ex.Message}");
                    return (Item: item, Error: ex.Message);
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            var failures = results.Where(result => result.Error is not null).ToList();
            var cancelled = token.IsCancellationRequested;

            if (cancelled)
                AppendRestoreLog("Restauração cancelada.");
            else if (failures.Count == 0)
                AppendRestoreLog("Restauração concluída com sucesso.");
            else
                AppendRestoreLog($"Restauração concluída com {failures.Count} falha(s): " + string.Join(", ", failures.Select(failure => failure.Item.Database)));

            if (results.Any(result => result.Error is null) && !cancelled)
            {
                var restoredFolder = Path.GetDirectoryName(plan[0].DestinationPath)!;
                var answer = MessageBox.Show(this,
                    $"Restauração concluída.\n\nDeseja atualizar o Registro para apontar para a pasta:\n{restoredFolder}?",
                    "Apontar Registro", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer == DialogResult.Yes) await UpdateRegistryAsync(restoredFolder, AppendRestoreLog);
            }

            if (cancelled)
            {
                // O cancelamento já foi registrado no log; nenhuma ação adicional.
            }
            else if (failures.Count == 0)
            {
                MessageBox.Show(this, $"{results.Length} base(s) restaurada(s) com sucesso.", "Restauração concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "A restauração terminou com falhas. Veja o log para detalhes.", "Falha na restauração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            AppendRestoreLog("ERRO: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Falha na restauração", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _restoreCancellation?.Dispose();
            _restoreCancellation = null;
            _restoreRunning = false;
            _restoreFolder.Enabled = true;
            _restoreBrowseButton.Enabled = true;
            _restoreButton.Enabled = true;
            _restoreCancelButton.Visible = false;
            foreach (var navigation in new[] { _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav }) navigation.Enabled = true;
        }
    }

    private async Task RestoreDatabaseAsync(RestorePlanItem item, CancellationToken token, Action<string> log)
    {
        var connectionString = BuildFirebirdServiceConnectionString(item.DestinationPath);
        var restore = new FbRestore(connectionString)
        {
            Options = File.Exists(item.DestinationPath) ? FbRestoreFlags.Replace : FbRestoreFlags.Create,
            Verbose = true
        };
        restore.ServiceOutput += (_, args) => log(args.Message);
        restore.BackupFiles.Add(new FbBackupFile(Path.GetFullPath(item.FbkPath), null));
        await restore.ExecuteAsync(token);
    }

    private string BuildFirebirdServiceConnectionString(string databasePath)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = "localhost",
            Database = Path.GetFullPath(databasePath),
            UserID = "SYSDBA",
            Password = _password.Text,
            Charset = "UTF8",
            Pooling = false,
            ServerType = FbServerType.Default
        };
        return builder.ToString();
    }

    private void AppendRestoreLog(string message)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => AppendRestoreLog(message))); return; }
        _restoreLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _restoreLog.SelectionStart = _restoreLog.TextLength;
        _restoreLog.ScrollToCaret();
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
        _resetView.Visible = false;
        _restoreView.Visible = false;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.FromArgb(231, 240, 255);
        _resetNav.BackColor = Color.White;
        _restoreNav.BackColor = Color.White;
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = _changeBaseButton;
        RefreshBaseFolders();
        ApplyNavigationColors();
        SelectBaseMatchingRegistry();
        _baseFolders.Focus();
    }

    private void ShowResetView()
    {
        _transferView.Visible = false;
        _settingsView.Visible = false;
        _changeBaseView.Visible = false;
        _resetView.Visible = true;
        _restoreView.Visible = false;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _resetRootLabel.Text = "Pasta local: " + (string.IsNullOrWhiteSpace(_settings.BasesRoot) ? "(não configurada)" : _settings.BasesRoot);
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.White;
        _resetNav.BackColor = Color.FromArgb(231, 240, 255);
        _restoreNav.BackColor = Color.White;
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = _resetButton;
        ApplyNavigationColors();
        _resetSl.Focus();
    }

    private void ShowRestoreView()
    {
        _transferView.Visible = false;
        _settingsView.Visible = false;
        _changeBaseView.Visible = false;
        _resetView.Visible = false;
        _restoreView.Visible = true;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.White;
        _resetNav.BackColor = Color.White;
        _restoreNav.BackColor = Color.FromArgb(231, 240, 255);
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = _restoreRunning ? null : _restoreButton;
        ApplyNavigationColors();
        if (!_restoreRunning) _restoreFolder.Focus();
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
            $"O SQL será executado na base {databaseName}:\n{database}\n\nComandos de alteração podem modificar os dados. Deseja continuar?",
            "Confirmar execução SQL", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        testButton.Enabled = false;
        output.Clear();
        AppendCommandOutput(output, $"Conectando em {Path.GetFileName(database)}…");
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
            AppendCommandOutput(output, "Comandos executados com sucesso.");
        }
        catch (Exception ex)
        {
            AppendCommandOutput(output, "Não foi possível executar o SQL:");
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

    private const int MenuCollapsedWidth = 68;
    private const int MenuExpandedWidth = 300;
    private const double MenuAnimationDurationMs = 220;
    private readonly System.Windows.Forms.Timer _menuAnimationTimer = new() { Interval = 15 };
    private readonly System.Diagnostics.Stopwatch _menuAnimationClock = new();
    private int _menuAnimationFromWidth = MenuCollapsedWidth;
    private int _menuAnimationTargetWidth = MenuCollapsedWidth;
    private bool _menuAnimationTargetExpanded;

    private void SetMenuExpanded(bool expanded, bool animate = true)
    {
        var changed = expanded != _menuExpanded;
        _menuExpanded = expanded;
        if (animate && changed && IsHandleCreated)
        {
            if (!expanded) ApplyMenuExpansionText(expanded); // rótulos somem imediatamente ao recolher
            StartMenuAnimation(expanded);
            return;
        }
        ApplyMenuExpansionState(expanded);
    }

    private void StartMenuAnimation(bool expanded)
    {
        _menuAnimationTimer.Stop();
        _menuAnimationFromWidth = Math.Clamp((int)_shell.ColumnStyles[0].Width, MenuCollapsedWidth, MenuExpandedWidth);
        _menuAnimationTargetWidth = expanded ? MenuExpandedWidth : MenuCollapsedWidth;
        _menuAnimationTargetExpanded = expanded;
        _menuAnimationClock.Restart();
        _menuAnimationTimer.Start();
    }

    private void OnMenuAnimationTick(object? sender, EventArgs e)
    {
        var progress = Math.Clamp(_menuAnimationClock.Elapsed.TotalMilliseconds / MenuAnimationDurationMs, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3); // ease-out cúbico
        _shell.ColumnStyles[0].Width = (int)Math.Round(_menuAnimationFromWidth + (_menuAnimationTargetWidth - _menuAnimationFromWidth) * eased);
        if (progress >= 1)
        {
            _menuAnimationTimer.Stop();
            _shell.ColumnStyles[0].Width = _menuAnimationTargetWidth;
            ApplyMenuExpansionState(_menuAnimationTargetExpanded, applyWidth: false);
        }
    }

    private void ApplyMenuExpansionState(bool expanded, bool applyWidth = true)
    {
        if (applyWidth) _shell.ColumnStyles[0].Width = expanded ? MenuExpandedWidth : MenuCollapsedWidth;
        _menuFooter.Visible = true;
        _menuFooter.Padding = expanded ? new Padding(12, 6, 12, 8) : new Padding(6, 4, 6, 6);
        _menuFooter.Height = expanded
            ? (_updateButton.Tag is ReleaseUpdate ? 104 : 70)
            : (_updateButton.Tag is ReleaseUpdate ? 78 : 48);
        _appVersionLabel.Visible = true;
        _appVersionLabel.Text = expanded ? $"Versão {GetCurrentVersion().ToString(3)}" : GetCurrentVersion().ToString(3);
        _creatorLabel.Visible = expanded;
        _updateRow.Visible = _updateButton.Tag is ReleaseUpdate;
        if (_updateButton.Tag is ReleaseUpdate update)
        {
            _updateRow.Visible = true;
            _updateRow.Height = 34;
            _updateButton.Font = UiTypography.BodySemibold(14f);
            UpdateDownloadButtonText();
        }
        ApplyMenuExpansionText(expanded);
    }

    private void ApplyMenuExpansionText(bool expanded)
    {
        _menuToggle.Text = expanded ? "Menu" : "";
        _transferNav.Text = expanded ? "Transferir" : "";
        _changeBaseNav.Text = expanded ? "Alterar base" : "";
        _resetNav.Text = expanded ? "Redefinir base" : "";
        _restoreNav.Text = expanded ? "Restaurar base" : "";
        _settingsNav.Text = expanded ? "Configuração" : "";
        _arqEmpNav.Text = expanded ? "Comandos ARQEMP" : "";
        _arqSistNav.Text = expanded ? "Comandos ARQSIST" : "";
        _menuToggle.ImageAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
        _menuToggle.TextAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
        _menuToggle.Padding = expanded ? new Padding(18, 0, 4, 0) : Padding.Empty;
        _menuToggle.FlatAppearance.BorderSize = 0;
        _menuToggle.Font = UiTypography.Body(12f);
        foreach (var button in new[] { _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav })
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
        _resetView.Visible = false;
        _restoreView.Visible = false;
        _arqEmpView.Visible = false;
        _arqSistView.Visible = false;
        _transferNav.BackColor = settings ? Color.White : Color.FromArgb(231, 240, 255);
        _changeBaseNav.BackColor = Color.White;
        _resetNav.BackColor = Color.White;
        _settingsNav.BackColor = settings ? Color.FromArgb(231, 240, 255) : Color.White;
        _restoreNav.BackColor = Color.White;
        _arqEmpNav.BackColor = Color.White;
        _arqSistNav.BackColor = Color.White;
        AcceptButton = settings ? null : _start;
        ApplyNavigationColors();
        if (settings) _fields[nameof(AppSettings.SolicitationRoot)].Focus();
        else _sl.Focus();
    }

    private void ShowCommandView(bool arqEmp)
    {
        _transferView.Visible = false;
        _settingsView.Visible = false;
        _changeBaseView.Visible = false;
        _resetView.Visible = false;
        _restoreView.Visible = false;
        _arqEmpView.Visible = arqEmp;
        _arqSistView.Visible = !arqEmp;
        _transferNav.BackColor = Color.White;
        _changeBaseNav.BackColor = Color.White;
        _resetNav.BackColor = Color.White;
        _restoreNav.BackColor = Color.White;
        _settingsNav.BackColor = Color.White;
        _arqEmpNav.BackColor = arqEmp ? Color.FromArgb(231, 240, 255) : Color.White;
        _arqSistNav.BackColor = arqEmp ? Color.White : Color.FromArgb(231, 240, 255);
        AcceptButton = null;
        ApplyNavigationColors();
        (arqEmp ? _arqEmpEditor : _arqSistEditor).Focus();
    }

    private Control BuildSettingsPanel()
    {
        var card = Card();
        var title = new Label { Text = "Configuração", Font = UiTypography.BodySemibold(12), ForeColor = Ink, AutoSize = true, Location = new Point(18, 13) };
        card.Controls.Add(title);
        var host = _settingsFieldsHost = new Panel
        {
            Location = new Point(18, 45),
            Size = new Size(Math.Max(100, card.ClientSize.Width - 54), 288),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = Padding.Empty
        };
        var labels = new[]
        {
            new Label { Text = "Origem das solicitações", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Pasta local das bases", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Pasta de bases de apoio", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Pasta da base temporária", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) },
            new Label { Text = "Senha Firebird", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f) }
        };
        var keys = new[] { nameof(AppSettings.SolicitationRoot), nameof(AppSettings.BasesRoot), nameof(AppSettings.SupportRoot), nameof(AppSettings.TempPath) };
        var emptyTempHint = TryGetSicTempPath() is string registryTemp
            ? $"(Vazio = valor do Registro: {registryTemp})"
            : "(Vazio = pasta temporária do Windows)";
        var pathFields = keys.Select(key => new TextBox
        {
            Text = GetSetting(key),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(249, 251, 254),
            PlaceholderText = key == nameof(AppSettings.TempPath) ? emptyTempHint : "",
            Height = 30
        }).ToArray();
        var browseButtons = keys.Select(key =>
        {
            var button = new Button
            {
                Text = "Procurar",
                Size = new Size(120, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(31, 111, 235),
                ForeColor = Color.White,
                Font = UiTypography.BodySemibold(9f),
                Tag = key
            };
            button.Click += (sender, _) => Browse((string)((Button)sender!).Tag!, "folder");
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 125, 245);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 94, 205);
            return button;
        }).ToArray();

        foreach (var keyIndex in Enumerable.Range(0, keys.Length))
        {
            _fields[keys[keyIndex]] = pathFields[keyIndex];
            host.Controls.Add(labels[keyIndex]);
            host.Controls.Add(pathFields[keyIndex]);
            host.Controls.Add(browseButtons[keyIndex]);
        }
        foreach (var field in pathFields) field.TextChanged += (_, _) => QueueSettingsSave();
        // usuário que digita vê as mudanças gravadas na hora; Procurar também grava ao confirmar
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.BackColor = Color.FromArgb(249, 251, 254);
        _password.Margin = Padding.Empty;
        _savePassword.Margin = Padding.Empty;
        host.Controls.Add(labels[4]);
        host.Controls.Add(_password);
        host.Controls.Add(_savePassword);
        host.Controls.Add(new Label { Text = "Nº de bases no Regedit", AutoSize = true, ForeColor = Muted, Font = UiTypography.Body(9f), Location = new Point(0, 187) });
        _baseCount.Location = new Point(180, 180);
        host.Controls.Add(_baseCount);
        _darkMode.Location = new Point(0, 219);
        host.Controls.Add(_darkMode);
        _forceMultiBase.Location = new Point(0, 249);
        host.Controls.Add(_forceMultiBase);

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
                    _password.Width = Math.Max(100, host.ClientSize.Width - fieldLeft - buttonWidth - rightInset - 8);
                    _password.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    _password.Height = 30;
                    _savePassword.Location = new Point(host.ClientSize.Width - buttonWidth, y + 6);
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

    private static Control BuildHelpCard(TableLayoutPanel page, int rowIndex, string text)
    {
        var padding = new Padding(16, 10, 16, 10);
        var card = Card();
        card.Padding = padding;
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = UiTypography.Body(9f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(label);

        void Fit()
        {
            var width = page.ClientSize.Width - page.Padding.Left - page.Padding.Right - padding.Left - padding.Right - 2;
            if (width < 50) width = 50;
            var measured = TextRenderer.MeasureText(text, label.Font, new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            // Margem extra de 8 px além da medição para não cortar a última linha.
            page.RowStyles[rowIndex].Height = measured.Height + padding.Top + padding.Bottom + 10;
        }

        page.Resize += (_, _) => Fit();
        Fit();
        return card;
    }

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
            if (control == _log || control == _resetLog || control == _restoreLog || control == _arqEmpEditor || control == _arqSistEditor || control == _arqEmpOutput || control == _arqSistOutput)
                continue; // SQL editors and the log retain their high-contrast palette.
            if (control is Panel panel)
            {
                if (panel == _progress) panel.BackColor = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(222, 228, 237);
                else if (panel == _progressFill) panel.BackColor = Color.FromArgb(31, 111, 235);
                else if (panel == _sidebar || panel == _menuFooter || panel == _updateRow) panel.BackColor = dark ? Color.FromArgb(28, 35, 47) : Color.White;
                else if (panel == _settingsFieldsHost) panel.BackColor = surface;
                else panel.BackColor = panel.BorderStyle == BorderStyle.None ? background : surface;
            }
            else if (control is TableLayoutPanel || control is FlowLayoutPanel) control.BackColor = background;

            if (control is ThemedProgressBar progressBar)
            {
                progressBar.TrackColor = dark ? Color.FromArgb(18, 25, 38) : Color.FromArgb(225, 232, 241);
                progressBar.FillColor = Color.FromArgb(31, 111, 235);
                progressBar.BorderColor = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(196, 207, 221);
                progressBar.Invalidate();
            }

            if (control is Label label)
            {
                label.ForeColor = label == _appVersionLabel
                    ? Color.FromArgb(190, 208, 230)
                    : label.Text == "TRAZ BASE" || label == _versionLabel
                    ? Color.FromArgb(31, 111, 235)
                    : label.Text is "Preparar base por solicitação" or "Transferir" or "Configurações" or "Comandos ARQEMP" or "Comandos ARQSIST"
                        ? text : muted;
                label.BackColor = label == _appVersionLabel
                    ? dark ? Color.FromArgb(38, 50, 68) : Color.FromArgb(231, 240, 255)
                    : Color.Transparent;
            }
            else if (control is ListView listView)
            {
                listView.BackColor = surface;
                listView.ForeColor = text;
            }
            else if (control is TextBox textBox) { textBox.BackColor = input; textBox.ForeColor = text; }
            else if (control is NumericUpDown numeric) { numeric.BackColor = input; numeric.ForeColor = text; }
            else if (control is CheckBox checkBox) { checkBox.BackColor = Color.Transparent; checkBox.ForeColor = text; }
            else if (control is Button button)
            {
                var isNavigation = button == _menuToggle || button == _transferNav || button == _changeBaseNav || button == _resetNav || button == _restoreNav || button == _settingsNav || button == _arqEmpNav || button == _arqSistNav;
                var isSecondaryAction = button == _openBaseFolderButton || button == _resetCancelButton || button == _restoreCancelButton;
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
        foreach (var button in new[] { _menuToggle, _transferNav, _changeBaseNav, _resetNav, _restoreNav, _settingsNav, _arqEmpNav, _arqSistNav }) button.BackColor = inactive;
        if (_transferView.Visible) _transferNav.BackColor = active;
        else if (_changeBaseView.Visible) _changeBaseNav.BackColor = active;
        else if (_resetView.Visible) _resetNav.BackColor = active;
        else if (_restoreView.Visible) _restoreNav.BackColor = active;
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
        nameof(AppSettings.TempPath) => _settings.TempPath,
        _ => ""
    };

    private void Browse(string key, string kind)
    {
        if (kind == "folder")
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = _fields[key].Text, ShowNewFolderButton = true };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _fields[key].Text = dialog.SelectedPath;
                try { PersistSettings(); }
                catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar as configurações: {ex.Message}", "Erro ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }
    }

    private void ReadFields()
    {
        _settings.SolicitationRoot = _fields[nameof(AppSettings.SolicitationRoot)].Text.Trim();
        _settings.BasesRoot = _fields[nameof(AppSettings.BasesRoot)].Text.Trim();
        _settings.SupportRoot = _fields[nameof(AppSettings.SupportRoot)].Text.Trim();
        _settings.TempPath = _fields[nameof(AppSettings.TempPath)].Text.Trim();
        _settings.BaseCount = Math.Clamp((int)_baseCount.Value, 1, 99);
        _settings.ArqEmpCommands = _arqEmpEditor.Text;
        _settings.ArqSistCommands = _arqSistEditor.Text;
    }

    private void PersistSettings()
    {
        ReadFields();
        _settings.SetFirebirdPassword(_savePassword.Checked ? _password.Text : null);
        _settings.Save();
    }

    private void QueueSettingsSave()
    {
        _settingsSaveTimer.Stop();
        _settingsSaveTimer.Start();
    }

    private void QueueCommandSettingsSave()
    {
        _commandSaveTimer.Stop();
        _commandSaveTimer.Start();
    }

    private void SaveCommandSettings()
    {
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
        if (sl.Length == 0 || sl.Any(c => !char.IsAsciiDigit(c))) { MessageBox.Show(this, "Digite o número da solicitação usando apenas números.\n\nExemplo: 123456", "Número inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_settings.BasesRoot) || string.IsNullOrWhiteSpace(_settings.SupportRoot)) { MessageBox.Show(this, "Antes de começar, defina estas pastas na tela Configuração:\n• Pasta local das bases\n• Pasta de bases de apoio", "Configuração necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!Directory.Exists(_settings.SolicitationRoot)) { MessageBox.Show(this, "Não foi possível acessar a pasta de origem das solicitações.\n\nVerifique se o compartilhamento de rede está disponível e tente novamente.", "Origem indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!Directory.Exists(_settings.BasesRoot) || !Directory.Exists(_settings.SupportRoot)) { MessageBox.Show(this, $"As pastas configuradas precisam existir e estar acessíveis:\n• Pasta local das bases: {_settings.BasesRoot}\n• Pasta de bases de apoio: {_settings.SupportRoot}\n\nConfira os caminhos na tela Configuração.", "Pastas indisponíveis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_password.Text)) { MessageBox.Show(this, "Para conectar ao Firebird, informe a senha na tela Configuração.", "Senha necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { PersistSettings(); }
        catch (Exception ex) { MessageBox.Show(this, $"Não foi possível salvar as configurações.\n\nDetalhe: {ex.Message}", "Configuração não salva", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        var source = Path.GetFullPath(Path.Combine(_settings.SolicitationRoot, sl));
        string destination;
        try { destination = BaseOperationRules.ResolveLocalBaseDirectory(_settings.BasesRoot, sl); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível definir a pasta de destino", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        if (!Directory.Exists(source)) { MessageBox.Show(this, $"Não encontrei a pasta da solicitação na origem:\n{source}\n\nConfira o número digitado e se o compartilhamento está acessível.", "SL não encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var destinationConflictChoice = ExistingDestinationAction.Cancel;
        if (Directory.Exists(destination))
        {
            var conflictResult = AppMessageBox.ShowFolderConflict(this,
                $"A pasta desta solicitação já existe:\n{destination}\n\n• Criar backup guarda uma cópia da pasta atual e continua.\n• Substituir apaga a pasta atual, sem guardar cópia.\n\nDeseja continuar?");
            destinationConflictChoice = BaseOperationRules.GetDestinationAction(conflictResult);
            if (destinationConflictChoice == ExistingDestinationAction.Cancel) return;
        }

        _running = true;
        _versionLabel.Text = "";
        _versionLabel.Visible = false;
        _runCancellation = new CancellationTokenSource();
        SetBusy(true);
        try
        {
            SetStatus("Preparando tudo para começar…");
            Log($"Iniciando a preparação da SL {sl}…");
            if (Directory.Exists(destination))
            {
                if (destinationConflictChoice == ExistingDestinationAction.CreateBackup)
                {
                    var backup = destination + ".backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    Directory.Move(destination, backup);
                    Log($"A pasta anterior foi preservada em: {backup}");
                }
                else
                {
                    Directory.Delete(destination, recursive: true);
                    Log("A pasta anterior foi substituída, sem cópia de segurança, conforme você escolheu.");
                }
            }
            Directory.CreateDirectory(destination);
            SetStatus("Copiando os arquivos da solicitação…");
            Log("Copiando os arquivos — isso pode levar alguns minutos.");
            await CopyDirectoryAsync(source, destination, _runCancellation.Token);
            Log("Cópia concluída.");
            SetStatus("Preparando a base…");

            await ExtractArchivesAsync(destination, _runCancellation.Token);
            var databaseFolders = MultiBaseOperationRules.FindDatabaseFolders(destination);
            var forced = !MultiBaseOperationRules.IsMultiBase(destination) && _settings.ForceMultiBase;
            var multiBase = forced || databaseFolders.Count > 0;
            string arqSistDatabase;
            if (multiBase)
            {
                if (forced) Log("Opção 'Sempre criar estrutura multi-base' ativa: aplicando o tratamento multi-base.");
                else Log("Pastas ARQ## encontradas: " + string.Join(", ", databaseFolders.Select(folder => Path.GetFileName(folder.DirectoryPath))) + ". Aplicando o tratamento multi-base.");
                Log("A ARQSIST usada será a da pasta ARQ01.");
                arqSistDatabase = await PrepareMultiBaseDatabasesAsync(destination, _runCancellation.Token);
            }
            else
            {
                MoveDatabasesToRoot(destination, _runCancellation.Token);
                await EnsureRequiredDatabasesAsync(destination, _runCancellation.Token);
                arqSistDatabase = Path.Combine(destination, "ARQSIST.FDB");
            }
            var arqEmpDatabase = multiBase
                ? Path.Combine(MultiBaseOperationRules.GetCompanyDirectory(destination), MultiBaseOperationRules.CompanyDatabaseEmp)
                : Path.Combine(destination, "ARQEMP.FDB");
            await RunSqlScriptsAsync(arqEmpDatabase, arqSistDatabase, destination, multiBase, _runCancellation.Token);
            if (multiBase) await UpdateRegistryForMultiBaseAsync(destination); else await UpdateRegistryAsync(destination);
            await ShowVersionAsync(arqSistDatabase, _runCancellation.Token);
            Log("Base preparada com sucesso. Ela já está selecionada para uso.");
            _status.Text = "Tudo pronto!";
            MessageBox.Show(this, $"A base da SL {sl} foi preparada e selecionada.\n\nÉ só abrir o sistema e trabalhar normalmente.", "Pronto!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) { Log("Cancelado a seu pedido. O que já tinha sido copiado permanece na pasta de destino."); _status.Text = "Cancelado"; }
        catch (Exception ex) { Log("Algo não saiu como esperado: " + ex.Message); _status.Text = "Não foi possível concluir"; MessageBox.Show(this, $"Algo não saiu como esperado:\n{ex.Message}\n\nVeja o log acima para saber em que etapa aconteceu.", "Não foi possível preparar a base", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _runCancellation?.Dispose(); _runCancellation = null; _running = false; SetBusy(false); }
    }

    private async Task ExtractArchivesAsync(string root, CancellationToken token)
    {
        var rars = Directory.GetFiles(root, "*.rar", SearchOption.AllDirectories);
        if (rars.Length > 0)
        {
            var selected = SelectArchives("Quais arquivos RAR você quer extrair?", rars);
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
            var selected = SelectArchives("Quais arquivos ZIP você quer extrair?", zips);
            token.ThrowIfCancellationRequested();
            foreach (var zip in selected)
            {
                Log("Extraindo " + Path.GetFileName(zip));
                SetArchiveProgress(0, "Extraindo " + Path.GetFileName(zip));
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
                SetArchiveProgress(100, "ZIP extraído com sucesso");
            }
        }
    }

    private async Task ExtractRarArchiveAsync(string archivePath, string destinationRoot, CancellationToken token)
    {
        Log("Extraindo " + Path.GetFileName(archivePath));
        SetArchiveProgress(0, "Extraindo " + Path.GetFileName(archivePath));
        await using var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var extractionProgress = new Progress<ProgressReport>(report =>
        {
            var percent = (int)Math.Clamp(Math.Round(report.PercentComplete ?? 0), 0d, 100d);
            var entryName = string.IsNullOrWhiteSpace(report.EntryPath) ? Path.GetFileName(archivePath) : Path.GetFileName(report.EntryPath);
            SetArchiveProgress(percent, $"Extraindo {entryName} ({percent}%)");
        });
        var readerOptions = ReaderOptions.ForExternalStream.WithProgress(extractionProgress);
        await using var reader = await ReaderFactory.OpenAsyncReader(archiveStream, readerOptions, token);
        while (await reader.MoveToNextEntryAsync(token))
        {
            token.ThrowIfCancellationRequested();
            var entry = reader.Entry;
            var entryKey = entry.Key ?? throw new InvalidOperationException("Há um arquivo sem nome dentro do RAR. Recompacte o arquivo ou extraia manualmente.");
            SetStatus("Extraindo " + Path.GetFileName(entryKey));
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
        SetArchiveProgress(100, "RAR extraído com sucesso");
    }

    private void SetArchiveProgress(int percent, string status)
    {
        _progressPercent = Math.Clamp(percent, 0, 100);
        UpdateProgressFill();
        SetStatus(status);
    }

    private static string GetSafeArchiveEntryPath(string destinationRoot, string entryKey)
    {
        return ArchivePathSafety.GetSafeEntryPath(destinationRoot, entryKey);
    }

    private string[] SelectArchives(string title, string[] files)
    {
        if (files.Length == 1) return files;
        if (files.Length > 1) NotifyIfMinimized(title, "O Traz Base aguarda a sua escolha dos arquivos.");
        var screen = Screen.FromControl(this);
        var dialogLocation = new Point(
            screen.WorkingArea.Left + Math.Max(0, (screen.WorkingArea.Width - 650) / 2),
            screen.WorkingArea.Top + Math.Max(0, (screen.WorkingArea.Height - 440) / 2));
        using var dialog = new Form { Text = title, Size = new Size(650, 440), Location = dialogLocation, StartPosition = FormStartPosition.Manual, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog, BackColor = Color.White };
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
        _status.Text = $"Copiando: {percent}%  ·  {copiedMb:N1} de {totalMb:N1} MB  ·  {filesCopied} de {totalFiles} arquivos";
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
            if (File.Exists(target)) throw new IOException($"Encontrei dois arquivos {Path.GetFileName(target)}: um na pasta raiz e outro numa subpasta.\nRemova um deles e tente novamente.");
            File.Move(file, target);
            Log("A base " + Path.GetFileName(target) + " foi movida para a raiz da pasta.");
        }
    }

    private async Task EnsureRequiredDatabasesAsync(string root, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var systemDb = Path.Combine(root, "ARQSIST.FDB");
        if (!File.Exists(systemDb))
        {
            NotifyIfMinimized("Selecione a ARQSIST.FDB", "O Traz Base precisa que você escolha o arquivo ARQSIST.");
            using var dialog = new OpenFileDialog { InitialDirectory = root, Title = "Selecione o arquivo que será usado como ARQSIST.FDB", Filter = "Bases Firebird (*.fdb)|*.fdb|Todos os arquivos (*.*)|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) throw new OperationCanceledException("A preparação foi interrompida porque nenhum arquivo foi selecionado.");
            await CopyFileAsync(dialog.FileName, systemDb, token);
            Log("ARQSIST.FDB criado com o arquivo que você selecionou.");
        }
        foreach (var supportFile in BaseOperationRules.GetSupportFilesToCopy(root, _settings.SupportRoot))
        {
            token.ThrowIfCancellationRequested();
            await CopyFileAsync(supportFile.Source, supportFile.Destination, token);
            Log($"Arquivo de apoio copiado: {supportFile.Name}");
        }
        if (!File.Exists(Path.Combine(root, "ARQEMP.FDB")) || !File.Exists(systemDb)) throw new InvalidOperationException("Faltam a ARQEMP.FDB ou a ARQSIST.FDB na pasta da base e elas também não existem na pasta de apoio.");
    }

    private async Task<string> PrepareMultiBaseDatabasesAsync(string root, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var companyDirectory = MultiBaseOperationRules.GetCompanyDirectory(root);
        Directory.CreateDirectory(companyDirectory);

        foreach (var databaseName in new[] { MultiBaseOperationRules.CompanyDatabaseEmp, MultiBaseOperationRules.CompanyDatabaseOcor })
        {
            token.ThrowIfCancellationRequested();
            var destination = Path.Combine(companyDirectory, databaseName);
            string? source = File.Exists(Path.Combine(root, databaseName)) ? Path.Combine(root, databaseName) : null;
            if (source is null)
            {
                var nested = MultiBaseOperationRules.FindFilesOutsideManagedFolders(root, databaseName);
                if (nested.Count > 1)
                    throw new IOException($"Encontrei mais de uma {databaseName} em subpastas:\n{string.Join("\n", nested)}\nRemova as extras ou mantenha apenas uma e tente novamente.");
                source = nested.Count == 1 ? nested[0] : null;
            }

            if (source is not null)
            {
                if (File.Exists(destination))
                    throw new IOException($"Encontrei {databaseName} em {Path.GetDirectoryName(source)} e também na pasta ARQ00. Resolva o conflito antes de continuar.");
                File.Move(source, destination);
                Log($"{databaseName} movida para a pasta ARQ00.");
            }
            else if (!File.Exists(destination))
            {
                var support = Path.Combine(_settings.SupportRoot, databaseName);
                if (!File.Exists(support))
                    throw new FileNotFoundException($"{databaseName} não existe na raiz nem em subpastas, nem na pasta ARQ00, nem na pasta de apoio.", support);
                await CopyFileAsync(support, destination, token);
                Log($"Arquivo de apoio copiado para ARQ00: {databaseName}");
            }
            else
            {
                Log($"{databaseName} já existe na pasta ARQ00 — mantida.");
            }
        }

        var arqSist = MultiBaseOperationRules.GetArqSistPath(root);
        var sistCandidates = MultiBaseOperationRules.FindFilesOutsideManagedFolders(root, "ARQSIST.FDB");
        if (sistCandidates.Count > 1)
            throw new IOException($"Encontrei mais de uma ARQSIST.FDB fora das pastas ARQ01/ARQ00:\n{string.Join("\n", sistCandidates)}\nRemova as extras ou mantenha apenas uma e tente novamente.");
        if (sistCandidates.Count == 1 && File.Exists(arqSist))
            throw new IOException($"Encontrei ARQSIST.FDB em {Path.GetDirectoryName(sistCandidates[0])} e também na pasta ARQ01. Resolva o conflito antes de continuar.");

        if (sistCandidates.Count == 1 && !File.Exists(arqSist))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arqSist)!);
            File.Move(sistCandidates[0], arqSist);
            Log("ARQSIST.FDB movida para a pasta ARQ01.");
        }
        else if (!File.Exists(arqSist))
        {
            NotifyIfMinimized("Selecione a ARQSIST.FDB", "O Traz Base precisa que você escolha o arquivo ARQSIST para a pasta ARQ01.");
            using var dialog = new OpenFileDialog { InitialDirectory = root, Title = "Selecione o arquivo que será usado como ARQSIST.FDB (pasta ARQ01)", Filter = "Bases Firebird (*.fdb)|*.fdb|Todos os arquivos (*.*)|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) throw new OperationCanceledException("A preparação foi interrompida porque nenhum arquivo foi selecionado.");
            Directory.CreateDirectory(Path.GetDirectoryName(arqSist)!);
            await CopyFileAsync(dialog.FileName, arqSist, token);
            Log("ARQSIST.FDB criado em ARQ01 com o arquivo que você selecionou.");
        }
        else
        {
            Log("ARQSIST.FDB encontrada na pasta ARQ01.");
        }

        if (!File.Exists(Path.Combine(companyDirectory, MultiBaseOperationRules.CompanyDatabaseEmp)) ||
            !File.Exists(Path.Combine(companyDirectory, MultiBaseOperationRules.CompanyDatabaseOcor)) ||
            !File.Exists(arqSist))
            throw new InvalidOperationException("A validação das bases obrigatórias falhou: confira ARQEMP.FDB/ARQOCOR.FDB na pasta ARQ00 e ARQSIST.FDB na pasta ARQ01.");

        return arqSist;
    }

    private async Task RunSqlScriptsAsync(string arqEmpDatabase, string arqSistDatabase, string destination, bool multiBase, CancellationToken token)
    {
        await RunSqlIfConfiguredAsync(_arqEmpEditor.Text, arqEmpDatabase, "Comandos ARQEMP", token);

        var targets = GetArqSistTargetsForSql(destination, multiBase, arqSistDatabase);
        foreach (var database in targets)
        {
            var label = targets.Count == 1
                ? "Comandos ARQSIST"
                : "Comandos ARQSIST (" + Path.GetFileName(Path.GetDirectoryName(database)!) + ")";
            await RunSqlIfConfiguredAsync(_arqSistEditor.Text, database, label, token);
        }
    }

    private IReadOnlyList<string> GetArqSistTargetsForSql(string destination, bool multiBase, string defaultArqSist)
    {
        if (!multiBase || string.IsNullOrWhiteSpace(_arqSistEditor.Text))
            return new[] { defaultArqSist };

        foreach (var folder in MultiBaseOperationRules.GetFolderNamesWithoutArqSist(destination))
            Log($"{folder}: sem ARQSIST.FDB — os comandos ARQSIST não serão executados nessa pasta.");

        var existing = MultiBaseOperationRules.GetArqSistDatabases(destination);
        if (existing.Count <= 1) return new[] { defaultArqSist };

        var names = existing.Select(database => Path.GetFileName(Path.GetDirectoryName(database)!) ?? database).ToList();
        var answer = MessageBox.Show(this,
            $"Executar os comandos ARQSIST em todas as {existing.Count} bases ARQSIST?\n\nPastas: {string.Join(", ", names)}\n\nSim = executa em todas, uma após a outra.\nNão = executa somente na ARQ01.",
            "Comandos ARQSIST", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        return answer == DialogResult.Yes ? existing : new[] { defaultArqSist };
    }

    private async Task RunSqlIfConfiguredAsync(string commands, string database, string label, CancellationToken token, Action<string>? operationLog = null)
    {
        if (string.IsNullOrWhiteSpace(commands)) { WriteOperationLog(operationLog, $"{label}: nada a executar (campo vazio)."); return; }
        token.ThrowIfCancellationRequested();
        SetStatus($"Conectando em {Path.GetFileName(database)}…");
        WriteOperationLog(operationLog, $"Conectando ao banco {database}");
        var connectionString = BuildFirebirdConnectionString(database);
        await using var connection = new FbConnection(connectionString);
        await connection.OpenAsync(token);

        var parsedScript = new FbScript(commands);
        parsedScript.Parse();
        var batch = new FbBatchExecution(connection);
        batch.AppendSqlStatements(parsedScript);

        SetStatus($"Executando {label} no banco…");
        WriteOperationLog(operationLog, $"{label}: executando os comandos no banco…");
        using var cancelRegistration = token.Register(() =>
        {
            try { connection.Close(); } catch { }
        });
        try { await Task.Run(() => batch.Execute(), CancellationToken.None); }
        catch (Exception) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        token.ThrowIfCancellationRequested();
        WriteOperationLog(operationLog, $"{label}: comandos executados com sucesso.");
    }

    private void NotifyIfMinimized(string caption, string? body = null)
    {
        // Sempre ativa: notificar quando há perguntas/mensagens pendentes com a janela minimizada.
        if (WindowState != FormWindowState.Minimized) return;
        ShowBalloon(caption, body);
    }

    private void ShowBalloon(string caption, string? body)
    {
        if (_notifyIcon is null)
        {
            _notifyIcon = new NotifyIcon { Icon = Icon, Text = "Traz Base", Visible = true };
            _notifyIcon.BalloonTipClicked += (_, _) => RestoreFromMinimized();
        }
        var message = body is { Length: > 0 }
            ? body + "\nClique aqui para abrir a janela."
            : "O Traz Base precisa da sua atenção.\nClique aqui para abrir a janela.";
        _notifyIcon.ShowBalloonTip(5000, caption, message, ToolTipIcon.Info);
    }

    private void RestoreFromMinimized()
    {
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private string GetConfiguredTempPath()
    {
        var custom = _settings.TempPath?.Trim();
        if (!string.IsNullOrWhiteSpace(custom)) return custom;
        return TryGetSicTempPath() ?? Path.GetTempPath();
    }

    private static string? TryGetSicTempPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SIC\CONEXAO", writable: false);
            var value = key?.GetValue("TEMP") as string;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch
        {
            return null; // Registro inacessível/ausente — cai no padrão do Windows.
        }
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

    private async Task UpdateRegistryAsync(string root, Action<string>? operationLog = null)
    {
        WriteOperationLog(operationLog, "Apontando o sistema para esta base (Registro do Windows)…");
        using var key = Registry.CurrentUser.CreateSubKey(@"SIC\CONEXAO", true) ?? throw new InvalidOperationException("Não foi possível atualizar o Registro do Windows (HKCU\\SIC\\CONEXAO).\nVerifique as permissões do seu usuário e tente novamente.");
        var basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var baseCount = Math.Clamp(_settings.BaseCount, 1, 99);
        for (var number = 1; number <= baseCount; number++)
            key.SetValue("BASE" + number, basePath, RegistryValueKind.String);
        WriteOperationLog(operationLog, $"{baseCount} base(s) configurada(s): BASE1…BASE{baseCount} = {basePath}");
        key.SetValue("EMPRESA", basePath, RegistryValueKind.String);
        key.SetValue("SERVIDOR", "LOCALHOST", RegistryValueKind.String);
        key.SetValue("TEMP", GetConfiguredTempPath(), RegistryValueKind.String);
        WriteOperationLog(operationLog, "O Registro foi atualizado; o sistema já aponta para esta base.");
        await Task.CompletedTask;
    }

    private async Task UpdateRegistryForMultiBaseAsync(string root, Action<string>? operationLog = null)
    {
        WriteOperationLog(operationLog, "Apontando o sistema para as bases ARQ## (Registro do Windows)…");
        using var key = Registry.CurrentUser.CreateSubKey(@"SIC\CONEXAO", true) ?? throw new InvalidOperationException("Não foi possível atualizar o Registro do Windows (HKCU\\SIC\\CONEXAO).\nVerifique as permissões do seu usuário e tente novamente.");

        foreach (var (name, value) in MultiBaseOperationRules.GetBaseKeyWrites(root))
        {
            key.SetValue(name, value, RegistryValueKind.String);
            WriteOperationLog(operationLog, $"{name} = {value}");
        }
        foreach (var name in MultiBaseOperationRules.GetBaseKeyDeletions(root))
        {
            if (key.GetValue(name) is not null)
            {
                key.DeleteValue(name, throwOnMissingValue: false);
                WriteOperationLog(operationLog, $"{name} removida (nenhuma pasta correspondente na SL).");
            }
        }

        var companyPath = MultiBaseOperationRules.WithTrailingSeparator(MultiBaseOperationRules.GetCompanyDirectory(root));
        key.SetValue("EMPRESA", companyPath, RegistryValueKind.String);
        key.SetValue("SERVIDOR", "LOCALHOST", RegistryValueKind.String);
        key.SetValue("TEMP", GetConfiguredTempPath(), RegistryValueKind.String);
        WriteOperationLog(operationLog, $"EMPRESA = {companyPath}");
        WriteOperationLog(operationLog, "SIC apontando para as bases recebidas (Registro atualizado).");
        await Task.CompletedTask;
    }

    private void WriteOperationLog(Action<string>? operationLog, string message)
    {
        if (operationLog is null) Log(message);
        else operationLog(message);
    }

    private async Task ShowVersionAsync(string database, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        SetStatus("Consultando a versão da base ARQSIST…");
        Log($"Consultando a versão em {database}");
        await using var connection = new FbConnection(BuildFirebirdConnectionString(database));
        await connection.OpenAsync(token);
        await using var command = new FbCommand("SELECT NRO_SCRIPT AS VERSAO FROM CONTROLE", connection)
        {
            CommandTimeout = 0
        };
        await using var reader = await command.ExecuteReaderAsync(token);
        var version = await reader.ReadAsync(token) ? Convert.ToString(reader["VERSAO"]) : "sem versão registrada";
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
        _menuToggle.Enabled = true;
        _transferNav.Enabled = true;
        _changeBaseNav.Enabled = !busy;
        _resetNav.Enabled = !busy;
        _restoreNav.Enabled = !busy;
        _settingsNav.Enabled = !busy;
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
            SetStatus("Só um instante…");
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
