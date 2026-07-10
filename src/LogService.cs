using System;

namespace GoodRP;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public bool Success { get; set; } = true;
}

public static class LogService
{
    public static event Action<LogEntry>? EntryAdded;

    private static readonly List<LogEntry> _entries = new();
    private const int MaxEntries = 500;

    public static void Log(string type, string message, bool success = true)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Type = type,
            Message = message,
            Success = success
        };

        lock (_entries)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        }

        EntryAdded?.Invoke(entry);
    }

    public static List<LogEntry> GetEntries()
    {
        lock (_entries)
            return new List<LogEntry>(_entries);
    }

    public static List<LogEntry> Filter(string? type = null)
    {
        lock (_entries)
        {
            if (string.IsNullOrEmpty(type) || type == "All")
                return new List<LogEntry>(_entries);

            return _entries.FindAll(e => string.Equals(e.Type, type, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void Clear()
    {
        lock (_entries)
            _entries.Clear();
    }
}
