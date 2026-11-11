using TrayMedia.Models;
using Xunit;

namespace TrayMedia.Tests;

public class MediaPlaybackStateTests
{
    [Fact]
    public void SameUiAs_true_when_only_subsecond_position_differs()
    {
        var a = new MediaPlaybackState
        {
            Title = "Track",
            Artist = "Artist",
            PositionSec = 10.1,
            DurationSec = 100,
            IsPlaying = true,
        };
        var b = new MediaPlaybackState
        {
            Title = "Track",
            Artist = "Artist",
            PositionSec = 10.9,
            DurationSec = 100,
            IsPlaying = true,
        };

        Assert.True(a.SameUiAs(b));
    }

    [Fact]
    public void SameUiAs_false_when_title_changes()
    {
        var a = new MediaPlaybackState { Title = "A", Artist = "X" };
        var b = new MediaPlaybackState { Title = "B", Artist = "X" };
        Assert.False(a.SameUiAs(b));
    }

    [Fact]
    public void Empty_has_placeholder_copy()
    {
        Assert.True(MediaPlaybackState.Empty.IsEmpty);
        Assert.False(string.IsNullOrWhiteSpace(MediaPlaybackState.Empty.Title));
        Assert.False(string.IsNullOrWhiteSpace(MediaPlaybackState.Empty.Artist));
    }
}
