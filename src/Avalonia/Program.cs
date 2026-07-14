using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace GoodRP.Avalonia;

public static class AvaloniaProgram
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void Launch(string[] args)
    {
        using var mutex = new System.Threading.Mutex(true, "GoodRP_Avalonia_SingleInstance", out bool createdNew);
        if (!createdNew) return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}
