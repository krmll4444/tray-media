namespace TrayMedia.Models;

public sealed class MediaPlaybackState
{
    public static MediaPlaybackState Empty { get; } = new()
    {
        IsEmpty = true,
        Title = "Nothing is playing",
        Artist = "Start media in another app",
    };

    public bool IsEmpty { get; init; }
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public byte[]? AlbumArtBytes { get; init; }
    public string AlbumArtKey { get; init; } = "";
    public bool IsPlaying { get; init; }
    public double PositionSec { get; init; }
    public double DurationSec { get; init; }
    public string AppId { get; init; } = "";
    public bool CanPlay { get; init; }
    public bool CanPause { get; init; }
    public bool CanNext { get; init; }
    public bool CanPrevious { get; init; }
    public bool CanSeek { get; init; }
}
