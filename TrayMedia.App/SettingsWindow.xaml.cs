using System.Windows;
using TrayMedia.Services;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace TrayMedia;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action? _onChanged;
    private bool _suppressEvents;

    public SettingsWindow(AppSettings settings, Action? onChanged = null)
    {
        InitializeComponent();
        _settings = settings;
        _onChanged = onChanged;

        ApplyChromeTheme();

        _suppressEvents = true;
        AutostartToggle.IsChecked = settings.Autostart || AutostartService.IsEnabled();
        PinToTrayToggle.IsChecked = settings.PinToTray;
        _suppressEvents = false;
    }

    private void ApplyChromeTheme()
    {
        var theme = WindowsChrome.Read();
        var dark = WindowsChrome.AppsUseDarkTheme();

        Resources["SettingsAccentBrush"] = theme.AccentBrush;

        // Win10 Settings content surface: flat page, no cards.
        var pageBg = dark ? BrushRgb(0x20, 0x20, 0x20) : BrushRgb(0xFF, 0xFF, 0xFF);
        var title = dark ? MediaBrushes.White : BrushRgb(0x1A, 0x1A, 0x1A);
        var body = dark ? BrushRgb(0xC8, 0xC8, 0xC8) : BrushRgb(0x5D, 0x5D, 0x5D);

        Background = pageBg;
        Root.Background = pageBg;

        PageTitle.Foreground = title;
        SectionLabel.Foreground = title;
        AutostartTitle.Foreground = title;
        PinTitle.Foreground = title;
        AutostartDesc.Foreground = body;
        PinDesc.Foreground = body;
        HintText.Foreground = body;
        LegalText.Foreground = body;

        if (dark)
        {
            Resources["ToggleOffBrush"] = BrushRgb(0x2C, 0x2C, 0x2C);
            Resources["ToggleOffBorderBrush"] = BrushRgb(0xA0, 0xA0, 0xA0);
            Resources["ToggleThumbOffBrush"] = BrushRgb(0xE0, 0xE0, 0xE0);
        }
        else
        {
            Resources["ToggleOffBrush"] = BrushRgb(0xFF, 0xFF, 0xFF);
            Resources["ToggleOffBorderBrush"] = BrushRgb(0x88, 0x88, 0x88);
            Resources["ToggleThumbOffBrush"] = BrushRgb(0x33, 0x33, 0x33);
        }
    }

    private void OnAutostartChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.Autostart = AutostartToggle.IsChecked == true;
        _settings.Save();
        AutostartService.SetEnabled(_settings.Autostart);
        _onChanged?.Invoke();
    }

    private void OnPinChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.PinToTray = PinToTrayToggle.IsChecked == true;
        _settings.Save();
        _onChanged?.Invoke();
    }

    private static SolidColorBrush BrushRgb(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(MediaColor.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
