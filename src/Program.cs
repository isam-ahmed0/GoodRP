using System.Diagnostics;
using System.Runtime.InteropServices;
using GoodRP.Api;
using GoodRP.Interfaces;
using GoodRP.Mcp;

namespace GoodRP;

static class Program
{
    private static IMediaWatcher? _mediaWatcher;
    private static DiscordManager? _discordManager;

    [STAThread]
    static async Task Main(string[] args)
    {
        var mcpMode = args.Contains("--mcp");
        var apiMode = args.Contains("--api");

        if (mcpMode || apiMode)
        {
            await RunHeadlessMode(args, mcpMode, apiMode);
            return;
        }

        var guiVersion = ConfigManager.Config.GuiVersion?.ToLowerInvariant() ?? "default";
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (!isWindows || guiVersion == "avalonia")
        {
            Avalonia.AvaloniaProgram.Launch(args);
            return;
        }

#if WINDOWS
        using var mutex = new Mutex(true, "GoodRP_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            var hwnd = NativeMethods.FindWindow(null, "GoodRP - Discord Rich Presence");
            if (hwnd == IntPtr.Zero)
                hwnd = NativeMethods.FindWindow(null, "GoodRP 9XT");
            var alive = false;
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out int pid);
                if (pid != 0)
                {
                    try { alive = !Process.GetProcessById(pid).HasExited; }
                    catch { alive = false; }
                }
            }
            if (alive)
            {
                NativeMethods.ShowWindow(hwnd, 9);
                NativeMethods.SetForegroundWindow(hwnd);
                return;
            }
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _discordManager = new DiscordManager();
        _mediaWatcher = MediaWatcherFactory.Create();

        var winMediaWatcher = _mediaWatcher as MediaWatcher;

        Form mainForm = string.Equals(ConfigManager.Config.GuiVersion, "9xt", StringComparison.OrdinalIgnoreCase)
            ? new ModernMainForm(winMediaWatcher!, _discordManager)
            : new MainForm(winMediaWatcher!, _discordManager);

        LogService.Log("App", "GoodRP started");

        await _mediaWatcher.StartAsync();

        if (!string.IsNullOrWhiteSpace(ConfigManager.Config.DiscordClientId))
        {
            _discordManager.Connect(ConfigManager.Config.DiscordClientId);
        }

        System.Windows.Forms.Application.Run(mainForm);

        _mediaWatcher?.Dispose();
        _discordManager?.Dispose();
#endif
    }

    static async Task RunHeadlessMode(string[] args, bool mcpMode, bool apiMode)
    {
        Console.Error.WriteLine("[GoodRP] Starting headless mode...");

        _discordManager = new DiscordManager();
        _mediaWatcher = MediaWatcherFactory.Create();

        await _mediaWatcher.StartAsync();

        if (!string.IsNullOrWhiteSpace(ConfigManager.Config.DiscordClientId))
        {
            _discordManager.Connect(ConfigManager.Config.DiscordClientId);
            Console.Error.WriteLine("[GoodRP] Connected to Discord");
        }
        else
        {
            Console.Error.WriteLine("[GoodRP] No Discord Application ID configured");
        }

        _mediaWatcher.MediaChanged += OnMcpMediaChanged;
        _mediaWatcher.MediaStopped += OnMcpMediaStopped;

        var tasks = new List<Task>();

        if (mcpMode)
            tasks.Add(McpServer.RunAsync(_discordManager, _mediaWatcher));

        if (apiMode)
            tasks.Add(ApiServer.RunAsync(_discordManager, _mediaWatcher, args));

        await Task.WhenAll(tasks);

        _mediaWatcher.MediaChanged -= OnMcpMediaChanged;
        _mediaWatcher.MediaStopped -= OnMcpMediaStopped;
        _mediaWatcher?.Dispose();
        _discordManager?.Dispose();
    }

    private static void OnMcpMediaChanged(MediaInfo media)
    {
        if (!ConfigManager.Config.AutoShowOnDiscord) return;
        if (!_discordManager!.IsConnected) return;

        var overrideType = ConfigManager.Config.ActivityTypeOverride;
        Console.Error.WriteLine($"[GoodRP] Auto-trigger: {media.CleanTitle} - {media.Artist} (type={overrideType})");

        _discordManager.SetPresence(media);
    }

    private static void OnMcpMediaStopped()
    {
        if (!ConfigManager.Config.AutoShowOnDiscord) return;
        if (!_discordManager!.IsConnected) return;

        Console.Error.WriteLine("[GoodRP] Auto-trigger: media stopped, clearing presence");
        _discordManager.ClearPresence();
    }

    public static string GetEstimatedType(string appName)
    {
        var lower = appName.ToLowerInvariant();
        return lower.Contains("video") || lower.Contains("movie") ||
               lower.Contains("vlc") || lower.Contains("mpv")
            ? "watching"
            : "listening";
    }
}
