namespace GoodRP;

static class Program
{
    private static MediaWatcher? _mediaWatcher;
    private static DiscordManager? _discordManager;

    [STAThread]
    static async Task Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _discordManager = new DiscordManager();
        _mediaWatcher = new MediaWatcher();

        var mainForm = new MainForm(_mediaWatcher, _discordManager);

        await _mediaWatcher.StartAsync();

        if (!string.IsNullOrWhiteSpace(ConfigManager.Config.DiscordClientId))
        {
            _discordManager.Connect(ConfigManager.Config.DiscordClientId);
        }

        Application.Run(mainForm);

        _mediaWatcher?.Dispose();
        _discordManager?.Dispose();
    }
}
