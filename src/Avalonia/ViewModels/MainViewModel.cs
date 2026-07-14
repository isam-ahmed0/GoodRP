using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GoodRP.Interfaces;

namespace GoodRP.Avalonia.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IMediaWatcher _mediaWatcher;
    private readonly DiscordManager _discordManager;
    private string _currentPage = "Home";
    private string _statusText = "Status: Disconnected";
    private string _statusColor = "#F26464";
    private string _nowPlaying = "Now: --";
    private string _homeStatus = "Discord RP: Not showing";
    private string _mediaTitle = "No media playing";
    private string _mediaArtist = "";
    private string _mediaAlbum = "";
    private string? _albumArtUrl;
    private string _connStatusText = "Status: Disconnected";
    private string _connStatusColor = "#F26464";
    private MediaInfo? _currentMedia;
    private string? _pendingImageUrl;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> LogEntries { get; } = new();
    public ObservableCollection<DiscordAppEntry> AppIds { get; } = new();

    public string CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public string NowPlaying
    {
        get => _nowPlaying;
        set { _nowPlaying = value; OnPropertyChanged(); }
    }

    public string HomeStatus
    {
        get => _homeStatus;
        set { _homeStatus = value; OnPropertyChanged(); }
    }

    public string MediaTitle
    {
        get => _mediaTitle;
        set { _mediaTitle = value; OnPropertyChanged(); }
    }

    public string MediaArtist
    {
        get => _mediaArtist;
        set { _mediaArtist = value; OnPropertyChanged(); }
    }

    public string MediaAlbum
    {
        get => _mediaAlbum;
        set { _mediaAlbum = value; OnPropertyChanged(); }
    }

    public string? AlbumArtUrl
    {
        get => _albumArtUrl;
        set { _albumArtUrl = value; OnPropertyChanged(); }
    }

    public string ConnStatusText
    {
        get => _connStatusText;
        set { _connStatusText = value; OnPropertyChanged(); }
    }

    public string ConnStatusColor
    {
        get => _connStatusColor;
        set { _connStatusColor = value; OnPropertyChanged(); }
    }

    public bool AutoShowOnDiscord
    {
        get => ConfigManager.Config.AutoShowOnDiscord;
        set { ConfigManager.Config.AutoShowOnDiscord = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public bool ShowAlbumArt
    {
        get => ConfigManager.Config.ShowAlbumArt;
        set { ConfigManager.Config.ShowAlbumArt = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public bool EnableArtFinder
    {
        get => ConfigManager.Config.EnableArtFinder;
        set { ConfigManager.Config.EnableArtFinder = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public bool McpServerEnabled
    {
        get => ConfigManager.Config.McpServerEnabled;
        set { ConfigManager.Config.McpServerEnabled = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public bool UseNotifications
    {
        get => ConfigManager.Config.UseNotifications;
        set { ConfigManager.Config.UseNotifications = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public bool UseHotkeys
    {
        get => ConfigManager.Config.UseHotkeys;
        set { ConfigManager.Config.UseHotkeys = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public string ActivityTypeOverride
    {
        get => ConfigManager.Config.ActivityTypeOverride;
        set { ConfigManager.Config.ActivityTypeOverride = value; ConfigManager.Save(); OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        _mediaWatcher = MediaWatcherFactory.Create();
        _discordManager = new DiscordManager();

        _mediaWatcher.MediaChanged += OnMediaChanged;
        _mediaWatcher.MediaStopped += OnMediaStopped;
        _mediaWatcher.PlaybackStateChanged += OnPlaybackStateChanged;
        _mediaWatcher.TimelineChanged += OnTimelineChanged;

        _discordManager.StatusChanged += OnDiscordStatusChanged;
        _discordManager.PresenceUpdated += OnPresenceUpdated;

        LogService.EntryAdded += OnLogEntry;

        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        await _mediaWatcher.StartAsync();

        if (!string.IsNullOrWhiteSpace(ConfigManager.Config.DiscordClientId))
        {
            _discordManager.Connect(ConfigManager.Config.DiscordClientId);
        }

        LogService.Log("App", "GoodRP Avalonia started");
    }

    private void OnMediaChanged(MediaInfo media)
    {
        _currentMedia = media;
        MediaTitle = media.CleanTitle;
        MediaArtist = media.Artist;
        MediaAlbum = media.Album;
        NowPlaying = $"Now: {media.CleanTitle}";
        LogService.Log("Media", $"Playing: {media.CleanTitle} - {media.Artist}");

        _ = FetchAlbumArtAsync(media);
    }

    private async Task FetchAlbumArtAsync(MediaInfo media)
    {
        if (!ConfigManager.Config.ShowAlbumArt) return;

        string? imageUrl = null;

        if (ConfigManager.Config.EnableArtFinder)
            imageUrl = await ArtFinderService.FindArtAsync(media.Title, media.Artist, media.Album);

#if WINDOWS
        imageUrl ??= await ImageUploader.UploadThumbnailAsync(media.Thumbnail, $"{media.Title}_{media.Artist}");
#endif

        if (imageUrl != null && Utilities.UrlHelper.IsDiscordCdnUrl(imageUrl))
            imageUrl = null;

        _pendingImageUrl = imageUrl;

        if (ConfigManager.Config.AutoShowOnDiscord)
            _discordManager.SetPresence(media, imageUrl);

        AlbumArtUrl = imageUrl;
    }

    private void OnMediaStopped()
    {
        _currentMedia = null;
        _pendingImageUrl = null;
        MediaTitle = "No media playing";
        MediaArtist = "";
        MediaAlbum = "";
        AlbumArtUrl = null;
        HomeStatus = "Discord RP: Not showing";
        LogService.Log("Media", "Playback stopped");

        _discordManager.ClearPresence();
    }

    private void OnPlaybackStateChanged(MediaPlaybackState state)
    {
        if (_currentMedia == null) return;

        LogService.Log("Media", $"State: {state}");

        if (state == MediaPlaybackState.Paused)
        {
            _discordManager.ClearPresence();
            HomeStatus = "Discord RP: Paused";
        }
        else if (state == MediaPlaybackState.Playing && ConfigManager.Config.AutoShowOnDiscord)
        {
            _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
            HomeStatus = "Discord RP: Showing";
        }
    }

    private void OnTimelineChanged()
    {
        _discordManager.RefreshPresence();
    }

    private void OnDiscordStatusChanged(string status)
    {
        StatusText = $"Status: {status}";
        ConnStatusText = $"Status: {status}";
        var connected = status.StartsWith("Connected");
        StatusColor = connected ? "LimeGreen" : "#F26464";
        ConnStatusColor = connected ? "LimeGreen" : "#F26464";
    }

    private void OnPresenceUpdated(string info)
    {
        if (!string.IsNullOrEmpty(info))
            HomeStatus = $"Discord RP: Showing";
    }

    private void OnLogEntry(LogEntry entry)
    {
        var msg = $"{entry.Timestamp:HH:mm:ss} | {entry.Type} | {entry.Message} | {(entry.Success ? "OK" : "FAIL")}";
        LogEntries.Add(msg);
        if (LogEntries.Count > 500)
            LogEntries.RemoveAt(0);
    }

    public void ShowOnDiscord()
    {
        if (_currentMedia == null) return;
        _discordManager.SetPresence(_currentMedia, _pendingImageUrl);
        HomeStatus = "Discord RP: Showing";
    }

    public void HideFromDiscord()
    {
        _discordManager.ClearPresence();
        HomeStatus = "Discord RP: Not showing";
    }

    public void ConnectSelectedAppId(DiscordAppEntry entry)
    {
        ConfigManager.Config.DiscordClientId = entry.Id;
        ConfigManager.Save();
        _discordManager.Disconnect();
        _discordManager.Connect(entry.Id);
    }

    public void RefreshAppIds()
    {
        AppIds.Clear();
        foreach (var entry in ConfigManager.Config.DiscordAppIds)
            AppIds.Add(entry);
    }

    public void AddAppId(string name, string id)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id)) return;
        ConfigManager.Config.DiscordAppIds.Add(new DiscordAppEntry { Name = name, Id = id });
        ConfigManager.Save();
        RefreshAppIds();
    }

    public void RemoveAppId(int index)
    {
        if (index < 0 || index >= ConfigManager.Config.DiscordAppIds.Count) return;
        ConfigManager.Config.DiscordAppIds.RemoveAt(index);
        ConfigManager.Save();
        RefreshAppIds();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaWatcher.MediaChanged -= OnMediaChanged;
        _mediaWatcher.MediaStopped -= OnMediaStopped;
        _mediaWatcher.PlaybackStateChanged -= OnPlaybackStateChanged;
        _mediaWatcher.TimelineChanged -= OnTimelineChanged;

        _discordManager.StatusChanged -= OnDiscordStatusChanged;
        _discordManager.PresenceUpdated -= OnPresenceUpdated;

        LogService.EntryAdded -= OnLogEntry;

        _mediaWatcher.Dispose();
        _discordManager.Dispose();

        GC.SuppressFinalize(this);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
