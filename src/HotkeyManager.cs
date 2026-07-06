using System.Runtime.InteropServices;

namespace GoodRP;

public class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, (uint modifiers, uint key, Action callback)> _hotkeys = new();
    private int _nextId = 1;
    private bool _disposed;

    public event Action<int, string>? HotkeyRegistered;
    public event Action<int, string>? HotkeyFailed;
    public event Action<int>? HotkeyUnregistered;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public int Register(string hotkeyString, Action callback)
    {
        if (!TryParse(hotkeyString, out var modifiers, out var key))
        {
            HotkeyFailed?.Invoke(0, $"Invalid hotkey: {hotkeyString}");
            return -1;
        }

        var id = _nextId++;
        var success = NativeMethods.RegisterHotKey(_hwnd, id, modifiers, key);

        if (success)
        {
            _hotkeys[id] = (modifiers, key, callback);
            HotkeyRegistered?.Invoke(id, hotkeyString);
            return id;
        }
        else
        {
            var error = Marshal.GetLastWin32Error();
            var reason = error == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED
                ? "already in use by another app"
                : $"error {error}";
            HotkeyFailed?.Invoke(id, $"Failed to register {hotkeyString}: {reason}");
            return -1;
        }
    }

    public void Unregister(int id)
    {
        if (_hotkeys.ContainsKey(id))
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
            _hotkeys.Remove(id);
            HotkeyUnregistered?.Invoke(id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys.ToList())
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _hotkeys.Clear();
    }

    public bool HandleHotkey(int wParam)
    {
        if (_hotkeys.TryGetValue(wParam, out var entry))
        {
            entry.callback();
            return true;
        }
        return false;
    }

    public static bool TryParse(string hotkeyString, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        var keyPart = parts[^1];

        foreach (var part in parts[..^1])
        {
            var mod = part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => NativeMethods.MOD_CONTROL,
                "shift" => NativeMethods.MOD_SHIFT,
                "alt" => NativeMethods.MOD_ALT,
                "win" or "super" or "meta" => NativeMethods.MOD_WIN,
                _ => (uint)0
            };

            if (mod == 0) return false;
            modifiers |= mod;
        }

        key = keyPart.ToLowerInvariant() switch
        {
            "space" => (uint)Keys.Space,
            "enter" or "return" => (uint)Keys.Enter,
            "escape" or "esc" => (uint)Keys.Escape,
            "tab" => (uint)Keys.Tab,
            "backspace" => (uint)Keys.Back,
            "delete" or "del" => (uint)Keys.Delete,
            "insert" or "ins" => (uint)Keys.Insert,
            "home" => (uint)Keys.Home,
            "end" => (uint)Keys.End,
            "pageup" or "pgup" => (uint)Keys.PageUp,
            "pagedown" or "pgdn" => (uint)Keys.PageDown,
            "up" => (uint)Keys.Up,
            "down" => (uint)Keys.Down,
            "left" => (uint)Keys.Left,
            "right" => (uint)Keys.Right,
            "f1" => (uint)Keys.F1,
            "f2" => (uint)Keys.F2,
            "f3" => (uint)Keys.F3,
            "f4" => (uint)Keys.F4,
            "f5" => (uint)Keys.F5,
            "f6" => (uint)Keys.F6,
            "f7" => (uint)Keys.F7,
            "f8" => (uint)Keys.F8,
            "f9" => (uint)Keys.F9,
            "f10" => (uint)Keys.F10,
            "f11" => (uint)Keys.F11,
            "f12" => (uint)Keys.F12,
            _ when keyPart.Length == 1 && char.IsLetterOrDigit(keyPart[0]) => (uint)char.ToUpper(keyPart[0]),
            _ when Enum.TryParse<Keys>(keyPart, true, out var parsed) => (uint)parsed,
            _ => (uint)0
        };

        return key != 0;
    }

    public static string GetHotkeyDisplayString(uint modifiers, uint key)
    {
        var parts = new List<string>();

        if ((modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");

        var keysEnum = (Keys)key;
        parts.Add(keysEnum.ToString());

        return string.Join("+", parts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        GC.SuppressFinalize(this);
    }
}
