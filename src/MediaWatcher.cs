using Windows.Media.Control;
using Windows.Storage.Streams;

namespace GoodRP;

public enum MediaPlaybackState
{
    None,
    Playing,
    Paused,
    Stopped
}

public class MediaInfo
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public string AppName { get; set; } = "";
    public MediaPlaybackState State { get; set; } = MediaPlaybackState.None;
    public TimeSpan Position { get; set; } = TimeSpan.Zero;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public IRandomAccessStreamReference? Thumbnail { get; set; } = null;

    public string CleanTitle => CleanMediaTitle(Title);

    private static string CleanMediaTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return title;

        var exts = new[] { ".mp3", ".mp4", ".mkv", ".avi", ".flac", ".ogg", ".wav", ".m4a", ".webm", ".mov", ".wmv" };
        foreach (var ext in exts)
        {
            if (title.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                title = title[..^ext.Length];
                break;
            }
        }

        return title.Replace('_', ' ').Trim();
    }
}

public class MediaWatcher : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _disposed;

    public event Action<MediaInfo>? MediaChanged;
    public event Action? MediaStopped;
    public event Action<MediaPlaybackState>? PlaybackStateChanged;
    public event Action? TimelineChanged;

    public MediaInfo? CurrentMedia { get; private set; }

    public async Task StartAsync()
    {
        _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

        var session = _sessionManager.GetCurrentSession();
        if (session != null)
        {
            await AttachSessionAsync(session);
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        DetachSession();
        var session = sender.GetCurrentSession();
        if (session != null)
        {
            await AttachSessionAsync(session);
        }
        else
        {
            CurrentMedia = null;
            MediaStopped?.Invoke();
        }
    }

    private async Task AttachSessionAsync(GlobalSystemMediaTransportControlsSession session)
    {
        DetachSession();
        _currentSession = session;

        session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

        await UpdateMediaInfoAsync(session);
    }

    private void DetachSession()
    {
        if (_currentSession == null) return;

        _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _currentSession = null;
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await UpdateMediaInfoAsync(sender);
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        try
        {
            var playback = sender.GetPlaybackInfo();
            var state = MapPlaybackState(playback.PlaybackStatus);

            if (state == MediaPlaybackState.Stopped)
            {
                CurrentMedia = null;
                MediaStopped?.Invoke();
                return;
            }

            if (CurrentMedia != null)
            {
                CurrentMedia.State = state;
                PlaybackStateChanged?.Invoke(state);
            }

            if (state == MediaPlaybackState.Playing)
            {
                await UpdateMediaInfoAsync(sender);
            }
        }
        catch { }
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        if (CurrentMedia == null) return;

        try
        {
            var timeline = sender.GetTimelineProperties();
            CurrentMedia.Position = timeline.Position;
            CurrentMedia.Duration = timeline.EndTime - timeline.StartTime;
            TimelineChanged?.Invoke();
        }
        catch { }
    }

    private async Task UpdateMediaInfoAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            var appName = session.SourceAppUserModelId;
            try
            {
                var appInfo = Windows.ApplicationModel.AppInfo.GetFromAppUserModelId(appName);
                appName = appInfo?.DisplayInfo?.DisplayName ?? appName;
            }
            catch { }

            var state = MapPlaybackState(playback.PlaybackStatus);

            if (state == MediaPlaybackState.Stopped)
            {
                CurrentMedia = null;
                MediaStopped?.Invoke();
                return;
            }

            CurrentMedia = new MediaInfo
            {
                Title = props.Title ?? "",
                Artist = props.Artist ?? "",
                Album = props.AlbumTitle ?? "",
                AppName = appName,
                State = state,
                Position = timeline.Position,
                Duration = timeline.EndTime - timeline.StartTime,
                Thumbnail = props.Thumbnail
            };

            MediaChanged?.Invoke(CurrentMedia);
        }
        catch
        {
            CurrentMedia = null;
            MediaStopped?.Invoke();
        }
    }

    private static MediaPlaybackState MapPlaybackState(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackState.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaPlaybackState.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => MediaPlaybackState.Playing,
            _ => MediaPlaybackState.None
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DetachSession();

        if (_sessionManager != null)
        {
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }

        GC.SuppressFinalize(this);
    }
}
