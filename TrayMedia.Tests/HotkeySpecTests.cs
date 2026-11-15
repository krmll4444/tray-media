using TrayMedia.Services;
using Xunit;

namespace TrayMedia.Tests;

public class HotkeySpecTests
{
    [Theory]
    [InlineData("RControl+PageUp", true)]
    [InlineData("RControl+PageDown", true)]
    [InlineData("Control+Up", true)]
    [InlineData("LAlt+F9", true)]
    [InlineData("PageUp", false)]
    [InlineData("", false)]
    [InlineData("RControl+", false)]
    [InlineData("NotAKey+PageUp", false)]
    public void TryParse_validates_chords(string input, bool expected)
    {
        Assert.Equal(expected, HotkeySpec.TryParse(input, out _));
    }

    [Fact]
    public void Default_volume_chords_roundtrip()
    {
        Assert.True(HotkeySpec.TryParse(HotkeySpec.DefaultVolumeUp, out var up));
        Assert.True(HotkeySpec.TryParse(HotkeySpec.DefaultVolumeDown, out var down));
        Assert.Equal(HotkeySpec.DefaultVolumeUp, up.ToStorageString());
        Assert.Equal(HotkeySpec.DefaultVolumeDown, down.ToStorageString());
        Assert.Contains("Right Ctrl", up.ToDisplayString(), StringComparison.Ordinal);
        Assert.Contains("Page Up", up.ToDisplayString(), StringComparison.Ordinal);
        Assert.Contains("Page Down", down.ToDisplayString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Matches_requires_right_ctrl_for_default_up()
    {
        var up = HotkeySpec.ParseOrDefault(null, HotkeySpec.DefaultVolumeUp);
        Assert.True(up.Matches(0x21, vk => vk == 0xA3)); // PageUp + RControl
        Assert.False(up.Matches(0x21, vk => vk == 0xA2)); // PageUp + LControl only
        Assert.False(up.Matches(0x22, vk => vk == 0xA3)); // PageDown + RControl
    }

    [Fact]
    public void Control_token_matches_either_side()
    {
        Assert.True(HotkeySpec.TryParse("Control+PageUp", out var spec));
        Assert.True(spec.Matches(0x21, vk => vk == 0xA2));
        Assert.True(spec.Matches(0x21, vk => vk == 0xA3));
        Assert.False(spec.Matches(0x21, _ => false));
    }

    [Fact]
    public void ParseOrDefault_falls_back_on_garbage()
    {
        var spec = HotkeySpec.ParseOrDefault("nope", HotkeySpec.DefaultVolumeDown);
        Assert.Equal(HotkeySpec.DefaultVolumeDown, spec.ToStorageString());
    }
}
