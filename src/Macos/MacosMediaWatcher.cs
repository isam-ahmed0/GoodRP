using GoodRP.Interfaces;

namespace GoodRP.Macos;

#pragma warning disable CS0067

public class MacosMediaWatcher : IMediaWatcher
{
    public event Action<MediaInfo>? MediaChanged;
    public event Action? MediaStopped;
    public event Action<MediaPlaybackState>? PlaybackStateChanged;
    public event Action? TimelineChanged;

    public MediaInfo? CurrentMedia { get; private set; }

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
