namespace GoodRP;

public class TrayIcon : IDisposable
{
    private NotifyIcon _trayIcon;
    private readonly MediaWatcher _mediaWatcher;
    private readonly DiscordManager _discordManager;
    private MediaInfo? _pendingMedia;
    private bool _showingPresence;
    private System.Windows.Forms.Timer? _clearTimer;
    private bool _disposed;

    public TrayIcon(MediaWatcher mediaWatcher, DiscordManager discordManager)
    {
        _mediaWatcher = mediaWatcher;
        _discordManager = discordManager;

        _trayIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "GoodRP",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.DoubleClick += OnTrayDoubleClick;
        _trayIcon.BalloonTipClicked += OnBalloonClicked;

        _mediaWatcher.MediaChanged += OnMediaChanged;
        _mediaWatcher.MediaStopped += OnMediaStopped;

        _clearTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _clearTimer.Tick += (s, e) =>
        {
            _clearTimer.Stop();
            if (!_showingPresence) return;
            _discordManager.ClearPresence();
            _showingPresence = false;
            UpdateTrayText("GoodRP");
        };
    }

    private void OnMediaChanged(MediaInfo media)
    {
        if (string.IsNullOrWhiteSpace(media.Title)) return;

        _pendingMedia = media;
        _clearTimer?.Stop();

        if (ConfigManager.Config.AutoShowOnDiscord)
        {
            ShowPresence(media);
            return;
        }

        _trayIcon.ShowBalloonTip(
            5000,
            "Now Playing",
            $"{media.Title} — Click to show on Discord",
            ToolTipIcon.Info
        );
    }

    private void OnMediaStopped()
    {
        _clearTimer?.Start();
    }

    private void OnBalloonClicked(object? sender, EventArgs e)
    {
        if (_pendingMedia != null)
        {
            ShowPresence(_pendingMedia);
        }
    }

    private async void ShowPresence(MediaInfo media)
    {
        _showingPresence = true;
        UpdateTrayText($"GoodRP — {media.Title}");

        string? imageUrl = null;

        if (ConfigManager.Config.ShowAlbumArt)
        {
            var cacheKey = $"{media.Title}_{media.Artist}";

            if (ConfigManager.Config.EnableArtFinder)
                imageUrl = await ArtFinderService.FindArtAsync(media.Title, media.Artist, media.Album);

            imageUrl ??= await ImageUploader.UploadThumbnailAsync(media.Thumbnail, cacheKey);

            if (imageUrl != null && Utilities.UrlHelper.IsDiscordCdnUrl(imageUrl))
                imageUrl = null;

            ImageUploader.TrimCache();
        }

        _discordManager.SetPresence(media, imageUrl);
    }

    private void OnTrayDoubleClick(object? sender, EventArgs e)
    {
        if (_pendingMedia != null && !_showingPresence)
        {
            ShowPresence(_pendingMedia);
        }
        else if (_showingPresence)
        {
            _discordManager.ClearPresence();
            _showingPresence = false;
            UpdateTrayText("GoodRP");
        }
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Status: Disconnected") { Enabled = false };
        statusItem.Tag = "status";
        menu.Items.Add(statusItem);

        menu.Items.Add(new ToolStripSeparator());

        var nowPlayingItem = new ToolStripMenuItem("Now Playing: None") { Enabled = false };
        nowPlayingItem.Tag = "nowplaying";
        menu.Items.Add(nowPlayingItem);

        var showItem = new ToolStripMenuItem("Show on Discord", null, (s, e) =>
        {
            if (_pendingMedia != null)
                ShowPresence(_pendingMedia);
        });
        showItem.Tag = "show";
        menu.Items.Add(showItem);

        var hideItem = new ToolStripMenuItem("Hide from Discord", null, (s, e) =>
        {
            _discordManager.ClearPresence();
            _showingPresence = false;
            UpdateTrayText("GoodRP");
        });
        hideItem.Tag = "hide";
        menu.Items.Add(hideItem);

        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Settings", null, (s, e) =>
        {
            using var form = new SettingsForm(_discordManager);
            form.ShowDialog();
        });
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem("Quit", null, (s, e) =>
        {
            Application.Exit();
        });
        menu.Items.Add(quitItem);

        return menu;
    }

    public void UpdateStatus(string status)
    {
        if (_trayIcon.ContextMenuStrip == null) return;

        foreach (ToolStripItem item in _trayIcon.ContextMenuStrip.Items)
        {
            if (item.Tag?.ToString() == "status")
            {
                item.Text = $"Status: {status}";
                break;
            }
        }
    }

    public void UpdateNowPlaying(string title)
    {
        if (_trayIcon.ContextMenuStrip == null) return;

        foreach (ToolStripItem item in _trayIcon.ContextMenuStrip.Items)
        {
            if (item.Tag?.ToString() == "nowplaying")
            {
                var display = string.IsNullOrWhiteSpace(title) ? "None" : TruncateText(title, 30);
                item.Text = $"Now Playing: {display}";
                break;
            }
        }
    }

    private void UpdateTrayText(string text)
    {
        if (text.Length > 63) text = text[..63];
        _trayIcon.Text = text;
        UpdateNowPlaying(text == "GoodRP" ? "" : text.Replace("GoodRP — ", ""));
    }

    private static string TruncateText(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }

    private static Icon CreateDefaultIcon()
    {
        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        g.FillEllipse(Brushes.MediumPurple, 1, 1, 14, 14);
        g.DrawString("G", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 2, 1);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _clearTimer?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        GC.SuppressFinalize(this);
    }
}
