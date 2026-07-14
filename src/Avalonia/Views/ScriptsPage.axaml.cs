using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;

namespace GoodRP.Avalonia.Views;

public partial class ScriptsPage : global::Avalonia.Controls.UserControl
{
    public ScriptsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        TxtMediaChanged.Text = ConfigManager.Config.OnMediaChangedScript ?? "";
        TglMediaChanged.IsChecked = !string.IsNullOrEmpty(ConfigManager.Config.OnMediaChangedScript);

        TxtMediaStopped.Text = ConfigManager.Config.OnMediaStoppedScript ?? "";
        TglMediaStopped.IsChecked = !string.IsNullOrEmpty(ConfigManager.Config.OnMediaStoppedScript);

        TxtStateChanged.Text = ConfigManager.Config.OnPlaybackStateChangedScript ?? "";
        TglStateChanged.IsChecked = !string.IsNullOrEmpty(ConfigManager.Config.OnPlaybackStateChangedScript);

        TxtTimeout.Text = ConfigManager.Config.ScriptTimeoutMs.ToString();
    }

    private async void BrowseMediaChanged_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickScriptFile();
        if (path != null) TxtMediaChanged.Text = path;
    }

    private async void BrowseMediaStopped_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickScriptFile();
        if (path != null) TxtMediaStopped.Text = path;
    }

    private async void BrowseStateChanged_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickScriptFile();
        if (path != null) TxtStateChanged.Text = path;
    }

    private async Task<string?> PickScriptFile()
    {
        var topLevel = global::Avalonia.Controls.TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Script",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("All scripts") { Patterns = new[] { "*.ps1", "*.py", "*.bat", "*.js", "*.grp" } },
                new FilePickerFileType("PowerShell") { Patterns = new[] { "*.ps1" } },
                new FilePickerFileType("Python") { Patterns = new[] { "*.py" } },
                new FilePickerFileType("Batch") { Patterns = new[] { "*.bat" } },
                new FilePickerFileType("JavaScript") { Patterns = new[] { "*.js" } },
                new FilePickerFileType("GoodRP Script") { Patterns = new[] { "*.grp" } }
            }
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        SaveSettings();
    }

    private void SaveSettings()
    {
        ConfigManager.Config.OnMediaChangedScript = TglMediaChanged.IsChecked == true ? TxtMediaChanged.Text : null;
        ConfigManager.Config.OnMediaStoppedScript = TglMediaStopped.IsChecked == true ? TxtMediaStopped.Text : null;
        ConfigManager.Config.OnPlaybackStateChangedScript = TglStateChanged.IsChecked == true ? TxtStateChanged.Text : null;

        if (int.TryParse(TxtTimeout.Text, out var timeout))
            ConfigManager.Config.ScriptTimeoutMs = timeout;

        ConfigManager.Save();
    }
}
