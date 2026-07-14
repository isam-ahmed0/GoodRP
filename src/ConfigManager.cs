using System.Text.Json;

namespace GoodRP;

public class AppConfig
{
    // GUI version: "default" (MainForm) | "9xt" (ModernMainForm) | "avalonia" (Cross-platform)
    public string GuiVersion { get; set; } = "default";

    public string DiscordClientId { get; set; } = "";
    public bool AutoShowOnDiscord { get; set; } = false;
    public bool ShowAlbumArt { get; set; } = true;
    public string ActivityTypeOverride { get; set; } = "Auto";
    public List<string> AllowedApps { get; set; } = new();
    public List<string> IgnoredApps { get; set; } = new();
    public bool McpServerEnabled { get; set; } = false;
    public bool UseHotkeys { get; set; } = true;
    public bool UseNotifications { get; set; } = true;
    public string ShowHotkey { get; set; } = "Ctrl+Shift+G";
    public string HideHotkey { get; set; } = "Ctrl+Shift+H";

    public List<string> ImageProviders { get; set; } = new() { "telegraph" };
    public string CloudinaryCloudName { get; set; } = "";
    public string CloudinaryUploadPreset { get; set; } = "";
    public string PostImageApiKey { get; set; } = "";
    public bool EnableArtFinder { get; set; } = true;

    public string? OnMediaChangedScript { get; set; }
    public string? OnMediaStoppedScript { get; set; }
    public string? OnPlaybackStateChangedScript { get; set; }

    public int ScriptTimeoutMs { get; set; } = 10000;

    // Multiple Discord App IDs for quick-switching (9XT GUI)
    public List<DiscordAppEntry> DiscordAppIds { get; set; } = new();
    public int ActiveAppIdIndex { get; set; } = 0;
}

public class DiscordAppEntry
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
}

public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoodRP");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static AppConfig Config { get; private set; } = new();

    static ConfigManager()
    {
        Load();
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch
        {
            Config = new AppConfig();
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Silent fail on save error
        }
    }
}
