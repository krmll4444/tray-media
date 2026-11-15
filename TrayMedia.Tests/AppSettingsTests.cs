using TrayMedia.Services;
using Xunit;

namespace TrayMedia.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Save_and_Load_roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "TrayMediaTests", Guid.NewGuid() + ".json");
        try
        {
            var settings = new AppSettings
            {
                Autostart = true,
                PinToTray = false,
                VolumeHotkeysEnabled = false,
                VolumeUpHotkey = "LControl+Up",
                VolumeDownHotkey = "LControl+Down",
            };
            settings.Save(path);

            var loaded = AppSettings.Load(path);
            Assert.True(loaded.Autostart);
            Assert.False(loaded.PinToTray);
            Assert.False(loaded.VolumeHotkeysEnabled);
            Assert.Equal("LControl+Up", loaded.VolumeUpHotkey);
            Assert.Equal("LControl+Down", loaded.VolumeDownHotkey);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_missing_file_returns_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "TrayMediaTests", Guid.NewGuid() + ".json");
        var loaded = AppSettings.Load(path);
        Assert.False(loaded.Autostart);
        Assert.True(loaded.PinToTray);
        Assert.True(loaded.VolumeHotkeysEnabled);
        Assert.Equal(HotkeySpec.DefaultVolumeUp, loaded.VolumeUpHotkey);
        Assert.Equal(HotkeySpec.DefaultVolumeDown, loaded.VolumeDownHotkey);
    }

    [Fact]
    public void Load_normalizes_invalid_hotkeys()
    {
        var path = Path.Combine(Path.GetTempPath(), "TrayMediaTests", Guid.NewGuid() + ".json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, """{"VolumeUpHotkey":"nope","VolumeDownHotkey":"PageDown"}""");
            var loaded = AppSettings.Load(path);
            Assert.Equal(HotkeySpec.DefaultVolumeUp, loaded.VolumeUpHotkey);
            Assert.Equal(HotkeySpec.DefaultVolumeDown, loaded.VolumeDownHotkey);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
