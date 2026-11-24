using System.Drawing.Drawing2D;

namespace TrazBase.App;

internal sealed partial class MainForm
{
    private TutorialGuide? _tutorialGuide;

    /// <summary>True enquanto o tour guiado está ativo (usado para bloquear o fechamento do app).</summary>
    internal bool IsTutorialActive => _tutorialGuide is not null;

    private async Task ShowTutorialAfterWelcomeAsync()
    {
        // Pequena pausa para a notificação de boas-vindas aparecer antes do spotlight.
        await Task.Delay(1400);
        if (IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            if (!IsDisposed) StartTutorial();
        }));
    }

    private void StartTutorial()
    {
        _tutorialGuide?.Dispose();
        _tutorialGuide = new TutorialGuide(this);
        _tutorialGuide.Disposed += (_, _) => _tutorialGuide = null;
        _tutorialGuide.Start();
    }

    private void ShowSettingsTab(int index)
    {
        ShowView(settings: true);
        if (_settingsTabs is not null && index >= 0 && index < _settingsTabs.TabPages.Count)
            _settingsTabs.SelectedIndex = index;
    }

    private Control? GetVisibleView() =>
        _homeView.Visible ? _homeView
        : _transferView.Visible ? _transferView
        : _settingsView.Visible ? _settingsView
        : _changeBaseView.Visible ? _changeBaseView
        : _resetView.Visible ? _resetView
        : _restoreView.Visible ? _restoreView
        : _updateSicView.Visible ? _updateSicView
        : _configureSicView.Visible ? _configureSicView
        : _arqEmpView.Visible ? _arqEmpView
        : _arqSistView.Visible ? _arqSistView
        : _folderShortcutsView.Visible ? _folderShortcutsView
        : _copySqlView.Visible ? _copySqlView
        : null;

    private Control? GetNavigationForView(Control? view) => view switch
    {
        null => null,
        _ when view == _homeView => _homeNav,
        _ when view == _transferView => _transferNav,
        _ when view == _settingsView => _settingsNav,
        _ when view == _changeBaseView => _changeBaseNav,
        _ when view == _resetView => _resetNav,
        _ when view == _restoreView => _restoreNav,
        _ when view == _updateSicView => _updateSicNav,
        _ when view == _configureSicView => _configureSicNav,
        _ when view == _arqEmpView => _arqEmpNav,
        _ when view == _arqSistView => _arqSistNav,
        _ when view == _folderShortcutsView => _folderShortcutsNav,
        _ when view == _copySqlView => _copySqlNav,
        _ => null
    };

    /// <summary>
    /// Tour guiado reduzido: Configurações → Tema → Transferir → Alterar base → encerramento.
    /// </summary>
    private sealed class TutorialGuide : IDisposable
    {
        private const int HighlightPadding = 3;
        private readonly MainForm _owner;
        private readonly List<TutorialStep> _steps;
        private int _index;
        private ClickThroughOverlayForm? _overlay;
        private HighlightForm? _highlight;
        private TutorialPopup? _popup;
        private System.Windows.Forms.Timer? _dialogWatchTimer;
        private readonly HashSet<Control> _hookedBrowses = [];
        private Control? _lastVisibleView;
        private bool _disposed;

        public event EventHandler? Disposed;

        public TutorialGuide(MainForm owner)
        {
            _owner = owner;
            _steps = BuildSteps();
        }

        public void Start()
        {
            if (_disposed || _steps.Count == 0) return;
            ShowStep(0);
        }

        private void ShowStep(int index)
        {
            if (_disposed) return;
            _index = Math.Clamp(index, 0, _steps.Count - 1);
            var step = _steps[_index];

            step.BeforeShow?.Invoke();
            _owner.Refresh();
            Application.DoEvents();

            EnsureForms();
            var targets = step.Targets.Select(getTarget => getTarget()).Where(c => c is not null).Cast<Control>().ToList();

            // Quando o tour troca de tela, destaque também o item do menu lateral dessa tela.
            var visibleView = _owner.GetVisibleView();
            var viewChanged = visibleView != _lastVisibleView;
            _lastVisibleView = visibleView;
            if (viewChanged && targets.Count > 0)
            {
                var navigation = _owner.GetNavigationForView(visibleView);
                if (navigation is not null && IsReallyVisible(navigation) && !targets.Contains(navigation))
                    targets.Add(navigation);
            }

            UpdateFocusArea(targets);
            _popup!.SetStep(step, _index + 1, _steps.Count);
            PositionPopup(step, targets);
            _popup.BringToFront();
            _popup.Activate();
        }

        private void EnsureForms()
        {
            if (_popup is not null) return;

            var bounds = _owner.Bounds;
            _overlay = new ClickThroughOverlayForm(bounds) { Owner = _owner };
            _overlay.Show();

            _highlight = new HighlightForm { Owner = _owner };
            _highlight.Show();

            _popup = new TutorialPopup(
                dark: _owner._settings.DarkMode,
                previous: () => ShowStep(_index - 1),
                next: () =>
                {
                    if (_index >= _steps.Count - 1) Finish(false);
                    else ShowStep(_index + 1);
                },
                skip: () => Finish(true))
            {
                Owner = _owner
            };
            // Sempre que o usuário clicar fora (no app), traz a mensagem do tour de volta para cima.
            _owner.Activated += OwnerActivated;
            _popup.Show();
        }

        private void OwnerActivated(object? sender, EventArgs e)
        {
            if (_disposed || _popup is null || _popup.IsDisposed) return;
            _popup.BringToFront();
            _popup.Activate();
        }

        private void UpdateFocusArea(IReadOnlyList<Control> targets)
        {
            var rects = new List<Rectangle>();
            foreach (var target in targets)
            {
                if (!target.IsHandleCreated || !IsReallyVisible(target)) continue;
                try
                {
                    rects.Add(PaddedScreenBounds(target));
                    var browse = GetAssociatedBrowseButton(target);
                    if (browse is not null && browse.IsHandleCreated && IsReallyVisible(browse))
                    {
                        rects.Add(PaddedScreenBounds(browse));
                        HookBrowseButton(browse);
                    }
                }
                catch { /* alvo pode ter sido descartado durante a troca de tela */ }
            }

            _highlight?.SetRectangles(rects);
            _overlay?.SetHoles(rects);
        }

        private static Rectangle PaddedScreenBounds(Control control)
        {
            var rect = control.RectangleToScreen(control.ClientRectangle);
            rect.Inflate(HighlightPadding, HighlightPadding);
            return rect;
        }

        private Control? GetAssociatedBrowseButton(Control? target)
        {
            if (target is null) return null;
            foreach (var pair in _owner._fields)
            {
                if (pair.Value == target && _owner._browseButtons.TryGetValue(pair.Key, out var browse))
                    return browse;
            }
            if (target == _owner._restoreFolder) return _owner._restoreBrowseButton;
            return null;
        }

        private void HookBrowseButton(Control browse)
        {
            if (_hookedBrowses.Contains(browse)) return;
            _hookedBrowses.Add(browse);
            // MouseDown antecede o Click (que abre o diálogo modal), permitindo esconder o overlay a tempo.
            browse.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) SuspendOverlayForDialog();
            };
        }

        private void SuspendOverlayForDialog()
        {
            if (_disposed) return;
            if (_overlay is not null) _overlay.Visible = false;
            if (_highlight is not null) _highlight.Visible = false;
            if (_popup is not null) _popup.Visible = false;

            _dialogWatchTimer ??= new System.Windows.Forms.Timer { Interval = 150 };
            _dialogWatchTimer.Tick -= OnDialogWatchTick;
            _dialogWatchTimer.Tick += OnDialogWatchTick;
            _dialogWatchTimer.Start();
        }

        private void OnDialogWatchTick(object? sender, EventArgs e)
        {
            if (_disposed || !_owner.Enabled) return; // diálogo modal ainda aberto
            _dialogWatchTimer?.Stop();
            if (_overlay is not null) _overlay.Visible = true;
            if (_popup is not null)
            {
                _popup.Visible = true;
                _popup.BringToFront();
                _popup.Activate();
            }
        }

        private static bool IsReallyVisible(Control control)
        {
            var current = control;
            while (current is not null)
            {
                if (!current.Visible) return false;
                current = current.Parent;
            }
            return true;
        }

        /// <summary>
        /// Retorna o controle apenas se ele estiver realmente visível e inteiramente dentro
        /// da área do formulário (não cortado por painel com rolagem).
        /// </summary>
        private Control? VisibleWithoutScroll(Control? control)
        {
            if (control is null || !IsReallyVisible(control)) return null;
            try
            {
                var formBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                var rect = control.RectangleToScreen(control.ClientRectangle);
                return formBounds.Contains(rect) ? control : null;
            }
            catch { return null; }
        }

        private void PositionPopup(TutorialStep step, IReadOnlyList<Control> targets)
        {
            if (_popup is null) return;
            var area = Screen.FromControl(_owner).WorkingArea;
            var gap = HighlightPadding + 12;

            // No passo dos comandos SQL, manter o popup no canto inferior direito para não cobrir o editor.
            if (step.KeepSqlEditorUncovered)
            {
                var formBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                _popup.Location = new Point(
                    ClampToRange(formBounds.Right - _popup.Width - 18, area.Left, area.Right - _popup.Width),
                    ClampToRange(formBounds.Bottom - _popup.Height - 18, area.Top, area.Bottom - _popup.Height));
                return;
            }

            var focus = Rectangle.Empty;
            foreach (var target in targets)
            {
                if (!target.Visible) continue;
                try
                {
                    var rect = target.RectangleToScreen(target.ClientRectangle);
                    focus = focus == Rectangle.Empty ? rect : Rectangle.Union(focus, rect);
                }
                catch { /* alvo pode ter sido descartado */ }
            }

            if (focus == Rectangle.Empty)
            {
                _popup.Location = new Point(
                    area.Left + Math.Max(0, (area.Width - _popup.Width) / 2),
                    area.Top + Math.Max(0, (area.Height - _popup.Height) / 2));
                return;
            }

            // Apenas os passos marcados com PlacePopupBesideTargets ficam ao lado.
            if (step.PlacePopupBesideTargets)
            {
                var y = ClampToRange(focus.Top, area.Top, area.Bottom - _popup.Height);
                var rightX = focus.Right + gap;
                if (rightX + _popup.Width <= area.Right)
                {
                    _popup.Location = new Point(rightX, y);
                    return;
                }
                var leftX = focus.Left - _popup.Width - gap;
                if (leftX >= area.Left)
                {
                    _popup.Location = new Point(leftX, y);
                    return;
                }

                // Não há espaço lateral: centralizar verticalmente evita cobrir o editor e o resultado.
                if (step.CenterPopupVertically)
                {
                    _popup.Location = new Point(
                        ClampToRange((area.Left + area.Right - _popup.Width) / 2, area.Left, area.Right - _popup.Width),
                        ClampToRange(focus.Top + Math.Max(0, (focus.Height - _popup.Height) / 2), area.Top, area.Bottom - _popup.Height));
                    return;
                }
            }

            // Comportamento padrão: abaixo do primeiro alvo (ou acima, se não couber).
            var first = targets.FirstOrDefault();
            if (first is not null && first.Visible)
            {
                try
                {
                    var rect = first.RectangleToScreen(first.ClientRectangle);
                    var x = ClampToRange(rect.Left, area.Left, area.Right - _popup.Width);
                    var below = rect.Bottom + gap;
                    var y = below + _popup.Height <= area.Bottom
                        ? below
                        : Math.Max(area.Top, rect.Top - _popup.Height - gap);
                    _popup.Location = new Point(x, y);
                    return;
                }
                catch { /* cai para o centro */ }
            }

            _popup.Location = new Point(
                area.Left + Math.Max(0, (area.Width - _popup.Width) / 2),
                area.Top + Math.Max(0, (area.Height - _popup.Height) / 2));
        }

        private static int ClampToRange(int value, int min, int max) =>
            Math.Clamp(value, min, Math.Max(min, max));

        private void Finish(bool skipped)
        {
            if (_disposed) return;
            try
            {
                _owner._settings.TutorialCompleted = true;
                _owner._settings.TutorialVersionShown = MainForm.GetCurrentVersion().ToString(3);
                _owner.PersistSettings();
            }
            catch
            {
                // Mesmo falhando ao gravar, o tour é encerrado para não travar o usuário.
            }
            Dispose();
            try { if (!_owner.IsDisposed) _owner.Activate(); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Activated -= OwnerActivated;
            _dialogWatchTimer?.Stop();
            _dialogWatchTimer?.Dispose();
            _dialogWatchTimer = null;
            try { _popup?.Close(); } catch { }
            try { _highlight?.Close(); } catch { }
            try { _overlay?.Close(); } catch { }
            _popup?.Dispose();
            _highlight?.Dispose();
            _overlay?.Dispose();
            _popup = null;
            _highlight = null;
            _overlay = null;
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        private List<TutorialStep> BuildSteps() =>
        [
            new TutorialStep(
                "Bem-vindo ao Traz Base!",
                "Olá! Seja bem-vindo ao Traz Base.\n\n" +
                "Use Próximo para avançar. Se preferir, clique em Pular no canto superior direito — você poderá rever este tutorial depois em Configurações → Geral.",
                beforeShow: () => _owner.ShowHomeView(),
                target: null),

            new TutorialStep(
                "Configurações — aba Geral",
                "Nesta aba ficam as pastas fundamentais (origem das solicitações, bases locais, bases de apoio e temporária) e as preferências de comportamento: nº de bases no Regedit, tela inicial, estrutura multi-base, renomeação da SL e execução em segundo plano.\n\n" +
                "Todas as alterações são salvas automaticamente.",
                beforeShow: () => _owner.ShowSettingsTab(0),
                target: () => _owner._generalSettingsHost,
                placePopupBesideTargets: true),

            new TutorialStep(
                "Configurações — aba Senhas",
                "Aqui ficam as senhas usadas pelo sistema: Firebird (bases .FDB), padrão do SIC e do sistema Empresa. Marque “Salvar senha” para protegê-las com a conta do Windows.\n\n" +
                "Os atalhos Ctrl+Alt+C e Ctrl+Alt+X copiam rapidamente as senhas do SIC e da Empresa.",
                beforeShow: () => _owner.ShowSettingsTab(1),
                target: () => _owner._passwordsSettingsHost,
                placePopupBesideTargets: true),

            new TutorialStep(
                "Configurações — aba VPN",
                "Use esta aba quando a origem das solicitações só estiver acessível fora da rede: escolha a conexão VPN, credenciais, tentativas de reconexão e um script opcional executado ao conectar.\n\n" +
                "O aplicativo conecta antes da transferência e desconecta ao final, se ela não estava conectada.",
                beforeShow: () => _owner.ShowSettingsTab(2),
                target: () => _owner._vpnSettingsHost,
                placePopupBesideTargets: true),

            new TutorialStep(
                "Tema claro e escuro",
                "Este interruptor no rodapé do menu alterna entre o tema claro e o escuro. A preferência é salva automaticamente.",
                target: () => _owner._themeToggle),

            new TutorialStep(
                "Comandos SQL (ARQEMP e ARQSIST)",
                "Nestas telas você escolhe a base Firebird (.FDB) em “Procurar”, escreve os comandos no editor e clica em “Testar comando”. O resultado aparece na área inferior e o editor salva automaticamente.\n\n" +
                "Há uma tela para a ARQEMP e outra para a ARQSIST.",
                beforeShow: () => _owner.ShowCommandView(arqEmp: true),
                target: () => _owner._arqEmpEditor,
                extraTargets:
                [
                    () => _owner._arqSistNav
                ],
                keepSqlEditorUncovered: true),

            new TutorialStep(
                "Transferir",
                "Na tela Transferir, digite o número da solicitação (SL) e clique em “Trazer e preparar base”. O aplicativo copia os arquivos, extrai os compactados, ajusta os bancos e aponta o Registro.\n\n" +
                "O andamento aparece no log e, ao concluir, a versão do SIC é exibida no canto da tela.",
                beforeShow: () => _owner.ShowView(settings: false),
                target: () => _owner._sl,
                placePopupBesideTargets: true),

            new TutorialStep(
                "Alterar base",
                "Escolha uma pasta na lista e clique em “Trocar base” para apontar o Registro para ela. “Abrir pasta” abre a base no Explorer.\n\n" +
                "A lista pode ser ordenada por nome ou data de modificação.",
                beforeShow: () => _owner.ShowChangeBaseView(),
                target: () => _owner._baseFolders,
                placePopupBesideTargets: true),

            new TutorialStep(
                "Tudo pronto!",
                "Você já conhece o essencial para começar: configurar o sistema, trazer e preparar bases, trocar a base ativa e testar comandos SQL.\n\n" +
                "No menu lateral você também encontra Redefinir base, Restaurar base, Atualização de executáveis, Configurar caminhos, Atalhos de pasta e Copiar SQL — explore à medida que precisar.\n\n" +
                "Podemos atualizar o aplicativo pelo botão no rodapé do menu.\n\n" +
                "Se quiser rever este passo a passo, abra Configurações → Geral → “Ver tutorial guiado”.\n\n" +
                "Bom trabalho!",
                beforeShow: () => _owner.ShowHomeView(),
                target: null)
        ];

        private sealed class TutorialStep
        {
            public TutorialStep(
                string title,
                string text,
                Action? beforeShow = null,
                Func<Control?>? target = null,
                IReadOnlyList<Func<Control?>>? extraTargets = null,
                bool placePopupBesideTargets = false,
                bool centerPopupVertically = false,
                bool keepSqlEditorUncovered = false)
            {
                Title = title;
                Text = text;
                BeforeShow = beforeShow;
                PlacePopupBesideTargets = placePopupBesideTargets;
                CenterPopupVertically = centerPopupVertically;
                KeepSqlEditorUncovered = keepSqlEditorUncovered;
                var all = new List<Func<Control?>>();
                if (target is not null) all.Add(target);
                if (extraTargets is not null) all.AddRange(extraTargets);
                Targets = all;
            }

            public string Title { get; }
            public string Text { get; }
            public Action? BeforeShow { get; }
            public IReadOnlyList<Func<Control?>> Targets { get; }
            public bool PlacePopupBesideTargets { get; }
            public bool CenterPopupVertically { get; }
            public bool KeepSqlEditorUncovered { get; }
        }

        /// <summary>
        /// Overlay que escurece a janela, mas permite clicar nas áreas "destacadas"
        /// (campo e, quando existir, seu botão Procurar) através de buracos na Region.
        /// </summary>
        private sealed class ClickThroughOverlayForm : Form
        {
            public ClickThroughOverlayForm(Rectangle bounds)
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
                BackColor = Color.Black;
                Opacity = 0.35;
                ShowInTaskbar = false;
                ControlBox = false;
            }

            public void SetHoles(IReadOnlyList<Rectangle> screenRectangles)
            {
                if (IsDisposed) return;
                var region = new Region(new Rectangle(0, 0, Width, Height));
                foreach (var rect in screenRectangles)
                {
                    var local = new Rectangle(rect.Left - Left, rect.Top - Top, rect.Width, rect.Height);
                    if (local.Width > 0 && local.Height > 0) region.Exclude(local);
                }
                var previous = Region;
                Region = region;
                previous?.Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) Region?.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class HighlightForm : Form
        {
            private readonly Pen _pen = new(Color.FromArgb(31, 111, 235), 3);
            private List<Rectangle> _localRectangles = [];

            public HighlightForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                BackColor = Color.Magenta;
                TransparencyKey = Color.Magenta;
                ShowInTaskbar = false;
            }

            public void SetRectangles(IReadOnlyList<Rectangle> screenRectangles)
            {
                if (screenRectangles.Count == 0)
                {
                    _localRectangles = [];
                    Visible = false;
                    return;
                }

                var union = screenRectangles[0];
                foreach (var rect in screenRectangles) union = Rectangle.Union(union, rect);
                Bounds = union;
                _localRectangles = screenRectangles
                    .Select(rect => new Rectangle(rect.Left - Left, rect.Top - Top, rect.Width, rect.Height))
                    .ToList();
                Visible = true;
                Invalidate();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT: cliques atravessam a moldura
                    return cp;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                foreach (var rect in _localRectangles)
                {
                    var inner = new Rectangle(rect.Left + 2, rect.Top + 2, Math.Max(1, rect.Width - 4), Math.Max(1, rect.Height - 4));
                    using var path = RoundedRectangle(inner, 2);
                    e.Graphics.DrawPath(_pen, path);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _pen.Dispose();
                base.Dispose(disposing);
            }

            private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
            {
                var path = new GraphicsPath();
                var diameter = radius * 2;
                var inner = new Rectangle(bounds.Left + radius, bounds.Top + radius, bounds.Width - diameter, bounds.Height - diameter);
                if (inner.Width < 0 || inner.Height < 0)
                {
                    path.AddRectangle(bounds);
                    return path;
                }
                path.AddArc(inner.Left, inner.Top, diameter, diameter, 180, 90);
                path.AddArc(inner.Right - diameter, inner.Top, diameter, diameter, 270, 90);
                path.AddArc(inner.Right - diameter, inner.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(inner.Left, inner.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private sealed class TutorialPopup : Form
        {
            private const int PopupWidth = 452;
            private const int HeaderHeight = 46;
            private const int FooterHeight = 60;
            private const int BodyLeft = 18;
            private const int BodyWidth = PopupWidth - 36;

            private readonly Panel _header = new();
            private readonly Label _title = new();
            private readonly RoundedButton _skip = new();
            private readonly Label _body = new();
            private readonly Panel _footer = new();
            private readonly Label _progress = new();
            private readonly RoundedButton _previous = new();
            private readonly RoundedButton _next = new();

            private readonly bool _dark;

            public TutorialPopup(bool dark, Action previous, Action next, Action skip)
            {
                _dark = dark;
                var background = dark ? Color.FromArgb(34, 42, 55) : Color.White;
                var headerColor = dark ? Color.FromArgb(28, 35, 47) : Color.FromArgb(31, 111, 235);
                var foreground = dark ? Color.FromArgb(232, 237, 245) : Color.FromArgb(29, 42, 61);
                var border = dark ? Color.FromArgb(57, 68, 84) : Color.FromArgb(208, 217, 230);

                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                MinimizeBox = false;
                MaximizeBox = false;
                TopMost = true;
                BackColor = border;
                Padding = new Padding(1);
                Font = UiTypography.Body();
                ClientSize = new Size(PopupWidth, 260);
                Text = "Tutorial do Traz Base";

                var surface = new Panel { Dock = DockStyle.Fill, BackColor = background };
                Controls.Add(surface);

                _header.Location = Point.Empty;
                _header.Size = new Size(PopupWidth - 2, HeaderHeight);
                _header.BackColor = headerColor;
                _header.Cursor = Cursors.Arrow;
                surface.Controls.Add(_header);

                _title.ForeColor = Color.White;
                _title.BackColor = Color.Transparent;
                _title.Font = UiTypography.BodySemibold(10f);
                _title.TextAlign = ContentAlignment.MiddleLeft;
                _title.AutoEllipsis = true;
                _title.Location = new Point(16, 0);
                _title.Size = new Size(PopupWidth - 130, HeaderHeight);
                _header.Controls.Add(_title);
                DraggableWindow.Wire(_title, this);

                StyleHeaderButton(_skip, "Pular", new Size(76, 30), headerColor);
                _skip.Location = new Point(_header.Width - 18 - _skip.Width, 8);
                _skip.Click += (_, _) => skip();
                _header.Controls.Add(_skip);

                _body.ForeColor = foreground;
                _body.BackColor = background;
                _body.Font = UiTypography.Body(9.5f);
                _body.AutoSize = true;
                _body.MaximumSize = new Size(BodyWidth, 0);
                surface.Controls.Add(_body);

                _footer.BackColor = background;
                surface.Controls.Add(_footer);

                _progress.ForeColor = dark ? Color.FromArgb(166, 180, 199) : Color.FromArgb(97, 113, 134);
                _progress.Font = UiTypography.Body(9f);
                _progress.AutoSize = true;
                _progress.Location = new Point(18, 19);
                _footer.Controls.Add(_progress);

                StyleActionButton(_previous, "Anterior", background, foreground, border);
                _previous.Size = new Size(104, 34);
                _previous.Click += (_, _) => previous();
                _footer.Controls.Add(_previous);

                StyleActionButton(_next, "Próximo", Color.FromArgb(31, 111, 235), Color.White, Color.FromArgb(31, 111, 235));
                _next.Size = new Size(110, 34);
                _next.Click += (_, _) => next();
                _footer.Controls.Add(_next);

                AcceptButton = _next;
                ActiveControl = _next;
            }

            public void SetStep(TutorialStep step, int current, int total)
            {
                _title.Text = step.Title;
                _body.Text = step.Text;
                _progress.Text = $"Passo {current} de {total}";
                _previous.Enabled = current > 1;
                _next.Text = current >= total ? "Concluir" : "Próximo";
                AcceptButton = _next;

                var bodyHeight = Math.Max(40, _body.PreferredSize.Height + 6);
                var height = HeaderHeight + bodyHeight + 22 + FooterHeight + 2;
                ClientSize = new Size(PopupWidth, height);
                var innerWidth = PopupWidth - 2;

                _header.Size = new Size(innerWidth, HeaderHeight);
                _title.Size = new Size(innerWidth - 110, HeaderHeight);
                _skip.Location = new Point(innerWidth - 18 - _skip.Width, 8);

                _body.Location = new Point(BodyLeft, HeaderHeight + 10);
                _body.Size = new Size(BodyWidth, bodyHeight);

                _footer.Location = new Point(0, HeaderHeight + bodyHeight + 16);
                _footer.Size = new Size(innerWidth, FooterHeight);
                _next.Location = new Point(innerWidth - 18 - _next.Width, 13);
                _previous.Location = new Point(_next.Left - 12 - _previous.Width, 13);
            }

            private static void StyleHeaderButton(RoundedButton button, string text, Size size, Color headerColor)
            {
                button.Text = text;
                button.Size = size;
                button.CornerRadius = 5;
                button.FlatStyle = FlatStyle.Flat;
                button.ForeColor = Color.White;
                button.BackColor = headerColor;
                button.TabStop = false;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 125, 245);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 94, 205);
            }

            private static void StyleActionButton(RoundedButton button, string text, Color back, Color fore, Color border)
            {
                button.Text = text;
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = back;
                button.ForeColor = fore;
                button.Font = UiTypography.BodySemibold(9f);
                button.FlatAppearance.BorderSize = back == Color.FromArgb(31, 111, 235) ? 0 : 1;
                button.FlatAppearance.BorderColor = border;
                button.FlatAppearance.MouseOverBackColor = back == Color.FromArgb(31, 111, 235)
                    ? Color.FromArgb(45, 125, 245)
                    : Color.FromArgb(231, 240, 255);
                button.FlatAppearance.MouseDownBackColor = back == Color.FromArgb(31, 111, 235)
                    ? Color.FromArgb(25, 94, 205)
                    : Color.FromArgb(208, 217, 230);
            }
        }
    }
}
