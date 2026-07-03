using DiscordRPC;

namespace GoodRP;

public class DiscordManager : IDisposable
{
    private DiscordRpcClient? _client;
    private bool _connected;
    private bool _disposed;

    public bool IsConnected => _connected;
    public event Action<string>? StatusChanged;
    public event Action<string>? PresenceUpdated;

    public bool Connect(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return false;

        try
        {
            _client?.Dispose();
            _client = new DiscordRpcClient(clientId, -1);

            _client.OnReady += (sender, e) =>
            {
                _connected = true;
                StatusChanged?.Invoke($"Connected as {e.User.Username}");
            };

            _client.OnConnectionFailed += (sender, e) =>
            {
                _connected = false;
                StatusChanged?.Invoke("Connection failed");
            };

            _client.OnError += (sender, e) =>
            {
                StatusChanged?.Invoke($"Error: {e.Message}");
            };

            _client.Initialize();
            return true;
        }
        catch (Exception ex)
        {
            _connected = false;
            StatusChanged?.Invoke($"Error: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            _client?.ClearPresence();
            _client?.Dispose();
            _client = null;
            _connected = false;
            StatusChanged?.Invoke("Disconnected");
        }
        catch { }
    }

    public void SetPresence(MediaInfo media, string? imageUrl = null)
    {
        if (_client == null || !_connected) return;

        try
        {
            var activityType = media.AppName.ToLower().Contains("video") ||
                               media.AppName.ToLower().Contains("movie") ||
                               media.AppName.ToLower().Contains("vlc") ||
                               media.AppName.ToLower().Contains("mpv")
                ? ActivityType.Watching
                : ActivityType.Listening;

            var details = media.Title;
            if (string.IsNullOrWhiteSpace(details))
                details = "Unknown Track";

            var state = FormatState(media);

            var presence = new RichPresence
            {
                Type = activityType,
                StatusDisplay = StatusDisplayType.State,
                Details = details,
                State = state,
                Assets = new Assets(),
                Timestamps = new Timestamps()
            };

            if (media.Duration.TotalSeconds > 0)
            {
                var startTime = DateTime.UtcNow - media.Position;
                presence.Timestamps.Start = startTime;
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                presence.Assets.LargeImageUrl = imageUrl;
                presence.Assets.LargeImageText = media.Title;
            }
            else
            {
                presence.Assets.LargeImageKey = "goodrp_logo";
                presence.Assets.LargeImageText = "GoodRP";
            }

            presence.Assets.SmallImageText = media.AppName;

            _client.SetPresence(presence);
            PresenceUpdated?.Invoke($"{media.Title} - {media.Artist}");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error setting presence: {ex.Message}");
        }
    }

    public void ClearPresence()
    {
        if (_client == null || !_connected) return;

        try
        {
            _client.ClearPresence();
            PresenceUpdated?.Invoke("");
        }
        catch { }
    }

    private static string FormatState(MediaInfo media)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(media.Artist))
            parts.Add(media.Artist);

        if (!string.IsNullOrWhiteSpace(media.Album))
            parts.Add(media.Album);

        return parts.Count > 0 ? string.Join(" • ", parts) : media.AppName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Disconnect();
        GC.SuppressFinalize(this);
    }
}
