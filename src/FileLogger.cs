using DiscordRPC.Logging;

namespace GoodRP;

public class FileLogger : ILogger
{
    private readonly string _path;
    private static readonly object _lock = new();

    public FileLogger(string path)
    {
        _path = path;
        File.WriteAllText(_path, $"[{DateTime.Now}] Logger initialized\n");
    }

    public LogLevel Level { get; set; } = LogLevel.Trace;

    public void Trace(string message, params object[] args) => Log("TRACE", message, args);
    public void Info(string message, params object[] args) => Log("INFO", message, args);
    public void Warning(string message, params object[] args) => Log("WARN", message, args);
    public void Error(string message, params object[] args) => Log("ERROR", message, args);

    private void Log(string level, string message, object[] args)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {string.Format(message, args)}\n";
            lock (_lock) File.AppendAllText(_path, line);
        }
        catch { }
    }
}
