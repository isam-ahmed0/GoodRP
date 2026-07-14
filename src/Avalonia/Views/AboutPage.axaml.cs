using Avalonia.Interactivity;

namespace GoodRP.Avalonia.Views;

public partial class AboutPage : global::Avalonia.Controls.UserControl
{
    private UpdateInfo? _pendingUpdate;

    public AboutPage()
    {
        InitializeComponent();
        _ = CheckForUpdatesOnStartup();
    }

    private async Task CheckForUpdatesOnStartup()
    {
        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update != null)
            {
                _pendingUpdate = update;
                UpdateStatus.Text = $"Update available: v{update.Version}";
            }
        }
        catch { }
    }

    private async void CheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (_pendingUpdate != null)
        {
            BtnCheckUpdate.IsEnabled = false;
            UpdateStatus.Text = "Downloading update...";
            try
            {
                await UpdateService.DownloadAndRunAsync(_pendingUpdate.DownloadUrl);
                if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                    lifetime.Shutdown();
            }
            catch (Exception ex)
            {
                UpdateStatus.Text = $"Download failed: {ex.Message}";
                BtnCheckUpdate.IsEnabled = true;
            }
        }
        else
        {
            BtnCheckUpdate.IsEnabled = false;
            UpdateStatus.Text = "Checking for updates...";
            try
            {
                var update = await UpdateService.CheckForUpdateAsync();
                if (update != null)
                {
                    _pendingUpdate = update;
                    UpdateStatus.Text = $"Update available: v{update.Version} -- click to install";
                    BtnCheckUpdate.Content = "Install update";
                    BtnCheckUpdate.IsEnabled = true;
                }
                else
                {
                    UpdateStatus.Text = "You are up to date.";
                    BtnCheckUpdate.IsEnabled = true;
                }
            }
            catch
            {
                UpdateStatus.Text = "Failed to check for updates.";
                BtnCheckUpdate.IsEnabled = true;
            }
        }
    }
}
