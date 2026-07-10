using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace GoodRP.Mcp.Tools;

[McpServerToolType]
public class PresenceTools
{
    private readonly DiscordManager _discordManager;
    private readonly MediaWatcher _mediaWatcher;

    public PresenceTools(DiscordManager discordManager, MediaWatcher mediaWatcher)
    {
        _discordManager = discordManager;
        _mediaWatcher = mediaWatcher;
    }

    [McpServerTool, Description("Show current media on Discord. 'type' is REQUIRED — must be 'watching' or 'listening'. Optionally override title, artist, album, app_name, or image.")]
    public string SetPresence(
        [Description("Required: 'watching' or 'listening'")] string type,
        [Description("Optional: override media title")] string? title = null,
        [Description("Optional: override artist name")] string? artist = null,
        [Description("Optional: override album name")] string? album = null,
        [Description("Optional: override app name")] string? app_name = null,
        [Description("Optional: override image URL")] string? image_url = null)
    {
        var normalizedType = type?.ToLowerInvariant();
        if (normalizedType != "watching" && normalizedType != "listening")
            return """{"error":"type is required and must be 'watching' or 'listening'"}""";

        if (!_discordManager.IsConnected)
            return """{"error":"Not connected to Discord"}""";

        var media = _mediaWatcher.CurrentMedia;
        if (media == null)
            return """{"error":"No media detected"}""";

        ConfigManager.Config.ActivityTypeOverride =
            normalizedType == "watching" ? "Watching" : "Listening";
        ConfigManager.Save();

        if (title != null || artist != null || album != null || app_name != null)
        {
            media = new MediaInfo
            {
                Title = title ?? media.Title,
                Artist = artist ?? media.Artist,
                Album = album ?? media.Album,
                AppName = app_name ?? media.AppName,
                State = media.State,
                Position = media.Position,
                Duration = media.Duration,
                Thumbnail = media.Thumbnail
            };
        }

        _discordManager.SetPresence(media, image_url);

        var showing = string.IsNullOrWhiteSpace(media.Artist)
            ? media.CleanTitle
            : $"{media.CleanTitle} — {media.Artist}";

        return JsonSerializer.Serialize(new
        {
            success = true,
            activity_type = normalizedType,
            showing
        });
    }

    [McpServerTool, Description("Hide current media from Discord")]
    public string ClearPresence()
    {
        _discordManager.ClearPresence();
        return """{"success":true}""";
    }

    [McpServerTool, Description("Enable or disable auto-show when media plays")]
    public string SetAutoShow(
        [Description("true to enable, false to disable")] bool enabled)
    {
        ConfigManager.Config.AutoShowOnDiscord = enabled;
        ConfigManager.Save();
        return JsonSerializer.Serialize(new { success = true, auto_show = enabled });
    }

    [McpServerTool, Description("Get current GoodRP config")]
    public string GetConfig()
    {
        var c = ConfigManager.Config;
        return JsonSerializer.Serialize(new
        {
            discord_client_id = c.DiscordClientId,
            cloudinary_cloud_name = c.CloudinaryCloudName,
            cloudinary_upload_preset = c.CloudinaryUploadPreset,
            show_album_art = c.ShowAlbumArt,
            enable_art_finder = c.EnableArtFinder,
            image_providers = c.ImageProviders,
            auto_show_on_discord = c.AutoShowOnDiscord,
            activity_type_override = c.ActivityTypeOverride
        });
    }
}
