using System.Drawing.Drawing2D;

namespace GoodRP;

public class MainForm : Form
{
    private readonly MediaWatcher _mediaWatcher;
    private readonly DiscordManager _discordManager;
    private HotkeyManager? _hotkeyManager;
    private NotifyIcon? _trayIcon;

    private const int HOTKEY_SHOW = 1;
    private const int HOTKEY_HIDE = 2;

    private Label _lblStatus = new();
    private Label _lblCurrentMedia = new();
    private TextBox _txtDiscordId = new();
    private TextBox _txtCloudName = new();
    private TextBox _txtCloudPreset = new();
    private TextBox _txtDiscordWebhook = new();
    private CheckBox _chkAutoShow = new();
    private CheckBox _chkShowAlbumArt = new();
    private CheckBox _chkEnableArtFinder = new();
    private CheckBox _chkMcpServer = new();
    private CheckBox _chkUseHotkeys = new();
    private CheckBox _chkUseNotifications = new();
    private RadioButton _rbAuto = new();
    private RadioButton _rbListening = new();
    private RadioButton _rbWatching = new();
    private Button _btnConnect = new();
    private Button _btnShow = new();
    private Button _btnHide = new();
    private GroupBox _grpConnection = new();
    private GroupBox _grpMedia = new();
    private GroupBox _grpSettings = new();
    private Label _lblNowPlaying = new();
    private Label _lblArtist = new();
    private Label _lblAlbum = new();
    private PictureBox _picAlbumArt = new();
    private Label _lblShowHotkey = new();
    private Label _lblHideHotkey = new();

    private MediaInfo? _currentMedia;
    private string? _pendingImageUrl;

    public MainForm(MediaWatcher mediaWatcher, DiscordManager discordManager)
    {
        _mediaWatcher = mediaWatcher;
        _discordManager = discordManager;

        InitializeUI();
        SetupTrayIcon();
        WireEvents();

        LoadSettings();
    }

    private void InitializeUI()
    {
        Text = "GoodRP - Discord Rich Presence";
        Size = new Size(480, 610);
        MinimumSize = new Size(480, 610);
        MaximumSize = new Size(480, 610);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        _grpConnection.Text = "Discord Connection";
        _grpConnection.Location = new Point(12, 12);
        _grpConnection.Size = new Size(440, 100);
        _grpConnection.ForeColor = Color.White;
        _grpConnection.BackColor = Color.FromArgb(40, 40, 40);

        var lblId = new Label { Text = "Application ID:", Location = new Point(15, 25), AutoSize = true, ForeColor = Color.LightGray };
        _txtDiscordId.Location = new Point(120, 22);
        _txtDiscordId.Size = new Size(200, 23);
        _txtDiscordId.BackColor = Color.FromArgb(50, 50, 50);
        _txtDiscordId.ForeColor = Color.White;
        _txtDiscordId.PlaceholderText = "Enter Discord App ID";

        _btnConnect.Text = "Connect";
        _btnConnect.Location = new Point(330, 20);
        _btnConnect.Size = new Size(90, 28);
        _btnConnect.BackColor = Color.FromArgb(88, 101, 242);
        _btnConnect.ForeColor = Color.White;
        _btnConnect.FlatStyle = FlatStyle.Flat;
        _btnConnect.Click += BtnConnect_Click;

        _lblStatus.Text = "Status: Disconnected";
        _lblStatus.Location = new Point(15, 60);
        _lblStatus.AutoSize = true;
        _lblStatus.ForeColor = Color.Red;

        _grpConnection.Controls.AddRange(new Control[] { lblId, _txtDiscordId, _btnConnect, _lblStatus });

        _grpMedia.Text = "Now Playing";
        _grpMedia.Location = new Point(12, 120);
        _grpMedia.Size = new Size(440, 160);
        _grpMedia.ForeColor = Color.White;
        _grpMedia.BackColor = Color.FromArgb(40, 40, 40);

        _picAlbumArt.Location = new Point(15, 25);
        _picAlbumArt.Size = new Size(80, 80);
        _picAlbumArt.SizeMode = PictureBoxSizeMode.Zoom;
        _picAlbumArt.BackColor = Color.FromArgb(60, 60, 60);

        _lblNowPlaying.Text = "No media playing";
        _lblNowPlaying.Location = new Point(110, 25);
        _lblNowPlaying.Size = new Size(310, 25);
        _lblNowPlaying.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _lblNowPlaying.ForeColor = Color.White;

        _lblArtist.Text = "";
        _lblArtist.Location = new Point(110, 55);
        _lblArtist.Size = new Size(310, 20);
        _lblArtist.Font = new Font("Segoe UI", 10);
        _lblArtist.ForeColor = Color.LightGray;

        _lblAlbum.Text = "";
        _lblAlbum.Location = new Point(110, 80);
        _lblAlbum.Size = new Size(310, 20);
        _lblAlbum.Font = new Font("Segoe UI", 10);
        _lblAlbum.ForeColor = Color.Gray;

        _btnShow.Text = "Show on Discord";
        _btnShow.Location = new Point(110, 115);
        _btnShow.Size = new Size(140, 30);
        _btnShow.BackColor = Color.FromArgb(88, 101, 242);
        _btnShow.ForeColor = Color.White;
        _btnShow.FlatStyle = FlatStyle.Flat;
        _btnShow.Click += BtnShow_Click;

        _btnHide.Text = "Hide";
        _btnHide.Location = new Point(260, 115);
        _btnHide.Size = new Size(90, 30);
        _btnHide.BackColor = Color.FromArgb(80, 80, 80);
        _btnHide.ForeColor = Color.White;
        _btnHide.FlatStyle = FlatStyle.Flat;
        _btnHide.Click += BtnHide_Click;

        _grpMedia.Controls.AddRange(new Control[] { _picAlbumArt, _lblNowPlaying, _lblArtist, _lblAlbum, _btnShow, _btnHide });

        _grpSettings.Text = "Settings";
        _grpSettings.Location = new Point(12, 290);
        _grpSettings.Size = new Size(440, 265);
        _grpSettings.ForeColor = Color.White;
        _grpSettings.BackColor = Color.FromArgb(40, 40, 40);

        var lblCloudName = new Label { Text = "Cloud Name:", Location = new Point(15, 22), AutoSize = true, ForeColor = Color.LightGray };
        _txtCloudName.Location = new Point(120, 19);
        _txtCloudName.Size = new Size(200, 23);
        _txtCloudName.BackColor = Color.FromArgb(50, 50, 50);
        _txtCloudName.ForeColor = Color.White;
        _txtCloudName.PlaceholderText = "Cloudinary cloud name";

        var lblCloudPreset = new Label { Text = "Upload Preset:", Location = new Point(15, 50), AutoSize = true, ForeColor = Color.LightGray };
        _txtCloudPreset.Location = new Point(120, 47);
        _txtCloudPreset.Size = new Size(200, 23);
        _txtCloudPreset.BackColor = Color.FromArgb(50, 50, 50);
        _txtCloudPreset.ForeColor = Color.White;
        _txtCloudPreset.PlaceholderText = "Cloudinary upload preset";

        var lblDiscordWebhook = new Label { Text = "Webhook URL:", Location = new Point(15, 78), AutoSize = true, ForeColor = Color.LightGray };
        _txtDiscordWebhook.Location = new Point(120, 75);
        _txtDiscordWebhook.Size = new Size(300, 23);
        _txtDiscordWebhook.BackColor = Color.FromArgb(50, 50, 50);
        _txtDiscordWebhook.ForeColor = Color.White;
        _txtDiscordWebhook.PlaceholderText = "Discord webhook (optional fallback)";

        _chkShowAlbumArt.Text = "Show album art";
        _chkShowAlbumArt.Location = new Point(15, 110);
        _chkShowAlbumArt.AutoSize = true;
        _chkShowAlbumArt.ForeColor = Color.LightGray;

        _chkAutoShow.Text = "Auto-show on Discord";
        _chkAutoShow.Location = new Point(15, 135);
        _chkAutoShow.AutoSize = true;
        _chkAutoShow.ForeColor = Color.LightGray;

        _chkMcpServer.Text = "Enable MCP Server (for AI agents)";
        _chkMcpServer.Location = new Point(230, 110);
        _chkMcpServer.AutoSize = true;
        _chkMcpServer.ForeColor = Color.LightGray;

        _chkEnableArtFinder.Text = "Art finder fallback";
        _chkEnableArtFinder.Location = new Point(230, 135);
        _chkEnableArtFinder.AutoSize = true;
        _chkEnableArtFinder.ForeColor = Color.LightGray;

        var lblActivity = new Label { Text = "Activity:", Location = new Point(15, 165), AutoSize = true, ForeColor = Color.LightGray };

        _rbAuto.Text = "Auto";
        _rbAuto.Location = new Point(85, 163);
        _rbAuto.AutoSize = true;
        _rbAuto.ForeColor = Color.LightGray;
        _rbAuto.Checked = true;

        _rbListening.Text = "Listening";
        _rbListening.Location = new Point(140, 163);
        _rbListening.AutoSize = true;
        _rbListening.ForeColor = Color.LightGray;

        _rbWatching.Text = "Watching";
        _rbWatching.Location = new Point(240, 163);
        _rbWatching.AutoSize = true;
        _rbWatching.ForeColor = Color.LightGray;

        var lblNotifications = new Label { Text = "Notifications:", Location = new Point(15, 195), AutoSize = true, ForeColor = Color.LightGray };

        _chkUseNotifications.Text = "Show balloon notification";
        _chkUseNotifications.Location = new Point(15, 215);
        _chkUseNotifications.AutoSize = true;
        _chkUseNotifications.ForeColor = Color.LightGray;

        var lblHotkeys = new Label { Text = "Hotkeys:", Location = new Point(230, 195), AutoSize = true, ForeColor = Color.LightGray };

        _chkUseHotkeys.Text = "Enable global hotkeys";
        _chkUseHotkeys.Location = new Point(230, 215);
        _chkUseHotkeys.AutoSize = true;
        _chkUseHotkeys.ForeColor = Color.LightGray;

        var lblShowKey = new Label { Text = "Show:", Location = new Point(15, 245), AutoSize = true, ForeColor = Color.LightGray };
        _lblShowHotkey.Text = ConfigManager.Config.ShowHotkey;
        _lblShowHotkey.Location = new Point(65, 245);
        _lblShowHotkey.AutoSize = true;
        _lblShowHotkey.ForeColor = Color.FromArgb(88, 101, 242);

        var lblHideKey = new Label { Text = "Hide:", Location = new Point(150, 245), AutoSize = true, ForeColor = Color.LightGray };
        _lblHideHotkey.Text = ConfigManager.Config.HideHotkey;
        _lblHideHotkey.Location = new Point(200, 245);
        _lblHideHotkey.AutoSize = true;
        _lblHideHotkey.ForeColor = Color.FromArgb(88, 101, 242);

        _grpSettings.Controls.AddRange(new Control[] { lblCloudName, _txtCloudName, lblCloudPreset, _txtCloudPreset, lblDiscordWebhook, _txtDiscordWebhook, _chkShowAlbumArt, _chkAutoShow, _chkMcpServer, _chkEnableArtFinder, lblActivity, _rbAuto, _rbListening, _rbWatching, lblNotifications, _chkUseNotifications, lblHotkeys, _chkUseHotkeys, lblShowKey, _lblShowHotkey, lblHideKey, _lblHideHotkey });

        _lblCurrentMedia.Text = "Discord RP: Not showing";
        _lblCurrentMedia.Location = new Point(12, 545);
        _lblCurrentMedia.Size = new Size(440, 20);
        _lblCurrentMedia.ForeColor = Color.Gray;
        _lblCurrentMedia.TextAlign = ContentAlignment.MiddleCenter;

        Controls.AddRange(new Control[] { _grpConnection, _grpMedia, _grpSettings, _lblCurrentMedia });
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "GoodRP",
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
        if (InvokeRequired)
        {
            Invoke(ShowForm);
            return;
        }
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

        _rbAuto.CheckedChanged += OnActivityTypeChanged;
        _rbListening.CheckedChanged += OnActivityTypeChanged;
        _rbWatching.CheckedChanged += OnActivityTypeChanged;
    }

    private void LoadSettings()
    {
        _txtDiscordId.Text = ConfigManager.Config.DiscordClientId;
        _txtCloudName.Text = ConfigManager.Config.CloudinaryCloudName;
        _txtCloudPreset.Text = ConfigManager.Config.CloudinaryUploadPreset;
        _txtDiscordWebhook.Text = ConfigManager.Config.DiscordWebhookUrl;
        _chkAutoShow.Checked = ConfigManager.Config.AutoShowOnDiscord;
        _chkShowAlbumArt.Checked = ConfigManager.Config.ShowAlbumArt;
        _chkEnableArtFinder.Checked = ConfigManager.Config.EnableArtFinder;
        _chkMcpServer.Checked = ConfigManager.Config.McpServerEnabled;
        _chkUseHotkeys.Checked = ConfigManager.Config.UseHotkeys;
        _chkUseNotifications.Checked = ConfigManager.Config.UseNotifications;

        var overrideVal = ConfigManager.Config.ActivityTypeOverride;
        _rbAuto.Checked = overrideVal == "Auto";
        _rbListening.Checked = overrideVal == "Listening";
        _rbWatching.Checked = overrideVal == "Watching";

        _lblShowHotkey.Text = ConfigManager.Config.ShowHotkey;
        _lblHideHotkey.Text = ConfigManager.Config.HideHotkey;
    }

    private void SaveSettings()
    {
        ConfigManager.Config.DiscordClientId = _txtDiscordId.Text.Trim();
        ConfigManager.Config.CloudinaryCloudName = _txtCloudName.Text.Trim();
        ConfigManager.Config.CloudinaryUploadPreset = _txtCloudPreset.Text.Trim();
        ConfigManager.Config.DiscordWebhookUrl = _txtDiscordWebhook.Text.Trim();
        ConfigManager.Config.AutoShowOnDiscord = _chkAutoShow.Checked;
        ConfigManager.Config.ShowAlbumArt = _chkShowAlbumArt.Checked;
        ConfigManager.Config.EnableArtFinder = _chkEnableArtFinder.Checked;
        ConfigManager.Config.McpServerEnabled = _chkMcpServer.Checked;
        ConfigManager.Config.UseHotkeys = _chkUseHotkeys.Checked;
        ConfigManager.Config.UseNotifications = _chkUseNotifications.Checked;
        ConfigManager.Config.ActivityTypeOverride = _rbAuto.Checked ? "Auto" : _rbListening.Checked ? "Listening" : "Watching";
        ConfigManager.Save();
    }

    private void BtnConnect_Click(object? sender, EventArgs e)
    {
        SaveSettings();

        var clientId = _txtDiscordId.Text.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            MessageBox.Show("Please enter a Discord Application ID.", "GoodRP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _discordManager.Connect(clientId);
    }

    private void BtnShow_Click(object? sender, EventArgs e)
    {
        if (_currentMedia != null)
        {
            _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
        }
    }

    private void BtnHide_Click(object? sender, EventArgs e)
    {
        _discordManager.ClearPresence();
        _lblCurrentMedia.Text = "Discord RP: Not showing";
    }

    private void OnActivityTypeChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton rb && rb.Checked)
        {
            SaveSettings();
            _discordManager.RefreshPresence();
        }
    }

    private async void OnMediaChanged(MediaInfo media)
    {
        _currentMedia = media;

        if (InvokeRequired)
            Invoke(() => UpdateMediaDisplay(media));
        else
            UpdateMediaDisplay(media);

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
        {
            _discordManager.SetPresence(media, imageUrl);
        }
        else if (ConfigManager.Config.UseNotifications)
        {
            ShowTrayNotification(media.CleanTitle, media.Artist);
        }
    }

    private void OnPlaybackStateChanged(MediaPlaybackState state)
    {
        if (state == MediaPlaybackState.Paused)
        {
            _discordManager.ClearPresence();

            if (InvokeRequired)
                Invoke(() => _lblCurrentMedia.Text = "Discord RP: Paused");
            else
                _lblCurrentMedia.Text = "Discord RP: Paused";
        }
        else if (state == MediaPlaybackState.Playing && _currentMedia != null)
        {
            if (ConfigManager.Config.AutoShowOnDiscord)
            {
                _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
            }
        }
    }

    private void ShowTrayNotification(string title, string artist)
    {
        var displayTitle = string.IsNullOrWhiteSpace(title) ? "Unknown" : title;
        var text = string.IsNullOrWhiteSpace(artist)
            ? displayTitle
            : $"{displayTitle} — {artist}";

        if (text.Length > 127) text = text[..124] + "...";

        _trayIcon!.ShowBalloonTip(
            8000,
            "Now Playing",
            text + "\nClick to show on Discord",
            ToolTipIcon.Info
        );
    }

    private void UpdateMediaDisplay(MediaInfo media)
    {
        _lblNowPlaying.Text = string.IsNullOrWhiteSpace(media.CleanTitle) ? "Unknown" : media.CleanTitle;
        _lblArtist.Text = media.Artist;
        _lblAlbum.Text = media.Album;

        var trayText = $"GoodRP - {media.CleanTitle}";
        _trayIcon!.Text = trayText.Length > 63 ? trayText[..63] : trayText;
    }

    private void OnTimelineChanged()
    {
        _discordManager.RefreshPresence();
    }

    private void OnMediaStopped()
    {
        _currentMedia = null;
        _pendingImageUrl = null;
        _discordManager.ClearPresence();

        if (InvokeRequired)
        {
            Invoke(() =>
            {
                _lblNowPlaying.Text = "No media playing";
                _lblArtist.Text = "";
                _lblAlbum.Text = "";
                _picAlbumArt.Image = null;
                _lblCurrentMedia.Text = "Discord RP: Not showing";
                _trayIcon!.Text = "GoodRP";
            });
        }
        else
        {
            _lblNowPlaying.Text = "No media playing";
            _lblArtist.Text = "";
            _lblAlbum.Text = "";
            _picAlbumArt.Image = null;
            _lblCurrentMedia.Text = "Discord RP: Not showing";
            _trayIcon!.Text = "GoodRP";
        }
    }

    private void OnDiscordStatusChanged(string status)
    {
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                _lblStatus.Text = $"Status: {status}";
                _lblStatus.ForeColor = status.Contains("Connected") ? Color.LimeGreen : Color.Red;
            });
        }
        else
        {
            _lblStatus.Text = $"Status: {status}";
            _lblStatus.ForeColor = status.Contains("Connected") ? Color.LimeGreen : Color.Red;
        }
    }

    private void OnPresenceUpdated(string info)
    {
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                _lblCurrentMedia.Text = string.IsNullOrEmpty(info)
                    ? "Discord RP: Not showing"
                    : $"Discord RP: {info}";
            });
        }
        else
        {
            _lblCurrentMedia.Text = string.IsNullOrEmpty(info)
                ? "Discord RP: Not showing"
                : $"Discord RP: {info}";
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager?.Dispose();
        _hotkeyManager = new HotkeyManager(Handle);

        _hotkeyManager.HotkeyFailed += (id, reason) =>
        {
            System.Diagnostics.Debug.WriteLine($"[GoodRP] Hotkey: {reason}");
        };

        if (ConfigManager.Config.UseHotkeys)
        {
            _hotkeyManager.Register(ConfigManager.Config.ShowHotkey, () =>
            {
                if (_currentMedia != null)
                {
                    _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
                    if (InvokeRequired)
                        Invoke(() => _lblCurrentMedia.Text = $"Discord RP: {_currentMedia.CleanTitle}");
                    else
                        _lblCurrentMedia.Text = $"Discord RP: {_currentMedia.CleanTitle}";
                }
            });

            _hotkeyManager.Register(ConfigManager.Config.HideHotkey, () =>
            {
                _discordManager.ClearPresence();
                if (InvokeRequired)
                    Invoke(() => _lblCurrentMedia.Text = "Discord RP: Not showing");
                else
                    _lblCurrentMedia.Text = "Discord RP: Not showing";
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

    private static Icon CreateAppIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Color.FromArgb(88, 101, 242));
        g.FillRoundedRectangle(brush, 0, 0, 32, 32, 6);

        using var font = new Font("Arial", 14, FontStyle.Bold);
        g.DrawString("G", font, Brushes.White, 7, 4);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static bool IsDiscordCdnUrl(string url)
    {
        return url.Contains("discordapp.com") || url.Contains("discordapp.net");
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        var rect = new Rectangle(x, y, width, height);
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
