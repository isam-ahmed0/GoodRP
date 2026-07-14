using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GoodRP.Avalonia.ViewModels;
using GoodRP.Avalonia.Views;

namespace GoodRP.Avalonia;

public partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                vm.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
