using Avalonia.Interactivity;
using GoodRP.Avalonia.ViewModels;

namespace GoodRP.Avalonia.Views;

public partial class ConnectionsPage : global::Avalonia.Controls.UserControl
{
    public ConnectionsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is MainViewModel vm)
            vm.RefreshAppIds();
    }

    private void Connect_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && AppIdList.SelectedItem is DiscordAppEntry entry)
            vm.ConnectSelectedAppId(entry);
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && AppIdList.SelectedIndex >= 0)
            vm.RemoveAppId(AppIdList.SelectedIndex);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddAppId(TxtName.Text ?? "", TxtId.Text ?? "");
            TxtName.Text = "";
            TxtId.Text = "";
        }
    }
}
