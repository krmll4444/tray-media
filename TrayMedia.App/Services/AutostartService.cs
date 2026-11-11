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
        key.SetValue(ValueName, $"\"{exe}\"");
    }
}
