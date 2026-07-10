using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GoodRP;

public static class GrpExecutor
{
    public static async Task RunAsync(GrpScript script, MediaInfo media, string? imageUrl)
    {
        if (!string.IsNullOrEmpty(script.WebhookUrl))
        {
            await SendWebhookAsync(script, media, imageUrl);
        }

        if (script.Log && !string.IsNullOrEmpty(script.LogFile))
        {
            WriteLog(script, media);
        }

        if (!string.IsNullOrEmpty(script.Language) && !string.IsNullOrWhiteSpace(script.CodeBody))
        {
            await RunInlineCodeAsync(script, media, imageUrl);
        }
    }

    private static string ApplyTemplate(string template, MediaInfo media, string? imageUrl)
    {
        return template
            .Replace("{{title}}", media.CleanTitle ?? "")
            .Replace("{{artist}}", media.Artist ?? "")
            .Replace("{{album}}", media.Album ?? "")
            .Replace("{{app}}", media.AppName ?? "")
            .Replace("{{state}}", media.State.ToString())
            .Replace("{{position}}", media.Position.ToString())
            .Replace("{{duration}}", media.Duration.ToString())
            .Replace("{{imageUrl}}", imageUrl ?? "");
    }

    private static bool IsVideo(MediaInfo media)
    {
        var app = (media.AppName ?? "").ToLowerInvariant();
        return app.Contains("video") || app.Contains("movie") ||
               app.Contains("vlc") || app.Contains("mpv");
    }

    private static async Task SendWebhookAsync(GrpScript script, MediaInfo media, string? imageUrl)
    {
        var message = ApplyTemplate(script.Template ?? "", media, imageUrl);
        using var http = new HttpClient();

        string json;
        if (script.Embed)
        {
            var color = IsVideo(media) ? 15158332 : 5814783;
            var embed = new Dictionary<string, object?>
            {
                ["title"] = media.CleanTitle,
                ["description"] = message,
                ["color"] = color,
                ["footer"] = new Dictionary<string, string> { ["text"] = $"via {media.AppName}" }
            };
            if (!string.IsNullOrEmpty(imageUrl))
                embed["thumbnail"] = new Dictionary<string, string> { ["url"] = imageUrl };

            var payload = new Dictionary<string, object?> { ["embeds"] = new[] { embed } };
            json = JsonSerializer.Serialize(payload);
        }
        else
        {
            var payload = new Dictionary<string, string> { ["content"] = message };
            json = JsonSerializer.Serialize(payload);
        }

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await http.PostAsync(script.WebhookUrl, content);
        }
        catch
        {
            // Ignore webhook errors
        }
    }

    private static void WriteLog(GrpScript script, MediaInfo media)
    {
        var line = ApplyTemplate(script.Template ?? "{{title}} - {{artist}}", media, null);
        try
        {
            var dir = Path.GetDirectoryName(script.LogFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir!);
            File.AppendAllText(script.LogFile!, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {line}{Environment.NewLine}");
        }
        catch
        {
            // Ignore log errors
        }
    }

    private static async Task RunInlineCodeAsync(GrpScript script, MediaInfo media, string? imageUrl)
    {
        var ext = script.Language switch
        {
            "ps1" => ".ps1",
            "py" => ".py",
            "bat" => ".bat",
            "js" => ".js",
            _ => ".tmp"
        };

        var tempFile = Path.Combine(Path.GetTempPath(), $"goodrp_{Guid.NewGuid():N}{ext}");
        File.WriteAllText(tempFile, script.CodeBody);

        var (fileName, args) = script.Language switch
        {
            "ps1" => ("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\""),
            "py" => ("python", $"\"{tempFile}\""),
            "bat" => ("cmd.exe", $"/c \"{tempFile}\""),
            "js" => ("node", $"\"{tempFile}\""),
            _ => throw new NotSupportedException($"Language '{script.Language}' is not supported")
        };

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.StartInfo.Environment["GOODRP_TITLE"] = media.CleanTitle ?? "";
            process.StartInfo.Environment["GOODRP_ARTIST"] = media.Artist ?? "";
            process.StartInfo.Environment["GOODRP_ALBUM"] = media.Album ?? "";
            process.StartInfo.Environment["GOODRP_APP"] = media.AppName ?? "";
            process.StartInfo.Environment["GOODRP_STATE"] = media.State.ToString();
            process.StartInfo.Environment["GOODRP_POSITION"] = media.Position.ToString();
            process.StartInfo.Environment["GOODRP_DURATION"] = media.Duration.ToString();
            process.StartInfo.Environment["GOODRP_IMAGE_URL"] = imageUrl ?? "";

            if (process.Start())
            {
                using var cts = new CancellationTokenSource(script.TimeoutMs);
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (TaskCanceledException)
                {
                    try { process.Kill(true); } catch { }
                }
            }
        }
        catch
        {
            // Ignore script errors
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
