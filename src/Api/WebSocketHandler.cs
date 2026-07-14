using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using GoodRP.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GoodRP.Api;

public class WebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly IMediaWatcher _watcher;
    private readonly DiscordManager _discord;

    public WebSocketHandler(IMediaWatcher watcher, DiscordManager discord)
    {
        _watcher = watcher;
        _discord = discord;

        _watcher.MediaChanged += OnMediaChanged;
        _watcher.MediaStopped += OnMediaStopped;
        _watcher.PlaybackStateChanged += OnPlaybackStateChanged;
        _discord.StatusChanged += OnDiscordStatusChanged;
    }

    public async Task HandleAsync(HttpContext context, WebSocket webSocket)
    {
        var id = Guid.NewGuid().ToString();
        _clients[id] = webSocket;

        await BroadcastAsync(new
        {
            type = "connected",
            client_id = id,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        var buffer = new byte[1024];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch
        {
            // Client disconnected
        }
        finally
        {
            _clients.TryRemove(id, out _);
            try { await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
            webSocket.Dispose();
        }
    }

    private void OnMediaChanged(MediaInfo media)
    {
        _ = BroadcastAsync(new
        {
            type = "media.changed",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            title = media.Title,
            clean_title = media.CleanTitle,
            artist = media.Artist,
            album = media.Album,
            app_name = media.AppName,
            state = media.State.ToString().ToLower(),
            position_seconds = (int)media.Position.TotalSeconds,
            duration_seconds = (int)media.Duration.TotalSeconds,
            estimated_type = Program.GetEstimatedType(media.AppName)
        });
    }

    private void OnMediaStopped()
    {
        _ = BroadcastAsync(new
        {
            type = "media.stopped",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }

    private void OnPlaybackStateChanged(MediaPlaybackState state)
    {
        _ = BroadcastAsync(new
        {
            type = "playback.state",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            state = state.ToString().ToLower()
        });
    }

    private void OnDiscordStatusChanged(string status)
    {
        _ = BroadcastAsync(new
        {
            type = "discord.status",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            status = status,
            connected = _discord.IsConnected
        });
    }

    public async Task BroadcastAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var client in _clients.Values)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    // Ignore send failures
                }
            }
        }
    }
}
