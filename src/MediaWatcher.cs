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
}

public class MediaWatcher : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _disposed;

    public event Action<MediaInfo>? MediaChanged;
    public event Action? MediaStopped;

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
        _currentSession = session;

        session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

        await UpdateMediaInfoAsync(session);
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await UpdateMediaInfoAsync(sender);
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        await UpdateMediaInfoAsync(sender);
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        UpdateTimeline(sender);
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

            if (state == MediaPlaybackState.Playing)
            {
                MediaChanged?.Invoke(CurrentMedia);
            }
            else if (state == MediaPlaybackState.Stopped)
            {
                CurrentMedia = null;
                MediaStopped?.Invoke();
            }
        }
        catch
        {
            // Session may have closed
        }
    }

    private void UpdateTimeline(GlobalSystemMediaTransportControlsSession session)
    {
        if (CurrentMedia == null) return;

        try
        {
            var timeline = session.GetTimelineProperties();
            CurrentMedia.Position = timeline.Position;
            CurrentMedia.Duration = timeline.EndTime - timeline.StartTime;
        }
        catch { }
    }

    private static MediaPlaybackState MapPlaybackState(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackState.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaPlaybackState.Stopped,
            _ => MediaPlaybackState.None
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        if (_sessionManager != null)
        {
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }

        GC.SuppressFinalize(this);
    }
}
