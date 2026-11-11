using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace TrayMedia.Models;

public sealed class ChromeTheme
{
    public bool Transparency { get; init; }
    public bool IsDark { get; init; }
    public MediaColor Accent { get; init; }
    public MediaColor TaskbarRgb { get; init; }
    public MediaBrush Background { get; init; } = MediaBrushes.Transparent;
    public MediaBrush FallbackBackground { get; init; } = MediaBrushes.Transparent;
    public MediaBrush Border { get; init; } = MediaBrushes.Transparent;
    public MediaBrush Text { get; init; } = MediaBrushes.White;
    public MediaBrush TextSecondary { get; init; } = MediaBrushes.Gray;
    public MediaBrush ButtonHover { get; init; } = MediaBrushes.Transparent;
    public MediaBrush ButtonActive { get; init; } = MediaBrushes.Transparent;
    public MediaBrush ProgressTrack { get; init; } = MediaBrushes.Gray;
    public MediaBrush AccentBrush { get; init; } = MediaBrushes.DodgerBlue;
    public int BlurPx { get; init; }
}
