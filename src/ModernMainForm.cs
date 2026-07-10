using System.Drawing.Drawing2D;
using Windows.Storage.Streams;

namespace GoodRP;

public class ModernMainForm : Form
{
    private readonly MediaWatcher _mediaWatcher;
    private readonly DiscordManager _discordManager;
    private HotkeyManager? _hotkeyManager;
    private NotifyIcon? _trayIcon;

    private const int HOTKEY_SHOW = 1;
    private const int HOTKEY_HIDE = 2;

    private const int SidebarWidth = 210;
    private const int ContentX = 210;
    private const int ContentWidth = 510;

    // Palette
    private static readonly Color Bg = Color.FromArgb(26, 26, 46);          // #1a1a2e
    private static readonly Color SidebarBg = Color.FromArgb(15, 15, 26);    // #0f0f1a
    private static readonly Color PanelBg = Color.FromArgb(22, 22, 38);      // #16162e
    private static readonly Color Accent = Color.FromArgb(88, 101, 242);     // #5865F2
    private static readonly Color TextMuted = Color.FromArgb(185, 187, 190); // #B9BBBE
    private static readonly Color TextDim = Color.FromArgb(114, 118, 125);   // #72767D
    private static readonly Color InputBg = Color.FromArgb(40, 40, 60);
    private static readonly Color Success = Color.LimeGreen;
    private static readonly Color Fail = Color.FromArgb(242, 100, 100);

    // Sidebar
    private readonly Panel _sidebar = new();
    private readonly Label _navStatus = new();
    private readonly Label _navNow = new();
    private Label? _activeNav;

    // Pages
    private readonly Panel _pnlHome = new();
    private readonly Panel _pnlConnections = new();
    private readonly Panel _pnlScripts = new();
    private readonly Panel _pnlLogs = new();
    private readonly Panel _pnlSettings = new();
    private readonly Panel _pnlAbout = new();

    // Home controls
    private readonly PictureBox _picAlbumArt = new();
    private readonly Label _lblTitle = new();
    private readonly Label _lblArtist = new();
    private readonly Label _lblAlbum = new();
    private readonly Label _lblHomeStatus = new();
    private readonly Button _btnShow = new();
    private readonly Button _btnHide = new();

    // Connections controls
    private readonly ListBox _lstAppIds = new();
    private readonly Label _lblConnStatus = new();
    private readonly TextBox _txtNewName = new();
    private readonly TextBox _txtNewId = new();

    // Scripts controls (rows)
    private readonly ToggleSwitch _tglMediaChanged = new();
    private readonly ToggleSwitch _tglMediaStopped = new();
    private readonly ToggleSwitch _tglStateChanged = new();
    private readonly TextBox _txtMediaChanged = new();
    private readonly TextBox _txtMediaStopped = new();
    private readonly TextBox _txtStateChanged = new();
    private readonly TextBox _txtScriptTimeout = new();

    // Logs controls
    private readonly ComboBox _cboLogFilter = new();
    private readonly ListView _lstLogs = new();

    // Settings controls
    private readonly ToggleSwitch _tglAutoShow = new();
    private readonly ToggleSwitch _tglShowArt = new();
    private readonly ToggleSwitch _tglArtFinder = new();
    private readonly ToggleSwitch _tglMcp = new();
    private readonly ToggleSwitch _tglNotify = new();
    private readonly ToggleSwitch _tglHotkeys = new();
    private readonly RadioButton _rbAuto = new();
    private readonly RadioButton _rbListening = new();
    private readonly RadioButton _rbWatching = new();
    private readonly TextBox _txtCloudName = new();
    private readonly TextBox _txtCloudPreset = new();
    private readonly ListBox _lstProviders = new();
    private readonly ListBox _lstAllowed = new();
    private readonly ListBox _lstIgnored = new();
    private readonly TextBox _txtAllowed = new();
    private readonly TextBox _txtIgnored = new();
    private readonly TextBox _txtShowKey = new();
    private readonly TextBox _txtHideKey = new();
    private readonly ComboBox _cboGui = new();

    private MediaInfo? _currentMedia;
    private string? _pendingImageUrl;

    public ModernMainForm(MediaWatcher mediaWatcher, DiscordManager discordManager)
    {
        _mediaWatcher = mediaWatcher;
        _discordManager = discordManager;

        InitializeSidebar();
        InitializeHome();
        InitializeConnections();
        InitializeScripts();
        InitializeLogs();
        InitializeSettings();
        InitializeAbout();
        SetupTrayIcon();
        WireEvents();

        LoadSettings();
        ShowPage(_pnlHome, _navHome);
    }

    #region Initialization

    private void InitializeSidebar()
    {
        Text = "GoodRP 9XT";
        Size = new Size(720, 580);
        MinimumSize = new Size(720, 580);
        MaximumSize = new Size(720, 580);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ForeColor = Color.White;

        _sidebar.BackColor = SidebarBg;
        _sidebar.Location = new Point(0, 0);
        _sidebar.Size = new Size(SidebarWidth, 580);

        var logo = new Label
        {
            Text = "9XT",
            Location = new Point(20, 24),
            Size = new Size(170, 34),
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White
        };
        var sub = new Label
        {
            Text = "by GoodRP",
            Location = new Point(22, 60),
            Size = new Size(170, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = TextDim
        };

        _navHome = MakeNavItem("Home", 110);
        _navConnections = MakeNavItem("Connections", 148);
        _navScripts = MakeNavItem("Scripts", 186);
        _navLogs = MakeNavItem("Logs", 224);
        _navSettings = MakeNavItem("Settings", 262);
        _navAbout = MakeNavItem("About", 300);

        var divider = new Label
        {
            Location = new Point(20, 340),
            Size = new Size(170, 1),
            BackColor = Color.FromArgb(50, 50, 70)
        };

        _navStatus.Text = "Status: Disconnected";
        _navStatus.Location = new Point(20, 360);
        _navStatus.Size = new Size(170, 18);
        _navStatus.Font = new Font("Segoe UI", 9);
        _navStatus.ForeColor = Fail;

        _navNow.Text = "Now: —";
        _navNow.Location = new Point(20, 382);
        _navNow.Size = new Size(170, 30);
        _navNow.Font = new Font("Segoe UI", 9);
        _navNow.ForeColor = TextMuted;

        _sidebar.Controls.AddRange(new Control[] { logo, sub, _navHome, _navConnections, _navScripts, _navLogs, _navSettings, _navAbout, divider, _navStatus, _navNow });

        Controls.Add(_sidebar);
    }

    private Label _navHome = new();
    private Label _navConnections = new();
    private Label _navScripts = new();
    private Label _navLogs = new();
    private Label _navSettings = new();
    private Label _navAbout = new();

    private Label MakeNavItem(string text, int y)
    {
        var nav = new Label
        {
            Text = text,
            Location = new Point(20, y),
            Size = new Size(170, 30),
            Font = new Font("Segoe UI", 11),
            ForeColor = TextMuted,
            Padding = new Padding(12, 0, 0, 0),
            Tag = null
        };
        nav.MouseEnter += (s, e) => { if (nav != _activeNav) nav.ForeColor = Color.White; };
        nav.MouseLeave += (s, e) => { if (nav != _activeNav) nav.ForeColor = TextMuted; };
        nav.Click += (s, e) =>
        {
            var page = nav == _navHome ? _pnlHome
                : nav == _navConnections ? _pnlConnections
                : nav == _navScripts ? _pnlScripts
                : nav == _navLogs ? _pnlLogs
                : nav == _navSettings ? _pnlSettings
                : _pnlAbout;
            ShowPage(page, nav);
        };
        return nav;
    }

    private void ShowPage(Panel page, Label nav)
    {
        _pnlHome.Visible = _pnlConnections.Visible = _pnlScripts.Visible =
            _pnlLogs.Visible = _pnlSettings.Visible = _pnlAbout.Visible = false;
        page.Visible = true;

        if (_activeNav != null)
        {
            _activeNav.ForeColor = TextMuted;
            _activeNav.Invalidate();
        }
        _activeNav = nav;
        nav.ForeColor = Color.White;
        nav.Invalidate();

        if (page == _pnlConnections) RefreshAppIdList();
        if (page == _pnlSettings) RefreshProviderList();
        if (page == _pnlLogs) RefreshLogs();
    }

    #endregion

    #region Home Page

    private void InitializeHome()
    {
        _pnlHome.Location = new Point(ContentX, 0);
        _pnlHome.Size = new Size(ContentWidth, 580);
        _pnlHome.BackColor = Bg;

        _picAlbumArt.Location = new Point(30, 40);
        _picAlbumArt.Size = new Size(140, 140);
        _picAlbumArt.SizeMode = PictureBoxSizeMode.Zoom;
        _picAlbumArt.BackColor = PanelBg;
        _picAlbumArt.Paint += (s, e) =>
        {
            using var path = RoundedRect(0, 0, _picAlbumArt.Width, _picAlbumArt.Height, 12);
            _picAlbumArt.Region = new Region(path);
        };

        _lblTitle.Text = "No media playing";
        _lblTitle.Location = new Point(200, 50);
        _lblTitle.Size = new Size(260, 30);
        _lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        _lblTitle.ForeColor = Color.White;

        _lblArtist.Text = "";
        _lblArtist.Location = new Point(200, 88);
        _lblArtist.Size = new Size(260, 22);
        _lblArtist.Font = new Font("Segoe UI", 11);
        _lblArtist.ForeColor = TextMuted;

        _lblAlbum.Text = "";
        _lblAlbum.Location = new Point(200, 112);
        _lblAlbum.Size = new Size(260, 22);
        _lblAlbum.Font = new Font("Segoe UI", 10);
        _lblAlbum.ForeColor = TextDim;

        _lblHomeStatus.Text = "Discord RP: Not showing";
        _lblHomeStatus.Location = new Point(200, 150);
        _lblHomeStatus.Size = new Size(260, 20);
        _lblHomeStatus.Font = new Font("Segoe UI", 9);
        _lblHomeStatus.ForeColor = TextDim;

        _btnShow.Text = "Show on Discord";
        _btnShow.Location = new Point(30, 210);
        _btnShow.Size = new Size(150, 38);
        MakeButton(_btnShow, Accent);
        _btnShow.Click += BtnShow_Click;

        _btnHide.Text = "Hide";
        _btnHide.Location = new Point(195, 210);
        _btnHide.Size = new Size(150, 38);
        MakeButton(_btnHide, Color.FromArgb(80, 80, 95));
        _btnHide.Click += BtnHide_Click;

        var hint = new Label
        {
            Text = "Tip: switch GUIs in config.json (\"GuiVersion\": \"9xt\").",
            Location = new Point(30, 280),
            Size = new Size(440, 20),
            Font = new Font("Segoe UI", 9),
            ForeColor = TextDim
        };

        _pnlHome.Controls.AddRange(new Control[] { _picAlbumArt, _lblTitle, _lblArtist, _lblAlbum, _lblHomeStatus, _btnShow, _btnHide, hint });
        Controls.Add(_pnlHome);
    }

    #endregion

    #region Connections Page

    private void InitializeConnections()
    {
        _pnlConnections.Location = new Point(ContentX, 0);
        _pnlConnections.Size = new Size(ContentWidth, 580);
        _pnlConnections.BackColor = Bg;
        _pnlConnections.AutoScroll = true;
        _pnlConnections.Visible = false;

        int y = 24;
        var header = SectionHeader("Discord Application IDs", y);
        y += 36;

        _lstAppIds.Location = new Point(20, y);
        _lstAppIds.Size = new Size(460, 150);
        _lstAppIds.BackColor = InputBg;
        _lstAppIds.ForeColor = Color.White;
        _lstAppIds.BorderStyle = BorderStyle.None;
        _lstAppIds.DrawMode = DrawMode.OwnerDrawFixed;
        _lstAppIds.DrawItem += (s, e) =>
        {
            e.DrawBackground();
            if (e.Index < 0) return;
            var item = (DiscordAppEntry)_lstAppIds.Items[e.Index];
            var active = e.Index == ConfigManager.Config.ActiveAppIdIndex;
            var text = $"{(active ? "● " : "  ")}{item.Name}  —  {item.Id}";
            using var brush = new SolidBrush(active ? Color.White : TextMuted);
            e.Graphics.DrawString(text, new Font("Segoe UI", 10), brush, e.Bounds.Left + 4, e.Bounds.Top + 4);
        };
        y += 160;

        var btnAdd = new Button { Text = "Add", Location = new Point(20, y), Size = new Size(90, 32) };
        MakeButton(btnAdd, Accent);
        btnAdd.Click += (s, e) => AddAppId();

        var btnRemove = new Button { Text = "Remove", Location = new Point(120, y), Size = new Size(90, 32) };
        MakeButton(btnRemove, Color.FromArgb(80, 80, 95));
        btnRemove.Click += (s, e) => RemoveAppId();

        var btnConnect = new Button { Text = "Connect", Location = new Point(220, y), Size = new Size(110, 32) };
        MakeButton(btnConnect, Accent);
        btnConnect.Click += (s, e) => ConnectSelectedAppId();

        var btnSetActive = new Button { Text = "Set Active", Location = new Point(340, y), Size = new Size(140, 32) };
        MakeButton(btnSetActive, Color.FromArgb(80, 80, 95));
        btnSetActive.Click += (s, e) => SetActiveAppId();
        y += 44;

        var addHeader = SectionHeader("Add new App ID", y);
        y += 30;
        _txtNewName.Location = new Point(20, y);
        _txtNewName.Size = new Size(220, 26);
        MakeTextBox(_txtNewName, "Name (e.g. Spotify RP)");
        _txtNewId.Location = new Point(250, y);
        _txtNewId.Size = new Size(230, 26);
        MakeTextBox(_txtNewId, "Discord App ID");
        y += 40;

        _lblConnStatus.Text = "Status: Disconnected";
        _lblConnStatus.Location = new Point(20, y);
        _lblConnStatus.Size = new Size(460, 20);
        _lblConnStatus.Font = new Font("Segoe UI", 10);
        _lblConnStatus.ForeColor = Fail;

        _pnlConnections.Controls.AddRange(new Control[] { header, _lstAppIds, btnAdd, btnRemove, btnConnect, btnSetActive, addHeader, _txtNewName, _txtNewId, _lblConnStatus });
        Controls.Add(_pnlConnections);
    }

    private void RefreshAppIdList()
    {
        _lstAppIds.Items.Clear();
        foreach (var entry in ConfigManager.Config.DiscordAppIds)
            _lstAppIds.Items.Add(entry);

        if (ConfigManager.Config.DiscordAppIds.Count == 0)
            _lblConnStatus.Text = "No App IDs saved. Add one below.";
        else if (_discordManager.IsConnected)
            _lblConnStatus.Text = $"Status: Connected as {ConfigManager.Config.DiscordAppIds[Math.Max(0, Math.Min(ConfigManager.Config.ActiveAppIdIndex, ConfigManager.Config.DiscordAppIds.Count - 1))].Name}";
        else
            _lblConnStatus.Text = "Status: Disconnected";
    }

    private void AddAppId()
    {
        var name = _txtNewName.Text.Trim();
        var id = _txtNewId.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
        {
            MessageBox.Show("Enter both a name and an App ID.", "9XT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        ConfigManager.Config.DiscordAppIds.Add(new DiscordAppEntry { Name = name, Id = id });
        ConfigManager.Save();
        _txtNewName.Text = "";
        _txtNewId.Text = "";
        RefreshAppIdList();
        LogService.Log("Discord", $"Added App ID '{name}'");
    }

    private void RemoveAppId()
    {
        if (_lstAppIds.SelectedIndex < 0) return;
        var idx = _lstAppIds.SelectedIndex;
        ConfigManager.Config.DiscordAppIds.RemoveAt(idx);
        if (ConfigManager.Config.ActiveAppIdIndex >= ConfigManager.Config.DiscordAppIds.Count)
            ConfigManager.Config.ActiveAppIdIndex = Math.Max(0, ConfigManager.Config.DiscordAppIds.Count - 1);
        ConfigManager.Save();
        RefreshAppIdList();
    }

    private void ConnectSelectedAppId()
    {
        if (_lstAppIds.SelectedIndex < 0)
        {
            MessageBox.Show("Select an App ID first.", "9XT", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var entry = ConfigManager.Config.DiscordAppIds[_lstAppIds.SelectedIndex];
        ConfigManager.Config.ActiveAppIdIndex = _lstAppIds.SelectedIndex;
        ConfigManager.Config.DiscordClientId = entry.Id;
        ConfigManager.Save();

        _discordManager.Disconnect();
        _discordManager.Connect(entry.Id);
        RefreshAppIdList();
        LogService.Log("Discord", $"Connected to '{entry.Name}'");
    }

    private void SetActiveAppId()
    {
        if (_lstAppIds.SelectedIndex < 0) return;
        ConfigManager.Config.ActiveAppIdIndex = _lstAppIds.SelectedIndex;
        ConfigManager.Config.DiscordClientId = ConfigManager.Config.DiscordAppIds[_lstAppIds.SelectedIndex].Id;
        ConfigManager.Save();
        RefreshAppIdList();
    }

    #endregion

    #region Scripts Page

    private void InitializeScripts()
    {
        _pnlScripts.Location = new Point(ContentX, 0);
        _pnlScripts.Size = new Size(ContentWidth, 580);
        _pnlScripts.BackColor = Bg;
        _pnlScripts.AutoScroll = true;
        _pnlScripts.Visible = false;

        int y = 24;
        var header = SectionHeader("Hooks / Scripts", y);
        y += 36;

        y = AddScriptRow(y, "On Media Changed", _tglMediaChanged, _txtMediaChanged, ConfigManager.Config.OnMediaChangedScript);
        y = AddScriptRow(y, "On Media Stopped", _tglMediaStopped, _txtMediaStopped, ConfigManager.Config.OnMediaStoppedScript);
        y = AddScriptRow(y, "On Playback State Changed", _tglStateChanged, _txtStateChanged, ConfigManager.Config.OnPlaybackStateChangedScript);

        y += 10;
        var lblTimeout = new Label { Text = "Script timeout (ms):", Location = new Point(20, y), Size = new Size(140, 26), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtScriptTimeout.Location = new Point(170, y);
        _txtScriptTimeout.Size = new Size(120, 26);
        MakeTextBox(_txtScriptTimeout, "10000");
        y += 40;

        var note = new Label
        {
            Text = "Supports .ps1, .py, .bat, .js and the native .grp format.\nToggle a hook off to disable it without removing the path.",
            Location = new Point(20, y),
            Size = new Size(460, 40),
            Font = new Font("Segoe UI", 9),
            ForeColor = TextDim
        };

        _pnlScripts.Controls.AddRange(new Control[] { header, lblTimeout, _txtScriptTimeout, note });
        Controls.Add(_pnlScripts);
    }

    private int AddScriptRow(int y, string label, ToggleSwitch toggle, TextBox pathBox, string? current)
    {
        var lbl = new Label { Text = label, Location = new Point(20, y), Size = new Size(200, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        toggle.Location = new Point(20, y + 28);
        pathBox.Location = new Point(80, y + 26);
        pathBox.Size = new Size(280, 26);
        MakeTextBox(pathBox, "Path to script...");
        if (!string.IsNullOrWhiteSpace(current)) pathBox.Text = current;

        var btn = new Button { Text = "Browse", Location = new Point(370, y + 25), Size = new Size(90, 28) };
        MakeButton(btn, Color.FromArgb(80, 80, 95));
        var capturedBox = pathBox;
        btn.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "All scripts|*.ps1;*.py;*.bat;*.js;*.grp|PowerShell|*.ps1|Python|*.py|Batch|*.bat|JavaScript|*.js|GoodRP Script|*.grp",
                Title = "Select script"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                capturedBox.Text = dlg.FileName;
        };

        _pnlScripts.Controls.AddRange(new Control[] { lbl, toggle, pathBox, btn });
        return y + 70;
    }

    #endregion

    #region Logs Page

    private void InitializeLogs()
    {
        _pnlLogs.Location = new Point(ContentX, 0);
        _pnlLogs.Size = new Size(ContentWidth, 580);
        _pnlLogs.BackColor = Bg;
        _pnlLogs.Visible = false;

        var header = SectionHeader("Activity Log", 20);

        _cboLogFilter.Location = new Point(20, 56);
        _cboLogFilter.Size = new Size(140, 26);
        _cboLogFilter.BackColor = InputBg;
        _cboLogFilter.ForeColor = Color.White;
        _cboLogFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboLogFilter.Items.AddRange(new object[] { "All", "Media", "Discord", "Script", "Error" });
        _cboLogFilter.SelectedIndex = 0;
        _cboLogFilter.SelectedIndexChanged += (s, e) => RefreshLogs();

        var btnClear = new Button { Text = "Clear", Location = new Point(380, 54), Size = new Size(100, 28) };
        MakeButton(btnClear, Color.FromArgb(80, 80, 95));
        btnClear.Click += (s, e) =>
        {
            LogService.Clear();
            RefreshLogs();
        };

        _lstLogs.Location = new Point(20, 92);
        _lstLogs.Size = new Size(460, 450);
        _lstLogs.View = View.Details;
        _lstLogs.FullRowSelect = true;
        _lstLogs.BackColor = InputBg;
        _lstLogs.ForeColor = Color.White;
        _lstLogs.Columns.Add("Time", 70);
        _lstLogs.Columns.Add("Type", 70);
        _lstLogs.Columns.Add("Message", 250);
        _lstLogs.Columns.Add("Status", 60);

        _pnlLogs.Controls.AddRange(new Control[] { header, _cboLogFilter, btnClear, _lstLogs });
        Controls.Add(_pnlLogs);
    }

    private void RefreshLogs()
    {
        var filter = _cboLogFilter.SelectedItem?.ToString();
        var entries = LogService.Filter(filter);
        _lstLogs.Items.Clear();
        foreach (var e in entries)
        {
            var item = new ListViewItem(e.Timestamp.ToString("HH:mm:ss"));
            item.SubItems.Add(e.Type);
            item.SubItems.Add(e.Message.Length > 60 ? e.Message[..57] + "..." : e.Message);
            item.SubItems.Add(e.Success ? "OK" : "FAIL");
            item.ForeColor = e.Success ? Color.White : Fail;
            _lstLogs.Items.Add(item);
        }
    }

    private void OnLogEntry(LogEntry entry)
    {
        if (InvokeRequired) { Invoke(() => OnLogEntry(entry)); return; }
        if (!_pnlLogs.Visible) return;
        RefreshLogs();
    }

    #endregion

    #region Settings Page

    private void InitializeSettings()
    {
        _pnlSettings.Location = new Point(ContentX, 0);
        _pnlSettings.Size = new Size(ContentWidth, 580);
        _pnlSettings.BackColor = Bg;
        _pnlSettings.AutoScroll = true;
        _pnlSettings.Visible = false;

        int y = 20;

        // GUI switcher
        var lblGui = new Label { Text = "GUI version:", Location = new Point(20, y), Size = new Size(120, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _cboGui.Location = new Point(140, y);
        _cboGui.Size = new Size(200, 26);
        _cboGui.BackColor = InputBg;
        _cboGui.ForeColor = Color.White;
        _cboGui.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboGui.Items.AddRange(new object[] { "Classic", "9XT (Modern)" });
        _cboGui.SelectedIndexChanged += CboGui_SelectedIndexChanged;
        y += 36;

        // Discord section
        var hDiscord = SectionHeader("Discord", y); y += 30;

        var lblActivity = new Label { Text = "Activity type:", Location = new Point(20, y), Size = new Size(110, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _rbAuto.Text = "Auto"; _rbListening.Text = "Listening"; _rbWatching.Text = "Watching";
        _rbAuto.Location = new Point(130, y); _rbListening.Location = new Point(190, y); _rbWatching.Location = new Point(280, y);
        StyleRadio(_rbAuto); StyleRadio(_rbListening); StyleRadio(_rbWatching);
        y += 36;

        var lblAuto = new Label { Text = "Auto-show on Discord", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglAutoShow.Location = new Point(400, y);
        y += 36;

        var lblMcp = new Label { Text = "Enable MCP Server (AI agents)", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglMcp.Location = new Point(400, y);
        y += 44;

        // Album Art
        var hArt = SectionHeader("Album Art", y); y += 30;
        var lblArt = new Label { Text = "Show album art", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglShowArt.Location = new Point(400, y); y += 36;
        var lblFinder = new Label { Text = "Art finder fallback (Deezer/iTunes)", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglArtFinder.Location = new Point(400, y); y += 44;

        // Notifications & Hotkeys
        var hNotif = SectionHeader("Notifications & Hotkeys", y); y += 30;
        var lblNotify = new Label { Text = "Show balloon notification", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglNotify.Location = new Point(400, y); y += 36;
        var lblHot = new Label { Text = "Enable global hotkeys", Location = new Point(20, y), Size = new Size(260, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _tglHotkeys.Location = new Point(400, y); y += 36;
        var lblShowK = new Label { Text = "Show hotkey:", Location = new Point(20, y), Size = new Size(120, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtShowKey.Location = new Point(140, y); _txtShowKey.Size = new Size(150, 24); MakeTextBox(_txtShowKey, "Ctrl+Shift+G");
        var lblHideK = new Label { Text = "Hide hotkey:", Location = new Point(300, y), Size = new Size(120, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtHideKey.Location = new Point(410, y); _txtHideKey.Size = new Size(150, 24); MakeTextBox(_txtHideKey, "Ctrl+Shift+H");
        y += 44;

        // Image providers
        var hProv = SectionHeader("Image Upload Providers (order)", y); y += 30;
        _lstProviders.Location = new Point(20, y); _lstProviders.Size = new Size(300, 90);
        _lstProviders.BackColor = InputBg; _lstProviders.ForeColor = Color.White; _lstProviders.BorderStyle = BorderStyle.None;
        _lstProviders.AllowDrop = true;
        SetupProviderDragDrop();
        y += 100;
        var lblCloudName = new Label { Text = "Cloudinary Cloud Name:", Location = new Point(20, y), Size = new Size(200, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtCloudName.Location = new Point(220, y); _txtCloudName.Size = new Size(240, 24); MakeTextBox(_txtCloudName, "cloud name"); y += 32;
        var lblCloudPreset = new Label { Text = "Cloudinary Upload Preset:", Location = new Point(20, y), Size = new Size(200, 24), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtCloudPreset.Location = new Point(220, y); _txtCloudPreset.Size = new Size(240, 24); MakeTextBox(_txtCloudPreset, "preset"); y += 44;

        // App filtering
        var hApps = SectionHeader("App Filtering", y); y += 30;
        var upAllowed = new Button { Text = "↑", Location = new Point(20, y), Size = new Size(30, 26) }; MakeButton(upAllowed, Color.FromArgb(80, 80, 95));
        var lblAllowed = new Label { Text = "Allowed apps", Location = new Point(56, y + 4), Size = new Size(150, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtAllowed.Location = new Point(220, y); _txtAllowed.Size = new Size(180, 24); MakeTextBox(_txtAllowed, "app name");
        var btnAddAllowed = new Button { Text = "+", Location = new Point(410, y), Size = new Size(30, 26) }; MakeButton(btnAddAllowed, Accent);
        y += 32;
        _lstAllowed.Location = new Point(20, y); _lstAllowed.Size = new Size(420, 70); _lstAllowed.BackColor = InputBg; _lstAllowed.ForeColor = Color.White; _lstAllowed.BorderStyle = BorderStyle.None;
        var btnDelAllowed = new Button { Text = "Remove", Location = new Point(350, y + 76), Size = new Size(90, 26) }; MakeButton(btnDelAllowed, Color.FromArgb(80, 80, 95));
        y += 110;

        var lblIgnored = new Label { Text = "Ignored apps", Location = new Point(20, y + 4), Size = new Size(150, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 10) };
        _txtIgnored.Location = new Point(220, y); _txtIgnored.Size = new Size(180, 24); MakeTextBox(_txtIgnored, "app name");
        var btnAddIgnored = new Button { Text = "+", Location = new Point(410, y), Size = new Size(30, 26) }; MakeButton(btnAddIgnored, Accent);
        y += 32;
        _lstIgnored.Location = new Point(20, y); _lstIgnored.Size = new Size(420, 70); _lstIgnored.BackColor = InputBg; _lstIgnored.ForeColor = Color.White; _lstIgnored.BorderStyle = BorderStyle.None;
        var btnDelIgnored = new Button { Text = "Remove", Location = new Point(350, y + 76), Size = new Size(90, 26) }; MakeButton(btnDelIgnored, Color.FromArgb(80, 80, 95));

        btnAddAllowed.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(_txtAllowed.Text)) { ConfigManager.Config.AllowedApps.Add(_txtAllowed.Text.Trim()); _txtAllowed.Text = ""; RefreshAppLists(); SaveSettings(); }
        };
        btnDelAllowed.Click += (s, e) => { if (_lstAllowed.SelectedIndex >= 0) { ConfigManager.Config.AllowedApps.RemoveAt(_lstAllowed.SelectedIndex); RefreshAppLists(); SaveSettings(); } };
        btnAddIgnored.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(_txtIgnored.Text)) { ConfigManager.Config.IgnoredApps.Add(_txtIgnored.Text.Trim()); _txtIgnored.Text = ""; RefreshAppLists(); SaveSettings(); }
        };
        btnDelIgnored.Click += (s, e) => { if (_lstIgnored.SelectedIndex >= 0) { ConfigManager.Config.IgnoredApps.RemoveAt(_lstIgnored.SelectedIndex); RefreshAppLists(); SaveSettings(); } };

        _pnlSettings.Controls.AddRange(new Control[] {
            hDiscord, lblActivity, _rbAuto, _rbListening, _rbWatching, lblAuto, _tglAutoShow, lblMcp, _tglMcp,
            hArt, lblArt, _tglShowArt, lblFinder, _tglArtFinder,
            hNotif, lblNotify, _tglNotify, lblHot, _tglHotkeys, lblShowK, _txtShowKey, lblHideK, _txtHideKey,
            hProv, _lstProviders, lblCloudName, _txtCloudName, lblCloudPreset, _txtCloudPreset,
            hApps, upAllowed, lblAllowed, _txtAllowed, btnAddAllowed, _lstAllowed, btnDelAllowed,
            lblIgnored, _txtIgnored, btnAddIgnored, _lstIgnored, btnDelIgnored,
            lblGui, _cboGui
        });
        Controls.Add(_pnlSettings);
    }

    private void SetupProviderDragDrop()
    {
        _lstProviders.MouseDown += (s, e) =>
        {
            if (_lstProviders.SelectedIndex >= 0)
                _lstProviders.DoDragDrop(_lstProviders.SelectedIndex, DragDropEffects.Move);
        };
        _lstProviders.DragOver += (s, e) =>
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(int)))
                e.Effect = DragDropEffects.Move;
        };
        _lstProviders.DragDrop += (s, e) =>
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(int))) return;
            var src = (int)e.Data.GetData(typeof(int))!;
            var dest = _lstProviders.IndexFromPoint(e.X, e.Y);
            if (dest < 0) dest = _lstProviders.Items.Count - 1;
            var providers = ConfigManager.Config.ImageProviders;
            if (src < 0 || src >= providers.Count || dest < 0 || dest >= providers.Count) return;
            var item = providers[src];
            providers.RemoveAt(src);
            providers.Insert(dest, item);
            ConfigManager.Save();
            RefreshProviderList();
        };
    }

    private void RefreshProviderList()
    {
        _lstProviders.Items.Clear();
        foreach (var p in ConfigManager.Config.ImageProviders)
            _lstProviders.Items.Add(p);
    }

    private void RefreshAppLists()
    {
        _lstAllowed.Items.Clear();
        _lstIgnored.Items.Clear();
        foreach (var a in ConfigManager.Config.AllowedApps) _lstAllowed.Items.Add(a);
        foreach (var i in ConfigManager.Config.IgnoredApps) _lstIgnored.Items.Add(i);
    }

    #endregion

    #region About Page

    private void InitializeAbout()
    {
        _pnlAbout.Location = new Point(ContentX, 0);
        _pnlAbout.Size = new Size(ContentWidth, 580);
        _pnlAbout.BackColor = Bg;
        _pnlAbout.Visible = false;

        var title = new Label { Text = "9XT", Location = new Point(30, 40), Size = new Size(400, 50), Font = new Font("Segoe UI", 32, FontStyle.Bold), ForeColor = Color.White };
        var subtitle = new Label { Text = "Modern control panel for GoodRP", Location = new Point(32, 96), Size = new Size(400, 24), Font = new Font("Segoe UI", 12), ForeColor = TextMuted };

        var version = new Label { Text = $"GoodRP {GetType().Assembly.GetName().Version?.ToString() ?? "1.0"}", Location = new Point(32, 150), Size = new Size(400, 22), Font = new Font("Segoe UI", 11), ForeColor = TextDim };
        var info = new Label
        {
            Text = "This is an alternative GUI for GoodRP.\nSwitch back to the classic GUI by setting\n\"GuiVersion\": \"default\" in %AppData%\\GoodRP\\config.json\n\nFeatures: multiple Discord App IDs, filterable activity log,\nhooks table, drag-reorder image providers, app filtering.",
            Location = new Point(32, 190),
            Size = new Size(440, 160),
            Font = new Font("Segoe UI", 10),
            ForeColor = TextMuted
        };

        _pnlAbout.Controls.AddRange(new Control[] { title, subtitle, version, info });
        Controls.Add(_pnlAbout);
    }

    #endregion

    #region Helpers

    private static Label SectionHeader(string text, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(20, y),
            Size = new Size(460, 24),
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White
        };
    }

    private static void MakeButton(Button btn, Color bg)
    {
        btn.BackColor = bg;
        btn.ForeColor = Color.White;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Segoe UI", 10);
        btn.Cursor = Cursors.Hand;
    }

    private static void MakeTextBox(TextBox tb, string placeholder)
    {
        tb.BackColor = InputBg;
        tb.ForeColor = Color.White;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.PlaceholderText = placeholder;
    }

    private static void StyleRadio(RadioButton rb)
    {
        rb.ForeColor = TextMuted;
        rb.Font = new Font("Segoe UI", 10);
        rb.AutoSize = true;
    }

    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Icon CreateAppIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Accent);
        g.FillRoundedRectangle(brush, 0, 0, 32, 32, 6);
        using var font = new Font("Arial", 14, FontStyle.Bold);
        g.DrawString("G", font, Brushes.White, 7, 4);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static bool IsDiscordCdnUrl(string url) =>
        url.Contains("discordapp.com") || url.Contains("discordapp.net");

    #endregion

    #region Tray / Events / Media

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "GoodRP 9XT",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _trayIcon.ContextMenuStrip.Items.Add("Open", null, (s, e) => ShowForm());
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add("Quit", null, (s, e) => Application.Exit());
        _trayIcon.DoubleClick += (s, e) => ShowForm();
        _trayIcon.BalloonTipClicked += (s, e) => ShowForm();
    }

    private void ShowForm()
    {
        if (InvokeRequired) { Invoke(ShowForm); return; }
        Visible = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        TopMost = true;
        TopMost = false;
    }

    private void WireEvents()
    {
        _mediaWatcher.MediaChanged += OnMediaChanged;
        _mediaWatcher.MediaStopped += OnMediaStopped;
        _mediaWatcher.TimelineChanged += OnTimelineChanged;
        _mediaWatcher.PlaybackStateChanged += OnPlaybackStateChanged;
        _discordManager.StatusChanged += OnDiscordStatusChanged;
        _discordManager.PresenceUpdated += OnPresenceUpdated;
        LogService.EntryAdded += OnLogEntry;

        _rbAuto.CheckedChanged += OnActivityTypeChanged;
        _rbListening.CheckedChanged += OnActivityTypeChanged;
        _rbWatching.CheckedChanged += OnActivityTypeChanged;

        _tglAutoShow.OnToggle += (v) => { ConfigManager.Config.AutoShowOnDiscord = v; ConfigManager.Save(); };
        _tglShowArt.OnToggle += (v) => { ConfigManager.Config.ShowAlbumArt = v; ConfigManager.Save(); };
        _tglArtFinder.OnToggle += (v) => { ConfigManager.Config.EnableArtFinder = v; ConfigManager.Save(); };
        _tglMcp.OnToggle += (v) => { ConfigManager.Config.McpServerEnabled = v; ConfigManager.Save(); };
        _tglNotify.OnToggle += (v) => { ConfigManager.Config.UseNotifications = v; ConfigManager.Save(); };
        _tglHotkeys.OnToggle += (v) => { ConfigManager.Config.UseHotkeys = v; ConfigManager.Save(); RegisterHotkeys(); };
    }

    private void LoadSettings()
    {
        _tglAutoShow.Checked = ConfigManager.Config.AutoShowOnDiscord;
        _tglShowArt.Checked = ConfigManager.Config.ShowAlbumArt;
        _tglArtFinder.Checked = ConfigManager.Config.EnableArtFinder;
        _tglMcp.Checked = ConfigManager.Config.McpServerEnabled;
        _tglNotify.Checked = ConfigManager.Config.UseNotifications;
        _tglHotkeys.Checked = ConfigManager.Config.UseHotkeys;

        var ov = ConfigManager.Config.ActivityTypeOverride;
        _rbAuto.Checked = ov == "Auto";
        _rbListening.Checked = ov == "Listening";
        _rbWatching.Checked = ov == "Watching";

        _txtCloudName.Text = ConfigManager.Config.CloudinaryCloudName;
        _txtCloudPreset.Text = ConfigManager.Config.CloudinaryUploadPreset;
        _txtShowKey.Text = ConfigManager.Config.ShowHotkey;
        _txtHideKey.Text = ConfigManager.Config.HideHotkey;
        _txtScriptTimeout.Text = ConfigManager.Config.ScriptTimeoutMs.ToString();
        _cboGui.SelectedIndex = string.Equals(ConfigManager.Config.GuiVersion, "9xt", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        _txtMediaChanged.Text = ConfigManager.Config.OnMediaChangedScript ?? "";
        _txtMediaStopped.Text = ConfigManager.Config.OnMediaStoppedScript ?? "";
        _txtStateChanged.Text = ConfigManager.Config.OnPlaybackStateChangedScript ?? "";
        _tglMediaChanged.Checked = !string.IsNullOrWhiteSpace(ConfigManager.Config.OnMediaChangedScript);
        _tglMediaStopped.Checked = !string.IsNullOrWhiteSpace(ConfigManager.Config.OnMediaStoppedScript);
        _tglStateChanged.Checked = !string.IsNullOrWhiteSpace(ConfigManager.Config.OnPlaybackStateChangedScript);

        RefreshProviderList();
        RefreshAppLists();
    }

    private void SaveSettings()
    {
        ConfigManager.Config.AutoShowOnDiscord = _tglAutoShow.Checked;
        ConfigManager.Config.ShowAlbumArt = _tglShowArt.Checked;
        ConfigManager.Config.EnableArtFinder = _tglArtFinder.Checked;
        ConfigManager.Config.McpServerEnabled = _tglMcp.Checked;
        ConfigManager.Config.UseNotifications = _tglNotify.Checked;
        ConfigManager.Config.UseHotkeys = _tglHotkeys.Checked;
        ConfigManager.Config.ActivityTypeOverride = _rbAuto.Checked ? "Auto" : _rbListening.Checked ? "Listening" : "Watching";
        ConfigManager.Config.CloudinaryCloudName = _txtCloudName.Text.Trim();
        ConfigManager.Config.CloudinaryUploadPreset = _txtCloudPreset.Text.Trim();
        ConfigManager.Config.ShowHotkey = _txtShowKey.Text.Trim();
        ConfigManager.Config.HideHotkey = _txtHideKey.Text.Trim();

        if (int.TryParse(_txtScriptTimeout.Text.Trim(), out var t) && t > 0)
            ConfigManager.Config.ScriptTimeoutMs = t;

        ConfigManager.Config.OnMediaChangedScript = _tglMediaChanged.Checked ? _txtMediaChanged.Text.Trim() : "";
        ConfigManager.Config.OnMediaStoppedScript = _tglMediaStopped.Checked ? _txtMediaStopped.Text.Trim() : "";
        ConfigManager.Config.OnPlaybackStateChangedScript = _tglStateChanged.Checked ? _txtStateChanged.Text.Trim() : "";

        ConfigManager.Save();
    }

    private void BtnShow_Click(object? sender, EventArgs e)
    {
        if (_currentMedia != null)
            _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
    }

    private void BtnHide_Click(object? sender, EventArgs e)
    {
        _discordManager.ClearPresence();
        _lblHomeStatus.Text = "Discord RP: Not showing";
    }

    private void OnActivityTypeChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton rb && rb.Checked)
        {
            ConfigManager.Config.ActivityTypeOverride = rb == _rbAuto ? "Auto" : rb == _rbListening ? "Listening" : "Watching";
            ConfigManager.Save();
            _discordManager.RefreshPresence();
        }
    }

    private void CboGui_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var newVersion = _cboGui.SelectedIndex == 1 ? "9xt" : "default";
        if (string.Equals(newVersion, ConfigManager.Config.GuiVersion, StringComparison.OrdinalIgnoreCase))
            return;

        ConfigManager.Config.GuiVersion = newVersion;
        ConfigManager.Save();

        MessageBox.Show(
            "GUI preference saved. Restart GoodRP (close and reopen) to switch to the " +
            (newVersion == "9xt" ? "9XT modern" : "classic") + " interface.",
            "9XT",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private async void OnMediaChanged(MediaInfo media)
    {
        _currentMedia = media;
        UpdateMediaDisplay(media);
        LogService.Log("Media", $"Playing: {media.CleanTitle} — {media.Artist}");

        string? imageUrl = null;
        if (ConfigManager.Config.ShowAlbumArt)
        {
            var cacheKey = $"{media.Title}_{media.Artist}";
            if (ConfigManager.Config.EnableArtFinder)
                imageUrl = await ArtFinderService.FindArtAsync(media.Title, media.Artist, media.Album);
            imageUrl ??= await ImageUploader.UploadThumbnailAsync(media.Thumbnail, cacheKey);
            if (imageUrl != null && IsDiscordCdnUrl(imageUrl))
                imageUrl = null;
        }
        _pendingImageUrl = imageUrl;

        if (ConfigManager.Config.AutoShowOnDiscord)
            _discordManager.SetPresence(media, imageUrl);
        else if (ConfigManager.Config.UseNotifications)
            ShowTrayNotification(media.CleanTitle, media.Artist);

        await LoadLocalThumbnailAsync(media.Thumbnail);
        if (!string.IsNullOrEmpty(imageUrl) && imageUrl!.StartsWith("http"))
            _ = LoadAlbumArtIntoGuiAsync(imageUrl);

        if (_tglMediaChanged.Checked && !string.IsNullOrWhiteSpace(_txtMediaChanged.Text))
        {
            LogService.Log("Script", $"Running: {Path.GetFileName(_txtMediaChanged.Text)}");
            ScriptingService.RunScript(_txtMediaChanged.Text.Trim(), media, imageUrl: imageUrl);
        }
    }

    private async Task LoadLocalThumbnailAsync(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail == null) return;
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            using var memStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memStream);
            memStream.Position = 0;
            Image img;
            using (var ms = new MemoryStream(memStream.ToArray()))
                img = Image.FromStream(ms);
            var cloned = new Bitmap(img);
            img.Dispose();
            if (InvokeRequired) Invoke(() => SetAlbumArt(cloned));
            else SetAlbumArt(cloned);
        }
        catch { /* ignore */ }
    }

    private async Task LoadAlbumArtIntoGuiAsync(string url)
    {
        try
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(url);
            Image img;
            using (var ms = new MemoryStream(bytes))
                img = Image.FromStream(ms);
            var cloned = new Bitmap(img);
            img.Dispose();
            if (InvokeRequired) Invoke(() => SetAlbumArt(cloned));
            else SetAlbumArt(cloned);
        }
        catch { /* ignore */ }
    }

    private void SetAlbumArt(Image img)
    {
        _picAlbumArt.Image?.Dispose();
        _picAlbumArt.Image = img;
    }

    private void UpdateMediaDisplay(MediaInfo media)
    {
        _lblTitle.Text = string.IsNullOrWhiteSpace(media.CleanTitle) ? "Unknown" : media.CleanTitle;
        _lblArtist.Text = media.Artist;
        _lblAlbum.Text = media.Album;
        _navNow.Text = $"Now: {media.CleanTitle}";
    }

    private void OnPlaybackStateChanged(MediaPlaybackState state)
    {
        if (_currentMedia != null && _tglStateChanged.Checked && !string.IsNullOrWhiteSpace(_txtStateChanged.Text))
        {
            LogService.Log("Script", $"Running: {Path.GetFileName(_txtStateChanged.Text)}");
            ScriptingService.RunScript(_txtStateChanged.Text.Trim(), _currentMedia, state, _pendingImageUrl);
        }

        LogService.Log("Media", $"State: {state}");

        if (state == MediaPlaybackState.Paused)
        {
            _discordManager.ClearPresence();
            _lblHomeStatus.Text = "Discord RP: Paused";
        }
        else if (state == MediaPlaybackState.Playing && _currentMedia != null && ConfigManager.Config.AutoShowOnDiscord)
        {
            _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
        }
    }

    private void OnTimelineChanged() => _discordManager.RefreshPresence();

    private void OnMediaStopped()
    {
        _currentMedia = null;
        _pendingImageUrl = null;
        _discordManager.ClearPresence();
        LogService.Log("Media", "Playback stopped");

        if (_tglMediaStopped.Checked && !string.IsNullOrWhiteSpace(_txtMediaStopped.Text))
        {
            LogService.Log("Script", $"Running: {Path.GetFileName(_txtMediaStopped.Text)}");
            ScriptingService.OnMediaStopped(_txtMediaStopped.Text.Trim());
        }

        if (InvokeRequired) Invoke(ResetMediaDisplay);
        else ResetMediaDisplay();
    }

    private void ResetMediaDisplay()
    {
        _lblTitle.Text = "No media playing";
        _lblArtist.Text = "";
        _lblAlbum.Text = "";
        _picAlbumArt.Image = null;
        _lblHomeStatus.Text = "Discord RP: Not showing";
        _navNow.Text = "Now: —";
    }

    private void OnDiscordStatusChanged(string status)
    {
        if (InvokeRequired) { Invoke(() => OnDiscordStatusChanged(status)); return; }
        _navStatus.Text = $"Status: {status}";
        _navStatus.ForeColor = status.Contains("Connected") ? Success : Fail;
        _lblConnStatus.Text = $"Status: {status}";
        _lblConnStatus.ForeColor = status.Contains("Connected") ? Success : Fail;
    }

    private void OnPresenceUpdated(string info)
    {
        if (InvokeRequired) { Invoke(() => OnPresenceUpdated(info)); return; }
        _lblHomeStatus.Text = string.IsNullOrEmpty(info) ? "Discord RP: Not showing" : $"Discord RP: {info}";
    }

    private void ShowTrayNotification(string title, string artist)
    {
        var displayTitle = string.IsNullOrWhiteSpace(title) ? "Unknown" : title;
        var text = string.IsNullOrWhiteSpace(artist) ? displayTitle : $"{displayTitle} — {artist}";
        if (text.Length > 127) text = text[..124] + "...";
        _trayIcon?.ShowBalloonTip(8000, "Now Playing", text + "\nClick to show on Discord", ToolTipIcon.Info);
    }

    #endregion

    #region Hotkeys / Lifecycle

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager?.Dispose();
        _hotkeyManager = new HotkeyManager(Handle);
        _hotkeyManager.HotkeyFailed += (id, reason) => System.Diagnostics.Debug.WriteLine($"[9XT] Hotkey: {reason}");

        if (ConfigManager.Config.UseHotkeys)
        {
            _hotkeyManager.Register(ConfigManager.Config.ShowHotkey, () =>
            {
                if (_currentMedia != null)
                {
                    _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
                    if (InvokeRequired) Invoke(() => _lblHomeStatus.Text = $"Discord RP: {_currentMedia.CleanTitle}");
                    else _lblHomeStatus.Text = $"Discord RP: {_currentMedia.CleanTitle}";
                }
            });
            _hotkeyManager.Register(ConfigManager.Config.HideHotkey, () =>
            {
                _discordManager.ClearPresence();
                if (InvokeRequired) Invoke(() => _lblHomeStatus.Text = "Discord RP: Not showing");
                else _lblHomeStatus.Text = "Discord RP: Not showing";
            });
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && _hotkeyManager != null)
        {
            _hotkeyManager.HandleHotkey(m.WParam.ToInt32());
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _hotkeyManager?.UnregisterAll();
        base.OnFormClosing(e);
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            SaveSettings();
            _trayIcon?.Dispose();
            _hotkeyManager?.Dispose();
        }
    }

    #endregion
}

public class ToggleSwitch : Panel
{
    public bool Checked;
    public Action<bool>? OnToggle;
    private static readonly Color OnColor = Color.FromArgb(88, 101, 242);
    private static readonly Color OffColor = Color.FromArgb(70, 70, 85);

    public ToggleSwitch()
    {
        Width = 46;
        Height = 24;
        DoubleBuffered = true;
        Click += (s, e) =>
        {
            Checked = !Checked;
            Invalidate();
            OnToggle?.Invoke(Checked);
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        var r = Height / 2;
        path.AddArc(0, 0, r * 2, r * 2, 180, 90);
        path.AddArc(Width - r * 2, 0, r * 2, r * 2, 270, 90);
        path.AddArc(Width - r * 2, Height - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(0, Height - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(new SolidBrush(Checked ? OnColor : OffColor), path);

        int thumb = Height - 6;
        int tx = Checked ? Width - thumb - 3 : 3;
        g.FillEllipse(Brushes.White, tx, 3, thumb, thumb);
        base.OnPaint(e);
    }
}
