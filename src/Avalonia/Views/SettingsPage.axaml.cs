using Avalonia.Interactivity;
using GoodRP.Avalonia.ViewModels;
using System.Collections.ObjectModel;

namespace GoodRP.Avalonia.Views;

public partial class SettingsPage : global::Avalonia.Controls.UserControl
{
    public ObservableCollection<string> AllowedApps { get; } = new();
    public ObservableCollection<string> IgnoredApps { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var gui = ConfigManager.Config.GuiVersion.ToLowerInvariant();
        RbClassic.IsChecked = gui == "default" || gui == "classic";
        Rb9xt.IsChecked = gui == "9xt";
        RbAvalonia.IsChecked = gui == "avalonia";

        RbAuto.IsChecked = ConfigManager.Config.ActivityTypeOverride == "Auto";
        RbListening.IsChecked = ConfigManager.Config.ActivityTypeOverride == "Listening";
        RbWatching.IsChecked = ConfigManager.Config.ActivityTypeOverride == "Watching";

        TglAutoShow.IsChecked = ConfigManager.Config.AutoShowOnDiscord;
        TglShowArt.IsChecked = ConfigManager.Config.ShowAlbumArt;
        TglArtFinder.IsChecked = ConfigManager.Config.EnableArtFinder;
        TglMcp.IsChecked = ConfigManager.Config.McpServerEnabled;
        TglNotify.IsChecked = ConfigManager.Config.UseNotifications;
        TglHotkeys.IsChecked = ConfigManager.Config.UseHotkeys;

        foreach (var app in ConfigManager.Config.AllowedApps)
            AllowedApps.Add(app);
        foreach (var app in ConfigManager.Config.IgnoredApps)
            IgnoredApps.Add(app);
    }

    private void SaveSettings()
    {
        ConfigManager.Config.GuiVersion = RbClassic.IsChecked == true ? "default" :
                                           Rb9xt.IsChecked == true ? "9xt" : "avalonia";

        ConfigManager.Config.ActivityTypeOverride = RbAuto.IsChecked == true ? "Auto" :
                                                     RbListening.IsChecked == true ? "Listening" : "Watching";

        ConfigManager.Config.AutoShowOnDiscord = TglAutoShow.IsChecked == true;
        ConfigManager.Config.ShowAlbumArt = TglShowArt.IsChecked == true;
        ConfigManager.Config.EnableArtFinder = TglArtFinder.IsChecked == true;
        ConfigManager.Config.McpServerEnabled = TglMcp.IsChecked == true;
        ConfigManager.Config.UseNotifications = TglNotify.IsChecked == true;
        ConfigManager.Config.UseHotkeys = TglHotkeys.IsChecked == true;

        ConfigManager.Config.AllowedApps = new System.Collections.Generic.List<string>(AllowedApps);
        ConfigManager.Config.IgnoredApps = new System.Collections.Generic.List<string>(IgnoredApps);

        ConfigManager.Save();
    }

    private void AddAllowed_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtAllowed.Text))
        {
            AllowedApps.Add(TxtAllowed.Text);
            TxtAllowed.Text = "";
            SaveSettings();
        }
    }

    private void RemoveAllowed_Click(object? sender, RoutedEventArgs e)
    {
        if (AllowedList.SelectedIndex >= 0)
        {
            AllowedApps.RemoveAt(AllowedList.SelectedIndex);
            SaveSettings();
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        SaveSettings();
    }
}
