using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using GoodRP.Interfaces;

namespace GoodRP.Linux;

public class LinuxMediaWatcher : IMediaWatcher
{
    private readonly List<string> _knownPlayers = new();
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
        _pollTimer.Elapsed += (_, _) => PollPlayers();
        _pollTimer.Start();

        PollPlayers();
        return Task.CompletedTask;
    }

    private void PollPlayers()
    {
        try
        {
            var players = GetMprisPlayers();

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

    private static List<MprisPlayerInfo> GetMprisPlayers()
    {
        var players = new List<MprisPlayerInfo>();

        try
        {
            var output = RunCommand("dbus-send", "--session --dest=org.freedesktop.DBus --type=method_call --print-reply /org/freedesktop/DBus org.freedesktop.DBus.ListNames");
            if (string.IsNullOrEmpty(output)) return players;

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim().Trim('"');
                if (!trimmed.StartsWith("org.mpris.MediaPlayer2.")) continue;

                var info = GetPlayerInfo(trimmed);
                if (info != null)
                    players.Add(info);
            }
        }
        catch { }

        return players;
    }

    private static MprisPlayerInfo? GetPlayerInfo(string busName)
    {
        try
        {
            var escaped = busName.Replace(".", "_").Replace("/", "_");

            var status = RunCommand("dbus-send",
                $"--session --dest={busName} --type=method_call --print-reply " +
                $"/org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get " +
                $"string:org.mpris.MediaPlayer2.Player string:PlaybackStatus");

            var playbackStatus = ExtractDBusString(status);

            var metadata = RunCommand("dbus-send",
                $"--session --dest={busName} --type=method_call --print-reply " +
                $"/org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get " +
                $"string:org.mpris.MediaPlayer2.Player string:Metadata");

            var metaDict = ParseMetadata(metadata);

            var positionReply = RunCommand("dbus-send",
                $"--session --dest={busName} --type=method_call --print-reply " +
                $"/org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get " +
                $"string:org.mpris.MediaPlayer2.Player string:Position");

            var positionUs = ExtractDBusLong(positionReply);
            var position = TimeSpan.FromTicks(positionUs * 10); // microseconds to TimeSpan

            var durationUs = 0L;
            if (metaDict.TryGetValue("mpris:length", out var lengthStr) && long.TryParse(lengthStr, out var len))
                durationUs = len;

            return new MprisPlayerInfo
            {
                BusName = busName,
                Status = playbackStatus ?? "Stopped",
                Title = metaDict.TryGetValue("xesam:title", out var t) ? t : null,
                Artist = metaDict.TryGetValue("xesam:artist", out var a) ? a : null,
                Album = metaDict.TryGetValue("xesam:album", out var al) ? al : null,
                ArtUrl = metaDict.TryGetValue("mpris:artUrl", out var art) ? art : null,
                Position = position,
                Duration = TimeSpan.FromTicks(durationUs * 10)
            };
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> ParseMetadata(string? dbusOutput)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(dbusOutput)) return result;

        var lines = dbusOutput.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("string \""))
            {
                var value = trimmed.Substring(8, trimmed.Length - 9);
                var eqIndex = value.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = value[..eqIndex].Trim();
                    var val = value[(eqIndex + 1)..].Trim();
                    result[key] = val;
                }
            }
        }

        return result;
    }

    private static string? ExtractDBusString(string? output)
    {
        if (string.IsNullOrEmpty(output)) return null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("string \""))
            {
                return trimmed.Substring(8, trimmed.Length - 9);
            }
        }
        return null;
    }

    private static long ExtractDBusLong(string? output)
    {
        if (string.IsNullOrEmpty(output)) return 0;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("int64 "))
            {
                if (long.TryParse(trimmed.Substring(6), out var val))
                    return val;
            }
        }
        return 0;
    }

    private static string? RunCommand(string command, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return output;
        }
        catch
        {
            return null;
        }
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
