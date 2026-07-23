using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoodRP;

public class DiscordManager : IDisposable
{
    private DiscordRpcClient? _client;
    private Process? _bridgeProcess;
    private HttpClient? _bridgeHttp;
    private volatile bool _connected;
    private bool _disposed;
    private bool _autoReconnect = true;
    private string? _clientId;
    private int _retryCount;
    private System.Timers.Timer? _reconnectTimer;
    private System.Timers.Timer? _statusPollTimer;
    private MediaInfo? _currentMedia;
    private string? _currentImageUrl;
    private DateTime _anchorTime;
    private TimeSpan _anchorPosition;

    private const int MaxRetryDelayMs = 30000;
    private const int BaseRetryDelayMs = 3000;
    private const int BridgePort = 19876;
    private const string BridgeBaseUrl = "http://127.0.0.1:19876";

    public bool IsConnected => _connected;
    public event Action<string>? StatusChanged;
    public event Action<string>? PresenceUpdated;

    private bool UseBridge => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public bool Connect(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return false;

        _clientId = clientId;
        _retryCount = 0;
        _autoReconnect = true;
        StopReconnectTimer();

        if (UseBridge)
            return TryConnectBridge(clientId);
        else
            return TryConnect(clientId);
    }

    private bool TryConnectBridge(string clientId)
    {
        try
        {
            StopBridge();

            var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            var bridgePath = Path.Combine(exeDir, "discord_bridge.py");
            if (!File.Exists(bridgePath))
            {
                bridgePath = Path.Combine(AppContext.BaseDirectory, "discord_bridge.py");
            }
            if (!File.Exists(bridgePath))
            {
                var srcPath = Path.Combine(AppContext.BaseDirectory, "src", "Linux", "discord_bridge.py");
                if (File.Exists(srcPath))
                    bridgePath = srcPath;
            }

            StatusChanged?.Invoke($"Starting bridge from {bridgePath}");

            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{bridgePath}\" {BridgePort}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            _bridgeProcess = Process.Start(psi);
            _bridgeHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            if (_bridgeProcess != null)
            {
                _bridgeProcess.OutputDataReceived += (_, _) => { };
                _bridgeProcess.ErrorDataReceived += (_, _) => { };
                _bridgeProcess.BeginOutputReadLine();
                _bridgeProcess.BeginErrorReadLine();
            }

            Thread.Sleep(500);

            if (_bridgeProcess != null && !_bridgeProcess.HasExited)
            {
                StatusChanged?.Invoke("Bridge process alive, polling...");
                _ = PollBridgeStatus(clientId);
                return true;
            }

            StatusChanged?.Invoke("Failed to start Discord bridge");
            return false;
        }
        catch (Exception ex)
        {
            _connected = false;
            StatusChanged?.Invoke($"Error: {ex.Message}");
            ScheduleReconnect();
            return false;
        }
    }

    private async Task PollBridgeStatus(string clientId)
    {
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(500);

            if (_disposed) return;

            try
            {
                var statusJson = await _bridgeHttp!.GetStringAsync($"{BridgeBaseUrl}/status");
                var status = JObject.Parse(statusJson);
                bool connected = status["connected"]?.Value<bool>() ?? false;
                StatusChanged?.Invoke($"Poll {i}: connected={connected}");

                if (!connected)
                {
                    var connectJson = JsonConvert.SerializeObject(new { client_id = clientId });
                    var content = new StringContent(connectJson, Encoding.UTF8, "application/json");
                    var response = await _bridgeHttp.PostAsync($"{BridgeBaseUrl}/connect", content);
                    var respBody = await response.Content.ReadAsStringAsync();
                    var resp = JObject.Parse(respBody);
                    connected = resp["connected"]?.Value<bool>() ?? false;
                    StatusChanged?.Invoke($"Connect response: {respBody}");
                }

                if (connected)
                {
                    _connected = true;
                    _retryCount = 0;
                    string username = status["username"]?.Value<string>() ?? "unknown";
                    StatusChanged?.Invoke($"Connected as {username}");
                    StartStatusPoll();
                    return;
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Poll error: {ex.Message}");
                break;
            }
        }

        _connected = false;
        StatusChanged?.Invoke("Failed to connect to Discord bridge");
        ScheduleReconnect();
    }

    private void StartStatusPoll()
    {
        _statusPollTimer?.Stop();
        _statusPollTimer?.Dispose();
        _statusPollTimer = new System.Timers.Timer(5000);
        _statusPollTimer.Elapsed += async (_, _) =>
        {
            if (_disposed || !UseBridge) return;
            try
            {
                var statusJson = await _bridgeHttp!.GetStringAsync($"{BridgeBaseUrl}/status");
                var status = JObject.Parse(statusJson);
                bool wasConnected = _connected;
                _connected = status["connected"]?.Value<bool>() ?? false;

                if (wasConnected && !_connected)
                {
                    StatusChanged?.Invoke("Connection lost");
                    _statusPollTimer?.Stop();
                    ScheduleReconnect();
                }
            }
            catch
            {
                if (_connected)
                {
                    _connected = false;
                    _statusPollTimer?.Stop();
                    StatusChanged?.Invoke("Bridge disconnected");
                    ScheduleReconnect();
                }
            }
        };
        _statusPollTimer.AutoReset = true;
        _statusPollTimer.Start();
    }

    private void StopBridge()
    {
        _statusPollTimer?.Stop();
        _statusPollTimer?.Dispose();
        _statusPollTimer = null;

        try
        {
            if (_bridgeProcess != null && !_bridgeProcess.HasExited)
            {
                _bridgeProcess.Kill();
                _bridgeProcess.WaitForExit(2000);
            }
        }
        catch { }
        _bridgeProcess = null;
        _bridgeHttp?.Dispose();
        _bridgeHttp = null;
    }

    private bool TryConnect(string clientId)
    {
        try
        {
            _client?.Dispose();

            var logger = new FileLogger(Path.Combine(AppContext.BaseDirectory, "goodrp-debug.log"));
            var pipeClient = new ManagedNamedPipeClient();

            _client = new DiscordRpcClient(clientId, -1, logger: logger, autoEvents: true, client: pipeClient);

            _client.OnReady += (sender, e) =>
            {
                _connected = true;
                _retryCount = 0;
                StatusChanged?.Invoke($"Connected as {e.User.Username}");
            };

            _client.OnConnectionFailed += (sender, e) =>
            {
                _connected = false;
                StatusChanged?.Invoke("Connection failed");
                ScheduleReconnect();
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
            ScheduleReconnect();
            return false;
        }
    }

    private void ScheduleReconnect()
    {
        if (!_autoReconnect || _disposed || string.IsNullOrEmpty(_clientId)) return;

        var delay = Math.Min(BaseRetryDelayMs * (1 << _retryCount), MaxRetryDelayMs);
        _retryCount++;

        StatusChanged?.Invoke($"Reconnecting in {delay / 1000}s...");
        StopReconnectTimer();

        _reconnectTimer = new System.Timers.Timer(delay);
        _reconnectTimer.Elapsed += (_, _) =>
        {
            StopReconnectTimer();
            if (!_disposed && _autoReconnect && !string.IsNullOrEmpty(_clientId))
                Connect(_clientId);
        };
        _reconnectTimer.AutoReset = false;
        _reconnectTimer.Start();
    }

    private void StopReconnectTimer()
    {
        _reconnectTimer?.Stop();
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;
    }

    public void Disconnect()
    {
        _autoReconnect = false;
        StopReconnectTimer();

        try
        {
            if (UseBridge)
            {
                _ = BridgeDeleteAsync("/presence");
                _connected = false;
                StopBridge();
            }
            else
            {
                _client?.ClearPresence();
                _client?.Dispose();
                _client = null;
                _connected = false;
            }

            _currentMedia = null;
            _currentImageUrl = null;
            _clientId = null;
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
        if (!_connected || _currentMedia == null)
        {
            StatusChanged?.Invoke($"UpdatePresence skipped: connected={_connected}, media={_currentMedia != null}");
            return;
        }

        try
        {
            var media = _currentMedia;

            var activityType = media.AppName.ToLower().Contains("video") ||
                               media.AppName.ToLower().Contains("movie") ||
                               media.AppName.ToLower().Contains("vlc") ||
                               media.AppName.ToLower().Contains("mpv")
                ? "watching"
                : "listening";

            var overrideStr = ConfigManager.Config.ActivityTypeOverride;
            if (overrideStr == "Listening")
                activityType = "listening";
            else if (overrideStr == "Watching")
                activityType = "watching";

            var title = media.CleanTitle;
            if (string.IsNullOrWhiteSpace(title))
                title = "Unknown Track";

            if (UseBridge)
            {
                _ = BridgeSetPresence(title, media.Artist, media.Album, media.AppName, activityType, _currentImageUrl);
            }
            else if (_client != null)
            {
                var state = FormatState(media);

                var presence = new RichPresence
                {
                    Type = activityType == "watching" ? ActivityType.Watching : ActivityType.Listening,
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
            }

            PresenceUpdated?.Invoke($"{title} - {media.Artist}");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error setting presence: {ex.Message}");
        }
    }

    private async Task BridgeSetPresence(string title, string artist, string album, string app, string type, string? imageUrl)
    {
        if (_bridgeHttp == null) return;

        try
        {
            var payload = new
            {
                title,
                artist,
                album,
                app,
                type,
                image_url = imageUrl,
            };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _bridgeHttp.PostAsync($"{BridgeBaseUrl}/presence", content);
        }
        catch { }
    }

    private async Task BridgeDeleteAsync(string path)
    {
        if (_bridgeHttp == null) return;
        try
        {
            await _bridgeHttp.DeleteAsync($"{BridgeBaseUrl}{path}");
        }
        catch { }
    }

    public void ClearPresence()
    {
        if (!_connected) return;

        try
        {
            if (UseBridge)
            {
                _ = BridgeDeleteAsync("/presence");
            }
            else if (_client != null)
            {
                _client.ClearPresence();
            }

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

        return parts.Count > 0 ? string.Join(" \u2022 ", parts) : media.AppName;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _autoReconnect = false;
        StopReconnectTimer();
        StopBridge();

        try
        {
            _client?.ClearPresence();
            _client?.Dispose();
            _client = null;
            _connected = false;
        }
        catch { }

        GC.SuppressFinalize(this);
    }
}
