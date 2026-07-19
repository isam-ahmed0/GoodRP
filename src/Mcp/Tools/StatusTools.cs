using System.ComponentModel;
using System.Text.Json;
using GoodRP.Interfaces;
using ModelContextProtocol.Server;

namespace GoodRP.Mcp.Tools;

[McpServerToolType]
public class StatusTools
{
    private readonly DiscordManager _discordManager;
    private readonly IMediaWatcher _mediaWatcher;

    public StatusTools(DiscordManager discordManager, IMediaWatcher mediaWatcher)
    {
        _discordManager = discordManager;
        _mediaWatcher = mediaWatcher;
    }

    [McpServerTool, Description("Get Discord connection and media detection status")]
    public string GetStatus()
    {
        var media = _mediaWatcher.CurrentMedia;

        return JsonSerializer.Serialize(new
        {
            connected = _discordManager.IsConnected,
            showing_presence = _discordManager.IsConnected && media != null,
            activity_type_override = ConfigManager.Config.ActivityTypeOverride,
            media_detected = media != null,
            media_state = media?.State.ToString().ToLower() ?? "none"
        });
    }
}
