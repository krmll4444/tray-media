using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using TrayMedia.Services;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace TrayMedia;

public partial class SettingsWindow : Window
{
    private enum CaptureTarget
    {
        None,
        VolumeUp,
        VolumeDown,
    }

    private readonly AppSettings _settings;
    private readonly Action? _onChanged;
    private readonly Action<bool>? _setVolumeHotkeysPaused;
    private bool _suppressEvents;
    private CaptureTarget _capture = CaptureTarget.None;

    public SettingsWindow(
        AppSettings settings,
        Action? onChanged = null,
        Action<bool>? setVolumeHotkeysPaused = null)
    {
        InitializeComponent();
        _settings = settings;
        _onChanged = onChanged;
        _setVolumeHotkeysPaused = setVolumeHotkeysPaused;

        ApplyChromeTheme();
        LegalText.Text = $"Tray Media {GetAppVersion()}  ·  © 2026 krmll4444  ·  MIT License";

        _suppressEvents = true;
        AutostartToggle.IsChecked = settings.Autostart || AutostartService.IsEnabled();
        PinToTrayToggle.IsChecked = settings.PinToTray;
        VolumeHotkeysToggle.IsChecked = settings.VolumeHotkeysEnabled;
        _suppressEvents = false;

        RefreshHotkeyLabels();
        UpdateHotkeyRowsEnabled();

        PreviewKeyDown += OnPreviewKeyDown;
        Closed += (_, _) => EndCapture(save: false);
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
        HotkeysSectionLabel.Foreground = title;
        AutostartTitle.Foreground = title;
        PinTitle.Foreground = title;
        VolumeHotkeysTitle.Foreground = title;
        VolumeUpTitle.Foreground = title;
        VolumeDownTitle.Foreground = title;
        AutostartDesc.Foreground = body;
        PinDesc.Foreground = body;
        VolumeHotkeysDesc.Foreground = body;
        VolumeUpChord.Foreground = body;
        VolumeDownChord.Foreground = body;
        HintText.Foreground = body;
        HotkeyHintText.Foreground = body;
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

    private void OnVolumeHotkeysChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.VolumeHotkeysEnabled = VolumeHotkeysToggle.IsChecked == true;
        _settings.Save();
        UpdateHotkeyRowsEnabled();
        _onChanged?.Invoke();
    }

    private void OnVolumeUpCaptureClick(object sender, RoutedEventArgs e) => BeginCapture(CaptureTarget.VolumeUp);

    private void OnVolumeDownCaptureClick(object sender, RoutedEventArgs e) => BeginCapture(CaptureTarget.VolumeDown);

    private void BeginCapture(CaptureTarget target)
    {
        if (_capture != CaptureTarget.None)
        {
            EndCapture(save: false);
        }

        _capture = target;
        _setVolumeHotkeysPaused?.Invoke(true);
        RefreshHotkeyLabels();
        UpdateCaptureButtons();
    }

    private void EndCapture(bool save)
    {
        if (_capture == CaptureTarget.None) return;
        _capture = CaptureTarget.None;
        _setVolumeHotkeysPaused?.Invoke(false);
        if (!save)
        {
            RefreshHotkeyLabels();
        }

        UpdateCaptureButtons();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_capture == CaptureTarget.None) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            EndCapture(save: false);
            e.Handled = true;
            return;
        }

        if (IsModifierOnly(key))
        {
            e.Handled = true;
            return;
        }

        var spec = HotkeySpec.FromKeyEvent(
            key,
            Keyboard.Modifiers,
            IsDown(0xA2),
            IsDown(0xA3),
            IsDown(0xA4),
            IsDown(0xA5),
            IsDown(0xA0),
            IsDown(0xA1),
            IsDown(0x5B) || IsDown(0x5C));

        e.Handled = true;
        if (spec is null)
        {
            HotkeyHintText.Text = "Shortcut needs a modifier (e.g. Right Ctrl) plus a key. Esc cancels.";
            return;
        }

        if (_capture == CaptureTarget.VolumeUp)
        {
            _settings.VolumeUpHotkey = spec.ToStorageString();
        }
        else
        {
            _settings.VolumeDownHotkey = spec.ToStorageString();
        }

        _settings.NormalizeHotkeys();
        _settings.Save();
        HotkeyHintText.Text = "Default: Right Ctrl + Page Up / Page Down. Click Change, then press a new shortcut (Esc cancels).";
        RefreshHotkeyLabels();
        EndCapture(save: true);
        _onChanged?.Invoke();
    }

    private void RefreshHotkeyLabels()
    {
        var up = _settings.GetVolumeUpHotkey();
        var down = _settings.GetVolumeDownHotkey();

        VolumeUpChord.Text = _capture == CaptureTarget.VolumeUp
            ? "Press new shortcut…"
            : up.ToDisplayString();
        VolumeDownChord.Text = _capture == CaptureTarget.VolumeDown
            ? "Press new shortcut…"
            : down.ToDisplayString();
    }

    private void UpdateCaptureButtons()
    {
        VolumeUpCaptureBtn.Content = _capture == CaptureTarget.VolumeUp ? "Listening…" : "Change";
        VolumeDownCaptureBtn.Content = _capture == CaptureTarget.VolumeDown ? "Listening…" : "Change";
    }

    private void UpdateHotkeyRowsEnabled()
    {
        var on = VolumeHotkeysToggle.IsChecked == true;
        VolumeUpRow.IsEnabled = on;
        VolumeDownRow.IsEnabled = on;
        VolumeUpRow.Opacity = on ? 1 : 0.45;
        VolumeDownRow.Opacity = on ? 1 : 0.45;
    }

    private static bool IsModifierOnly(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.System;

    private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static SolidColorBrush BrushRgb(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(MediaColor.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static string GetAppVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(info))
        {
            info = asm.GetName().Version?.ToString() ?? "0.0.0";
        }

        var plus = info.IndexOf('+');
        if (plus >= 0) info = info[..plus];
        return info;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
