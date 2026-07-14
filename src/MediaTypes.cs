using System.IO;

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
#if WINDOWS
    public Windows.Storage.Streams.IRandomAccessStreamReference? Thumbnail { get; set; } = null;
#endif
    public string? ArtworkUrl { get; set; } = null;

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
