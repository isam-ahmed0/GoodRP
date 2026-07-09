using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoodRP.Api;

public static class ApiServer
{
    public static async Task RunAsync(DiscordManager discordManager, MediaWatcher mediaWatcher, string[] args)
    {
        var port = "9876";
        var portIdx = Array.IndexOf(args, "--port");
        if (portIdx >= 0 && portIdx + 1 < args.Length)
            port = args[portIdx + 1];

        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSingleton(discordManager);
        builder.Services.AddSingleton(mediaWatcher);

        var app = builder.Build();
        app.Urls.Add($"http://127.0.0.1:{port}");

        MapEndpoints(app, discordManager, mediaWatcher);

        Console.Error.WriteLine($"[GoodRP] API server listening on http://127.0.0.1:{port}");
        await app.RunAsync();
    }

    private static void MapEndpoints(WebApplication app, DiscordManager discord, MediaWatcher watcher)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

        app.MapGet("/api/media", () =>
        {
            var media = watcher.CurrentMedia;
            if (media == null)
                return Results.Ok(new { error = "No media detected" });

            return Results.Ok(new
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
        });

        app.MapPost("/api/presence", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<JsonElement>();
            var type = body.TryGetProperty("type", out var t) ? t.GetString()?.ToLowerInvariant() : null;

            if (type != "watching" && type != "listening")
                return Results.BadRequest(new { error = "type is required ('watching' or 'listening')" });

            if (!discord.IsConnected)
                return Results.BadRequest(new { error = "Not connected to Discord" });

            var media = watcher.CurrentMedia;
            if (media == null)
                return Results.BadRequest(new { error = "No media detected" });

            ConfigManager.Config.ActivityTypeOverride = type == "watching" ? "Watching" : "Listening";
            ConfigManager.Save();

            var title = body.TryGetProperty("title", out var tv) ? tv.GetString() : null;
            var artist = body.TryGetProperty("artist", out var av) ? av.GetString() : null;
            var album = body.TryGetProperty("album", out var alv) ? alv.GetString() : null;
            var appName = body.TryGetProperty("app_name", out var anv) ? anv.GetString() : null;
            var imageUrl = body.TryGetProperty("image_url", out var iv) ? iv.GetString() : null;

            if (title != null || artist != null || album != null || appName != null)
            {
                media = new MediaInfo
                {
                    Title = title ?? media.Title,
                    Artist = artist ?? media.Artist,
                    Album = album ?? media.Album,
                    AppName = appName ?? media.AppName,
                    State = media.State,
                    Position = media.Position,
                    Duration = media.Duration,
                    Thumbnail = media.Thumbnail
                };
            }

            discord.SetPresence(media, imageUrl);

            var showing = string.IsNullOrWhiteSpace(media.Artist)
                ? media.CleanTitle
                : $"{media.CleanTitle} — {media.Artist}";

            return Results.Ok(new { success = true, activity_type = type, showing });
        });

        app.MapDelete("/api/presence", () =>
        {
            discord.ClearPresence();
            return Results.Ok(new { success = true });
        });

        app.MapPut("/api/presence/activity", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<JsonElement>();
            var type = body.TryGetProperty("type", out var t) ? t.GetString()?.ToLowerInvariant() : null;

            if (type != "watching" && type != "listening")
                return Results.BadRequest(new { error = "type must be 'watching' or 'listening'" });

            ConfigManager.Config.ActivityTypeOverride = type == "watching" ? "Watching" : "Listening";
            ConfigManager.Save();

            return Results.Ok(new { success = true, activity_type = type });
        });

        app.MapGet("/api/status", () =>
        {
            var media = watcher.CurrentMedia;
            return Results.Ok(new
            {
                connected = discord.IsConnected,
                showing_presence = discord.IsConnected && media != null,
                activity_type_override = ConfigManager.Config.ActivityTypeOverride,
                media_detected = media != null,
                media_state = media?.State.ToString().ToLower() ?? "none"
            });
        });

        app.MapGet("/api/config", () =>
        {
            var c = ConfigManager.Config;
            return Results.Ok(new
            {
                discord_client_id = c.DiscordClientId,
                cloudinary_cloud_name = c.CloudinaryCloudName,
                cloudinary_upload_preset = c.CloudinaryUploadPreset,
                discord_webhook_url = c.DiscordWebhookUrl,
                show_album_art = c.ShowAlbumArt,
                enable_art_finder = c.EnableArtFinder,
                image_providers = c.ImageProviders,
                auto_show_on_discord = c.AutoShowOnDiscord,
                activity_type_override = c.ActivityTypeOverride
            });
        });

        app.MapPut("/api/config", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<JsonElement>();

            if (body.TryGetProperty("auto_show_on_discord", out var asv))
                ConfigManager.Config.AutoShowOnDiscord = asv.GetBoolean();
            if (body.TryGetProperty("show_album_art", out var ssv))
                ConfigManager.Config.ShowAlbumArt = ssv.GetBoolean();
            if (body.TryGetProperty("enable_art_finder", out var eaf))
                ConfigManager.Config.EnableArtFinder = eaf.GetBoolean();
            if (body.TryGetProperty("activity_type_override", out var atv))
                ConfigManager.Config.ActivityTypeOverride = atv.GetString() ?? "Auto";
            if (body.TryGetProperty("discord_client_id", out var dcv))
                ConfigManager.Config.DiscordClientId = dcv.GetString() ?? "";
            if (body.TryGetProperty("cloudinary_cloud_name", out var ccn))
                ConfigManager.Config.CloudinaryCloudName = ccn.GetString() ?? "";
            if (body.TryGetProperty("cloudinary_upload_preset", out var cup))
                ConfigManager.Config.CloudinaryUploadPreset = cup.GetString() ?? "";
            if (body.TryGetProperty("discord_webhook_url", out var dwu))
                ConfigManager.Config.DiscordWebhookUrl = dwu.GetString() ?? "";
            if (body.TryGetProperty("image_providers", out var ipv))
                ConfigManager.Config.ImageProviders = JsonSerializer.Deserialize<List<string>>(ipv.GetRawText()) ?? new() { "cloudinary", "discord", "postimage" };

            ConfigManager.Save();
            return Results.Ok(new { success = true });
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
