namespace GoodRP.Interfaces;

public interface IMediaWatcher : IDisposable
{
    event Action<MediaInfo>? MediaChanged;
    event Action? MediaStopped;
    event Action<MediaPlaybackState>? PlaybackStateChanged;
    event Action? TimelineChanged;

    MediaInfo? CurrentMedia { get; }

    Task StartAsync();
}
