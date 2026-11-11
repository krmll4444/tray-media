using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrayMedia.Models;
using TrayMedia.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfCursors = System.Windows.Input.Cursors;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace TrayMedia;

public partial class MainWindow : Window
{
    private ChromeTheme _theme = WindowsChrome.Read();
    private MediaPlaybackState _state = MediaPlaybackState.Empty;
    private bool _dragging;
    private bool _progressHover;
    private double _dragPositionSec;
    private string _lastArtKey = "";

    public long IgnoreDeactivateUntil { get; set; }

    public event EventHandler? PlayPauseRequested;
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler<double>? SeekRequested;
    public event EventHandler? HideRequested;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => AcrylicHelper.Apply(this);
        ApplyTheme(_theme);
        ApplyState(_state);
    }

    public void ApplyTheme(ChromeTheme theme)
    {
        _theme = theme;
        // WPF owns every pixel of this fill — no DWM acrylic compositing.
        Flyout.Background = theme.Background;
        Flyout.BorderBrush = theme.Border;
        TitleText.Foreground = theme.Text;
        ArtistText.Foreground = theme.TextSecondary;
        ElapsedText.Foreground = theme.TextSecondary;
        DurationText.Foreground = theme.TextSecondary;
        ProgressTrack.Fill = theme.ProgressTrack;
        ProgressFill.Fill = theme.AccentBrush;
        ProgressThumb.Fill = theme.AccentBrush;
        PrevIcon.Fill = theme.Text;
        PlayIcon.Fill = theme.Text;
        PauseIcon.Fill = theme.Text;
        NextIcon.Fill = theme.Text;

        UpdateButtonChrome(PrevBtn, false);
        UpdateButtonChrome(PlayBtn, false);
        UpdateButtonChrome(NextBtn, false);
    }

    public void ApplyState(MediaPlaybackState state)
    {
        _state = state;
        TitleText.Text = string.IsNullOrWhiteSpace(state.Title) ? " " : state.Title;
        ArtistText.Text = string.IsNullOrWhiteSpace(state.Artist) ? " " : state.Artist;
        TitleText.Opacity = state.IsEmpty ? 0.85 : 1;
        ArtistText.Opacity = state.IsEmpty ? 0.85 : 1;

        if (state.AlbumArtBytes is { Length: > 0 })
        {
            if (state.AlbumArtKey != _lastArtKey)
            {
                _lastArtKey = state.AlbumArtKey;
                AlbumArt.Source = DecodeImage(state.AlbumArtBytes);
            }

            AlbumArt.Visibility = Visibility.Visible;
        }
        else
        {
            _lastArtKey = "";
            AlbumArt.Source = null;
            AlbumArt.Visibility = Visibility.Collapsed;
        }

        PlayIcon.Visibility = state.IsPlaying ? Visibility.Collapsed : Visibility.Visible;
        PauseIcon.Visibility = state.IsPlaying ? Visibility.Visible : Visibility.Collapsed;
        PlayBtn.ToolTip = state.IsPlaying ? "Pause" : "Play";

        PlayBtn.IsEnabled = !(state.IsEmpty && !state.CanPlay && !state.CanPause);
        PrevBtn.IsEnabled = !state.IsEmpty && state.CanPrevious;
        NextBtn.IsEnabled = !state.IsEmpty && state.CanNext;
        ProgressBar.IsEnabled = !state.IsEmpty && state.CanSeek;
        ProgressBar.Opacity = ProgressBar.IsEnabled ? 1 : 0.45;
        ProgressBar.Cursor = ProgressBar.IsEnabled ? WpfCursors.Hand : WpfCursors.Arrow;

        if (!_dragging)
        {
            SetProgress(state.PositionSec, state.DurationSec);
        }
    }

    private void SetProgress(double positionSec, double durationSec)
    {
        var ratio = durationSec > 0 ? Math.Min(Math.Max(positionSec / durationSec, 0), 1) : 0;
        var width = ProgressBar.ActualWidth;
        if (width <= 0) width = 296;
        ProgressFill.Width = width * ratio;
        ProgressThumb.Margin = new Thickness(width * ratio - 5, 0, 0, 0);
        ElapsedText.Text = TimeFormatting.Format(positionSec);
        DurationText.Text = TimeFormatting.Format(durationSec);
        UpdateThumbVisibility();
    }

    private void OnProgressDown(object sender, WpfMouseButtonEventArgs e)
    {
        if (!ProgressBar.IsEnabled) return;
        _dragging = true;
        ProgressBar.CaptureMouse();
        _dragPositionSec = PositionFromPoint(e.GetPosition(ProgressBar));
        SetProgress(_dragPositionSec, _state.DurationSec);
        UpdateThumbVisibility();
        e.Handled = true;
    }

    private void OnProgressMove(object sender, WpfMouseEventArgs e)
    {
        _progressHover = true;
        UpdateThumbVisibility();
        if (!_dragging) return;
        _dragPositionSec = PositionFromPoint(e.GetPosition(ProgressBar));
        SetProgress(_dragPositionSec, _state.DurationSec);
    }

    private void OnProgressUp(object sender, WpfMouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ProgressBar.ReleaseMouseCapture();
        _dragPositionSec = PositionFromPoint(e.GetPosition(ProgressBar));
        SetProgress(_dragPositionSec, _state.DurationSec);
        SeekRequested?.Invoke(this, _dragPositionSec);
        UpdateThumbVisibility();
    }

    private void OnProgressLeave(object sender, WpfMouseEventArgs e)
    {
        if (_dragging) return;
        _progressHover = false;
        UpdateThumbVisibility();
    }

    private void UpdateThumbVisibility()
    {
        var show = _dragging || _progressHover || ProgressBar.IsKeyboardFocusWithin;
        if (ProgressThumb.RenderTransform is ScaleTransform st)
        {
            st.ScaleX = show ? 1 : 0;
            st.ScaleY = show ? 1 : 0;
        }

        ProgressThumb.Opacity = show ? 1 : 0;
    }

    private double PositionFromPoint(WpfPoint point)
    {
        var width = ProgressBar.ActualWidth;
        var ratio = width > 0 ? point.X / width : 0;
        ratio = Math.Min(Math.Max(ratio, 0), 1);
        return ratio * Math.Max(0, _state.DurationSec);
    }

    private void OnPrevClick(object sender, RoutedEventArgs e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
    private void OnPlayClick(object sender, RoutedEventArgs e) => PlayPauseRequested?.Invoke(this, EventArgs.Empty);
    private void OnNextClick(object sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);

    private void OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key != WpfKey.Escape) return;
        HideRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WireButtonHover(PrevBtn);
        WireButtonHover(PlayBtn);
        WireButtonHover(NextBtn);
        SizeChanged += (_, _) =>
        {
            if (!_dragging) SetProgress(_state.PositionSec, _state.DurationSec);
        };
    }

    private void WireButtonHover(WpfButton button)
    {
        button.MouseEnter += (_, _) => UpdateButtonChrome(button, hover: true);
        button.MouseLeave += (_, _) => UpdateButtonChrome(button, hover: false);
        button.PreviewMouseLeftButtonDown += (_, _) => UpdateButtonChrome(button, hover: true, active: true);
        button.PreviewMouseLeftButtonUp += (_, _) => UpdateButtonChrome(button, hover: button.IsMouseOver);
    }

    private void UpdateButtonChrome(WpfButton button, bool hover, bool active = false)
    {
        if (active) button.Background = _theme.ButtonActive;
        else if (hover) button.Background = _theme.ButtonHover;
        else button.Background = System.Windows.Media.Brushes.Transparent;
    }

    private static BitmapImage? DecodeImage(byte[] bytes)
    {
        try
        {
            using var ms = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
