using System.Text.Json;

namespace GoodRP;

public class AppConfig
{
    public string DiscordClientId { get; set; } = "";
    public string ImgurClientId { get; set; } = "";
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
