using System.IO;
using System.Text.Json;

namespace TrayMedia.Services;

public sealed class AppSettings
{
    public const long MaxSettingsBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        MaxDepth = 8,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
    };

    public bool Autostart { get; set; }
    public bool PinToTray { get; set; } = true;

    public static string SettingsPath
    {
        get
        {
            var dir = GetSettingsDirectory();
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static string GetSettingsDirectory()
    {
        var dir = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TrayMedia"));
        return dir;
    }

    /// <summary>
    /// Ensures a settings path cannot escape LocalAppData\TrayMedia when using the default store.
    /// Custom paths (tests) must be absolute and under the system temp or the settings directory.
    /// </summary>
    public static bool IsAllowedSettingsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        if (!full.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;

        var settingsDir = GetSettingsDirectory();
        if (IsUnderDirectory(full, settingsDir)) return true;

        var temp = Path.GetFullPath(Path.GetTempPath());
        return IsUnderDirectory(full, temp);
    }

    public static AppSettings Load(string? path = null)
    {
        path ??= SettingsPath;
        if (!IsAllowedSettingsPath(path)) return new AppSettings();

        try
        {
            if (!File.Exists(path)) return new AppSettings();

            var info = new FileInfo(path);
            if (info.Length <= 0 || info.Length > MaxSettingsBytes) return new AppSettings();

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(string? path = null)
    {
        path ??= SettingsPath;
        if (!IsAllowedSettingsPath(path))
        {
            throw new InvalidOperationException("Settings path is not allowed.");
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static bool IsUnderDirectory(string fullFilePath, string directory)
    {
        var prefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        return fullFilePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
