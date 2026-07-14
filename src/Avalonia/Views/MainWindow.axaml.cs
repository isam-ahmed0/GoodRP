using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace GoodRP.Avalonia.Views;

public partial class MainWindow : Window
{
    private TextBlock? _activeNav;

    public MainWindow()
    {
        InitializeComponent();
        _activeNav = NavHome;
        if (_activeNav != null) _activeNav.Foreground = global::Avalonia.Media.Brushes.White;
    }

    private void Nav_PointerEntered(object? sender, global::Avalonia.Input.PointerEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            var text = border.GetVisualChildren().OfType<TextBlock>().FirstOrDefault();
            if (text != null && text != _activeNav)
                text.Foreground = global::Avalonia.Media.Brushes.White;
        }
    }

    private void Nav_PointerExited(object? sender, global::Avalonia.Input.PointerEventArgs e)
    {
        if (sender is Border border && border.Tag is string tag)
        {
            var text = border.GetVisualChildren().OfType<TextBlock>().FirstOrDefault();
            if (text != null && text != _activeNav)
                text.Foreground = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#B9BBBE"));
        }
    }

    private void Nav_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string page)
        {
            ShowPage(page);
        }
    }

    private void ShowPage(string page)
    {
        HomePage.IsVisible = page == "Home";
        ConnectionsPage.IsVisible = page == "Connections";
        ScriptsPage.IsVisible = page == "Scripts";
        LogsPage.IsVisible = page == "Logs";
        SettingsPage.IsVisible = page == "Settings";
        AboutPage.IsVisible = page == "About";

        if (_activeNav != null)
            _activeNav.Foreground = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#B9BBBE"));

        var navBorder = this.GetVisualChildren()
            .OfType<Border>()
            .FirstOrDefault(b => b.Tag?.ToString() == page);

        _activeNav = navBorder?.GetVisualChildren().OfType<TextBlock>().FirstOrDefault();
        if (_activeNav != null)
            _activeNav.Foreground = global::Avalonia.Media.Brushes.White;

        if (DataContext is ViewModels.MainViewModel vm)
            vm.CurrentPage = page;
    }
}
