using Avalonia.Interactivity;
using GoodRP.Avalonia.ViewModels;

namespace GoodRP.Avalonia.Views;

public partial class LogsPage : global::Avalonia.Controls.UserControl
{
    public LogsPage()
    {
        InitializeComponent();
    }

    private void Filter_Changed(object? sender, global::Avalonia.Controls.SelectionChangedEventArgs e)
    {
        RefreshLogs();
    }

    private void Clear_Click(object? sender, RoutedEventArgs e)
    {
        LogService.Clear();
        if (DataContext is MainViewModel vm)
            vm.LogEntries.Clear();
    }

    private void RefreshLogs()
    {
        if (DataContext is not MainViewModel vm) return;

        var filter = LogFilter.SelectedItem is global::Avalonia.Controls.ComboBoxItem item ? item.Content?.ToString() : "All";
        var entries = LogService.Filter(filter);

        vm.LogEntries.Clear();
        foreach (var entry in entries)
        {
            var status = entry.Success ? "OK" : "FAIL";
            vm.LogEntries.Add($"{entry.Timestamp:HH:mm:ss} | {entry.Type} | {entry.Message} | {status}");
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RefreshLogs();
    }
}
