using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TrayMedia.Models;
using TrayMedia.Services;

namespace TrayMedia;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Local\\TrayMedia.krmll4444.singleinstance";

    private Mutex? _mutex;
    private MainWindow? _popover;
    private SettingsWindow? _settingsWindow;
    private TrayIconService? _tray;
    private SmtcService? _smtc;
    private AppSettings _settings = AppSettings.Load();
    private ChromeTheme _theme = WindowsChrome.Read();
    private MediaPlaybackState _playerState = MediaPlaybackState.Empty;
    private bool _exiting;
    private long _hiddenAt;
    private CancellationTokenSource? _promoteCts;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        AutostartService.SetEnabled(_settings.Autostart);

        _smtc = new SmtcService();
        _smtc.StateChanged += OnSmtcStateChanged;

        _popover = new MainWindow();
        _ = new WindowInteropHelper(_popover).EnsureHandle();
        _popover.Deactivated += (_, _) =>
        {
            if (_exiting) return;
            if (_popover.IgnoreDeactivateUntil > Environment.TickCount64) return;
            _popover.Hide();
            _hiddenAt = Environment.TickCount64;
        };

        // Fire media commands without blocking the UI thread on SMTC round-trips.
        _popover.PlayPauseRequested += (_, _) => _ = _smtc.ToggleAsync();
        _popover.NextRequested += (_, _) => _ = _smtc.NextAsync();
        _popover.PreviousRequested += (_, _) => _ = _smtc.PreviousAsync();
        _popover.SeekRequested += (_, pos) => _ = _smtc.SeekAsync(pos);
        _popover.HideRequested += (_, _) => _popover.Hide();

        _tray = new TrayIconService();
        _tray.LeftClick += (_, _) => TogglePopover();
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.ExitRequested += (_, _) => ExitApp();
        _tray.Update(_playerState, _theme.IsDark);

        ScheduleTrayPromotion();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        try
        {
            await _smtc.StartAsync();
        }
        catch (Exception ex)
        {
            _playerState = new MediaPlaybackState
            {
                IsEmpty = true,
                Title = "SMTC unavailable",
                Artist = ex.Message,
            };
            _tray.Update(_playerState, _theme.IsDark);
            _popover.ApplyState(_playerState);
        }

        _popover.ApplyTheme(_theme);
        _popover.ApplyState(_playerState);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _exiting = true;
        _promoteCts?.Cancel();
        _promoteCts?.Dispose();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _smtc?.Dispose();
        _tray?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void ScheduleTrayPromotion()
    {
        _promoteCts?.Cancel();
        _promoteCts = new CancellationTokenSource();
        var token = _promoteCts.Token;
        _ = Task.Run(async () =>
        {
            int[] delaysMs = { 300, 1000, 2500 };
            foreach (var delay in delaysMs)
            {
                if (token.IsCancellationRequested) return;
                try { await Task.Delay(delay, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
                _ = Dispatcher.BeginInvoke(ApplyTrayPinPreference, DispatcherPriority.Background);
            }
        }, token);
    }

    private void ApplyTrayPinPreference()
    {
        if (_tray is null) return;
        var (hwnd, id) = _tray.GetNotifyIdentity();
        if (hwnd == IntPtr.Zero) return;
        TrayIconPromoter.Apply(_settings.PinToTray, hwnd, id);
    }

    private void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, OnSettingsChanged);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsChanged()
    {
        ApplyTrayPinPreference();
        ScheduleTrayPromotion();
    }

    private void OnSmtcStateChanged(object? sender, MediaPlaybackState state)
    {
        // Don't block SMTC thread; coalesce to UI at Render priority.
        Dispatcher.BeginInvoke(() =>
        {
            _playerState = state;
            _tray?.Update(state, _theme.IsDark);
            if (_popover is { IsVisible: true })
            {
                _popover.ApplyState(state);
            }
            else
            {
                // While hidden, still keep last state for instant open.
                _popover?.ApplyState(state);
            }
        }, DispatcherPriority.Background);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.BeginInvoke(RefreshTheme, DispatcherPriority.Background);
        }
    }

    private void RefreshTheme()
    {
        _theme = WindowsChrome.Read();
        _popover?.ApplyTheme(_theme);
        _tray?.Update(_playerState, _theme.IsDark);
    }

    private void TogglePopover()
    {
        if (_popover is null) return;
        if (_popover.IsVisible)
        {
            _popover.Hide();
            _hiddenAt = Environment.TickCount64;
            return;
        }

        // Short guard against tray-click blur race (deactivate then click).
        if (Environment.TickCount64 - _hiddenAt < 180) return;
        ShowPopover();
    }

    private void ShowPopover()
    {
        if (_popover is null || _tray is null) return;

        var dpi = GetDpi(_popover);
        var trayBounds = _tray.GetBounds();
        var widthPx = (int)Math.Round(_popover.Width * dpi);
        var heightPx = (int)Math.Round(_popover.Height * dpi);
        var pos = PopoverPositioner.Place(trayBounds, new System.Drawing.Size(widthPx, heightPx));

        _popover.IgnoreDeactivateUntil = Environment.TickCount64 + 200;
        _popover.Left = pos.X / dpi;
        _popover.Top = pos.Y / dpi;
        _popover.ApplyTheme(_theme);
        _popover.ApplyState(_playerState);
        _popover.Show();
        _popover.Activate();
    }

    private void ExitApp()
    {
        _exiting = true;
        Shutdown();
    }

    private static double GetDpi(Window window)
    {
        try
        {
            return VisualTreeHelper.GetDpi(window).DpiScaleX;
        }
        catch
        {
            var source = PresentationSource.FromVisual(window);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }
    }
}
