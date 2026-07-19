using Tmds.DBus.Protocol;
using GoodRP.Interfaces;

namespace GoodRP.Linux;

public class LinuxMediaWatcher : IMediaWatcher
{
    private System.Timers.Timer? _pollTimer;
    private bool _disposed;

    public event Action<MediaInfo>? MediaChanged;
    public event Action? MediaStopped;
    public event Action<MediaPlaybackState>? PlaybackStateChanged;
    public event Action? TimelineChanged;

    public MediaInfo? CurrentMedia { get; private set; }

    public Task StartAsync()
    {
        _pollTimer = new System.Timers.Timer(1000);
        _pollTimer.Elapsed += async (_, _) => await PollPlayersAsync();
        _pollTimer.Start();

        _ = PollPlayersAsync();
        return Task.CompletedTask;
    }

    private async Task PollPlayersAsync()
    {
        try
        {
            var players = await GetMprisPlayersAsync();

            var playingPlayer = players.FirstOrDefault(p => p.Status == "Playing")
                                ?? players.FirstOrDefault(p => p.Status == "Paused");

            if (playingPlayer == null)
            {
                if (CurrentMedia != null)
                {
                    CurrentMedia = null;
                    MediaStopped?.Invoke();
                }
                return;
            }

            var newMedia = new MediaInfo
            {
                Title = playingPlayer.Title ?? "",
                Artist = playingPlayer.Artist ?? "",
                Album = playingPlayer.Album ?? "",
                AppName = FormatPlayerName(playingPlayer.BusName),
                State = playingPlayer.Status == "Playing" ? MediaPlaybackState.Playing : MediaPlaybackState.Paused,
                Position = playingPlayer.Position,
                Duration = playingPlayer.Duration,
                ArtworkUrl = playingPlayer.ArtUrl
            };

            if (CurrentMedia == null ||
                CurrentMedia.Title != newMedia.Title ||
                CurrentMedia.Artist != newMedia.Artist)
            {
                CurrentMedia = newMedia;
                MediaChanged?.Invoke(newMedia);
            }
            else if (CurrentMedia.State != newMedia.State)
            {
                CurrentMedia.State = newMedia.State;
                PlaybackStateChanged?.Invoke(newMedia.State);
            }
            else
            {
                CurrentMedia.Position = newMedia.Position;
                CurrentMedia.Duration = newMedia.Duration;
                TimelineChanged?.Invoke();
            }
        }
        catch { }
    }

    private static string FormatPlayerName(string busName)
    {
        return busName.Replace("org.mpris.MediaPlayer2.", "");
    }

    private static async Task<List<MprisPlayerInfo>> GetMprisPlayersAsync()
    {
        var players = new List<MprisPlayerInfo>();

        try
        {
            var connection = new DBusConnection(DBusAddress.Session!);
            await connection.ConnectAsync();

            string[] busNames = await ListNamesAsync(connection);

            foreach (var busName in busNames)
            {
                if (!busName.StartsWith("org.mpris.MediaPlayer2.")) continue;

                var info = await GetPlayerInfoAsync(connection, busName);
                if (info != null)
                    players.Add(info);
            }

            connection.Dispose();
        }
        catch { }

        return players;
    }

    private static MessageBuffer BuildListNamesCall(DBusConnection connection)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: "org.freedesktop.DBus",
            path: "/org/freedesktop/DBus",
            @interface: "org.freedesktop.DBus",
            member: "ListNames");
        return writer.CreateMessage();
    }

    private static MessageBuffer BuildPropertyGetCall(DBusConnection connection, string busName, string property)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: busName,
            path: "/org/mpris/MediaPlayer2",
            @interface: "org.freedesktop.DBus.Properties",
            signature: "ss",
            member: "Get");
        writer.WriteString("org.mpris.MediaPlayer2.Player");
        writer.WriteString(property);
        return writer.CreateMessage();
    }

    private static async Task<string[]> ListNamesAsync(DBusConnection connection)
    {
        var msg = BuildListNamesCall(connection);

        return await connection.CallMethodAsync(
            msg,
            (Message message, object? state) =>
            {
                var reader = message.GetBodyReader();
                var names = new List<string>();
                var arrayEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(arrayEnd))
                {
                    names.Add(reader.ReadString());
                }
                return names.ToArray();
            });
    }

    private static async Task<MprisPlayerInfo?> GetPlayerInfoAsync(DBusConnection connection, string busName)
    {
        try
        {
            var status = await GetPropertyStringAsync(connection, busName, "PlaybackStatus");
            var metadata = await GetMetadataAsync(connection, busName);
            var positionUs = await GetPropertyLongAsync(connection, busName, "Position");

            var durationUs = 0L;
            if (metadata.TryGetValue("mpris:length", out var lengthStr) && long.TryParse(lengthStr, out var len))
                durationUs = len;

            return new MprisPlayerInfo
            {
                BusName = busName,
                Status = status ?? "Stopped",
                Title = metadata.TryGetValue("xesam:title", out var t) ? t : null,
                Artist = metadata.TryGetValue("xesam:artist", out var a) ? a : null,
                Album = metadata.TryGetValue("xesam:album", out var al) ? al : null,
                ArtUrl = metadata.TryGetValue("mpris:artUrl", out var art) ? art : null,
                Position = TimeSpan.FromTicks(positionUs * 10),
                Duration = TimeSpan.FromTicks(durationUs * 10)
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetPropertyStringAsync(DBusConnection connection, string busName, string property)
    {
        try
        {
            var msg = BuildPropertyGetCall(connection, busName, property);

            return await connection.CallMethodAsync(
                msg,
                (Message message, object? state) =>
                {
                    var reader = message.GetBodyReader();
                    var variant = reader.ReadVariantValue();
                    return variant.GetString();
                });
        }
        catch { return null; }
    }

    private static async Task<long> GetPropertyLongAsync(DBusConnection connection, string busName, string property)
    {
        try
        {
            var msg = BuildPropertyGetCall(connection, busName, property);

            return await connection.CallMethodAsync(
                msg,
                (Message message, object? state) =>
                {
                    var reader = message.GetBodyReader();
                    var variant = reader.ReadVariantValue();
                    return variant.GetInt64();
                });
        }
        catch { return 0; }
    }

    private static async Task<Dictionary<string, string>> GetMetadataAsync(DBusConnection connection, string busName)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var msg = BuildPropertyGetCall(connection, busName, "Metadata");

            var dict = await connection.CallMethodAsync(
                msg,
                (Message message, object? state) =>
                {
                    var reader = message.GetBodyReader();
                    var variant = reader.ReadVariantValue();
                    return variant.GetDictionary<string, VariantValue>();
                });

            foreach (var kvp in dict)
            {
                var val = kvp.Value;
                result[kvp.Key] = val.Type switch
                {
                    VariantValueType.String => val.GetString() ?? "",
                    VariantValueType.Int32 => val.GetInt32().ToString(),
                    VariantValueType.Int64 => val.GetInt64().ToString(),
                    VariantValueType.Byte => val.GetByte().ToString(),
                    _ => val.ToString() ?? ""
                };
            }
        }
        catch { }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollTimer?.Stop();
        _pollTimer?.Dispose();

        GC.SuppressFinalize(this);
    }
}

internal class MprisPlayerInfo
{
    public string BusName { get; set; } = "";
    public string Status { get; set; } = "Stopped";
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? ArtUrl { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}
