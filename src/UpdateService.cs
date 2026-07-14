using System.Net.Http;
using System.Text.Json;

namespace GoodRP;

public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Notes { get; set; } = "";
}

public static class UpdateService
{
    private static readonly HttpClient _http = new();

    private const string ApiUrl = "https://api.github.com/repos/isam-ahmed0/GoodRP/releases/latest";

    static UpdateService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoodRP");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public static string CurrentVersion =>
        Normalize(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0");

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var version = Normalize(tag);
            if (!IsNewer(version, CurrentVersion))
                return null;

            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

            string? url = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.GetProperty("name").GetString() ?? "";
                    if (name.Equals("GoodRP-Setup.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        url = a.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            if (url == null) return null;

            return new UpdateInfo { Version = version, DownloadUrl = url, Notes = notes };
        }
        catch
        {
            return null;
        }
    }

    public static async Task DownloadAndRunAsync(string url, Action<string>? onStatus = null)
    {
        var temp = Path.Combine(Path.GetTempPath(), "GoodRP-Setup.exe");
        onStatus?.Invoke("Downloading update...");
        using var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        await using (var fs = File.Create(temp))
            await resp.Content.CopyToAsync(fs);

        onStatus?.Invoke("Launching installer...");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = temp,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /SUPPRESSMSGBOXES",
            UseShellExecute = true
        });
        System.Threading.Thread.Sleep(1500);
    }

    private static string Normalize(string v) => v.Trim().TrimStart('v', 'V');

    private static bool IsNewer(string remote, string local)
    {
        if (!Version.TryParse(remote, out var r) || !Version.TryParse(local, out var l))
            return string.Compare(remote, local, StringComparison.OrdinalIgnoreCase) > 0;
        return r > l;
    }
}
