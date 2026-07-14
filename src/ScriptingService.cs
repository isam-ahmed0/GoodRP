using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoodRP;

public static class ScriptingService
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static string GetScriptRunner(string scriptPath)
    {
        var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
        return ext switch
        {
            ".ps1" => IsWindows ? "powershell.exe" : "pwsh",
            ".py" => IsWindows ? "python" : "python3",
            ".bat" => IsWindows ? "cmd.exe" : "sh",
            ".js" => "node",
            _ => scriptPath
        };
    }

    private static string GetScriptArgs(string scriptPath)
    {
        var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
        if (ext == ".bat" && !IsWindows)
            return $"-c \"{scriptPath}\"";
        return $"\"{scriptPath}\"";
    }

    public static void RunScript(string? scriptPath, MediaInfo media, MediaPlaybackState? state = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            return;

        if (scriptPath.EndsWith(".grp", StringComparison.OrdinalIgnoreCase))
        {
            var grpScript = GrpParser.Parse(scriptPath);
            _ = Task.Run(async () =>
            {
                try
                {
                    await GrpExecutor.RunAsync(grpScript, media, imageUrl);
                }
                catch
                {
                    // Ignore .grp execution errors
                }
            });
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var runner = GetScriptRunner(scriptPath);
                var args = GetScriptArgs(scriptPath);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = runner,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };

                process.StartInfo.Environment["GOODRP_TITLE"] = media.CleanTitle ?? "";
                process.StartInfo.Environment["GOODRP_ARTIST"] = media.Artist ?? "";
                process.StartInfo.Environment["GOODRP_ALBUM"] = media.Album ?? "";
                process.StartInfo.Environment["GOODRP_APP"] = media.AppName ?? "";
                process.StartInfo.Environment["GOODRP_STATE"] = state?.ToString() ?? media.State.ToString();
                process.StartInfo.Environment["GOODRP_POSITION"] = media.Position.ToString();
                process.StartInfo.Environment["GOODRP_DURATION"] = media.Duration.ToString();
                process.StartInfo.Environment["GOODRP_IMAGE_URL"] = imageUrl ?? "";

                if (process.Start())
                {
                    using var cts = new CancellationTokenSource(ConfigManager.Config.ScriptTimeoutMs);
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
        });
    }

    public static void OnMediaStopped(string? scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var runner = GetScriptRunner(scriptPath);
                var args = GetScriptArgs(scriptPath);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = runner,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.StartInfo.Environment["GOODRP_EVENT"] = "stopped";

                if (process.Start())
                {
                    using var cts = new CancellationTokenSource(ConfigManager.Config.ScriptTimeoutMs);
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
        });
    }
}
