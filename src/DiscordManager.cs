using DiscordRPC;

namespace GoodRP;

public class DiscordManager : IDisposable
{
    private DiscordRpcClient? _client;
    private bool _connected;
    private bool _disposed;
    private MediaInfo? _currentMedia;
    private string? _currentImageUrl;
    private DateTime _anchorTime;
    private TimeSpan _anchorPosition;

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
            _currentMedia = null;
            _currentImageUrl = null;
            StatusChanged?.Invoke("Disconnected");
        }
        catch { }
    }

    public void SetPresence(MediaInfo media, string? imageUrl = null)
    {
        _currentMedia = media;
        _currentImageUrl = imageUrl;
        _anchorTime = DateTime.UtcNow;
        _anchorPosition = media.Position;
        UpdatePresence();
    }

    public void RefreshPresence()
    {
        if (_currentMedia != null)
            UpdatePresence();
    }

    private void UpdatePresence()
    {
        if (_client == null || !_connected || _currentMedia == null) return;

        try
        {
            var media = _currentMedia;

            var activityType = media.AppName.ToLower().Contains("video") ||
                               media.AppName.ToLower().Contains("movie") ||
                               media.AppName.ToLower().Contains("vlc") ||
                               media.AppName.ToLower().Contains("mpv")
                ? ActivityType.Watching
                : ActivityType.Listening;

            var overrideStr = ConfigManager.Config.ActivityTypeOverride;
            if (overrideStr == "Listening")
                activityType = ActivityType.Listening;
            else if (overrideStr == "Watching")
                activityType = ActivityType.Watching;

            var title = media.CleanTitle;
            if (string.IsNullOrWhiteSpace(title))
                title = "Unknown Track";

            var state = FormatState(media);

            var presence = new RichPresence
            {
                Name = title,
                Type = activityType,
                Details = title,
                State = state,
                Assets = new Assets()
            };

            if (media.Duration.TotalSeconds > 0 && media.State == MediaPlaybackState.Playing)
            {
                var elapsed = DateTime.UtcNow - _anchorTime + _anchorPosition;
                if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
                if (elapsed > media.Duration) elapsed = media.Duration;

                presence.Timestamps = new Timestamps
                {
                    Start = DateTime.UtcNow - elapsed,
                    End = DateTime.UtcNow - elapsed + media.Duration
                };
            }

            if (!string.IsNullOrWhiteSpace(_currentImageUrl))
            {
                presence.Assets.LargeImageKey = _currentImageUrl;
                presence.Assets.LargeImageText = title;
            }

            presence.Assets.SmallImageText = media.AppName;

            _client.SetPresence(presence);
            PresenceUpdated?.Invoke($"{title} - {media.Artist}");
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
            _currentMedia = null;
            _currentImageUrl = null;
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
