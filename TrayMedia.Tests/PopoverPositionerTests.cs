using System.Drawing;
using TrayMedia.Services;
using Xunit;

namespace TrayMedia.Tests;

public class PopoverPositionerTests
{
    private static readonly Rectangle Display = new(0, 0, 1920, 1080);
    private static readonly Rectangle WorkBottom = new(0, 0, 1920, 1040); // taskbar bottom
    private static readonly Size Popover = new(320, 146);

    [Fact]
    public void DetectTaskbarSide_bottom()
    {
        Assert.Equal("bottom", PopoverPositioner.DetectTaskbarSide(Display, WorkBottom));
    }

    [Fact]
    public void DetectTaskbarSide_top()
    {
        var work = new Rectangle(0, 40, 1920, 1040);
        Assert.Equal("top", PopoverPositioner.DetectTaskbarSide(Display, work));
    }

    [Fact]
    public void DetectTaskbarSide_left()
    {
        var work = new Rectangle(60, 0, 1860, 1080);
        Assert.Equal("left", PopoverPositioner.DetectTaskbarSide(Display, work));
    }

    [Fact]
    public void DetectTaskbarSide_right()
    {
        var work = new Rectangle(0, 0, 1860, 1080);
        Assert.Equal("right", PopoverPositioner.DetectTaskbarSide(Display, work));
    }

    [Fact]
    public void Place_above_tray_when_taskbar_is_bottom()
    {
        var tray = new Rectangle(1800, 1048, 24, 24);
        var pos = PopoverPositioner.Place(tray, Popover, Display, WorkBottom);

        Assert.True(pos.Y + Popover.Height <= tray.Y);
        Assert.InRange(pos.X, WorkBottom.Left + 4, WorkBottom.Right - Popover.Width - 4);
        Assert.InRange(pos.Y, WorkBottom.Top + 4, WorkBottom.Bottom - Popover.Height - 4);
    }

    [Fact]
    public void Place_fallback_when_tray_bounds_invalid()
    {
        var tray = new Rectangle(0, 0, 0, 0);
        var pos = PopoverPositioner.Place(tray, Popover, Display, WorkBottom);

        Assert.Equal(WorkBottom.Right - Popover.Width - 8, pos.X);
        Assert.Equal(WorkBottom.Bottom - Popover.Height - 8, pos.Y);
    }

    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-1, 0, 10, 0)]
    [InlineData(99, 0, 10, 10)]
    public void Clamp_bounds_value(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, PopoverPositioner.Clamp(value, min, max));
    }
}
