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

    /// <summary>
    /// Returns true when UI-visible fields match (position rounded to whole seconds).
    /// </summary>
    public bool SameUiAs(MediaPlaybackState other) =>
        IsEmpty == other.IsEmpty
        && IsPlaying == other.IsPlaying
        && CanPlay == other.CanPlay
        && CanPause == other.CanPause
        && CanNext == other.CanNext
        && CanPrevious == other.CanPrevious
        && CanSeek == other.CanSeek
        && AlbumArtKey == other.AlbumArtKey
        && Title == other.Title
        && Artist == other.Artist
        && AppId == other.AppId
        && (int)PositionSec == (int)other.PositionSec
        && (int)DurationSec == (int)other.DurationSec;
}
