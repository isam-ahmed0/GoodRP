using System.ComponentModel;
using System.Text.Json;
using GoodRP.Interfaces;
using ModelContextProtocol.Server;

namespace GoodRP.Mcp.Tools;

[McpServerToolType]
public class MediaTools
{
    private readonly IMediaWatcher _mediaWatcher;

    public MediaTools(IMediaWatcher mediaWatcher)
    {
        _mediaWatcher = mediaWatcher;
    }

    [McpServerTool, Description("Get full details of the currently detected media")]
    public string GetCurrentMedia()
    {
        var media = _mediaWatcher.CurrentMedia;
        if (media == null)
            return """{"error":"No media detected"}""";

        return JsonSerializer.Serialize(new
        {
            title = media.Title,
            artist = media.Artist,
            album = media.Album,
            app_name = media.AppName,
            state = media.State.ToString().ToLower(),
            position_seconds = (int)media.Position.TotalSeconds,
            duration_seconds = (int)media.Duration.TotalSeconds,
            position_formatted = FormatTime(media.Position),
            duration_formatted = FormatTime(media.Duration),
            progress_percent = media.Duration.TotalSeconds > 0
                ? Math.Round(media.Position.TotalSeconds / media.Duration.TotalSeconds * 100, 1)
                : 0,
            clean_title = media.CleanTitle,
            estimated_type = GetEstimatedType(media.AppName)
        });
    }

    private static string FormatTime(TimeSpan ts)
    {
        return ts.Hours > 0
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private static string GetEstimatedType(string appName)
    {
        var lower = appName.ToLowerInvariant();
        return lower.Contains("video") || lower.Contains("movie") ||
               lower.Contains("vlc") || lower.Contains("mpv")
            ? "watching"
            : "listening";
    }
}
