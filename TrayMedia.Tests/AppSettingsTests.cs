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
            };
            settings.Save(path);

            var loaded = AppSettings.Load(path);
            Assert.True(loaded.Autostart);
            Assert.False(loaded.PinToTray);
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
    }
}
