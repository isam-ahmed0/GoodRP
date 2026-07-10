using System.Diagnostics;

namespace GoodRP;

public static class ScriptingService
{
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

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scriptPath!,
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
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scriptPath!,
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
