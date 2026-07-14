using Avalonia.Interactivity;
using GoodRP.Avalonia.ViewModels;

namespace GoodRP.Avalonia.Views;

public partial class HomePage : global::Avalonia.Controls.UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void Show_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ShowOnDiscord();
    }

    private void Hide_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.HideFromDiscord();
    }
}
