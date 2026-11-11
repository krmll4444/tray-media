using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TrayMedia.Services;

public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TrayMedia";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null) return;

        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        var exe = Environment.ProcessPath
                   ?? Process.GetCurrentProcess().MainModule?.FileName
                   ?? Path.Combine(AppContext.BaseDirectory, "TrayMedia.exe");

        var runValue = BuildRunValue(exe);
        if (runValue is null) return;

        key.SetValue(ValueName, runValue);
    }

    /// <summary>
    /// Builds a safe HKCU Run value. Rejects quote / path injection.
    /// </summary>
    public static string? BuildRunValue(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath)) return null;

        string full;
        try
        {
            full = Path.GetFullPath(exePath.Trim());
        }
        catch
        {
            return null;
        }

        if (full.IndexOf('"') >= 0) return null;
        if (full.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) return null;
        if (!full.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return null;

        var name = Path.GetFileNameWithoutExtension(full);
        if (!name.Equals("TrayMedia", StringComparison.OrdinalIgnoreCase)) return null;

        return $"\"{full}\"";
    }
}
