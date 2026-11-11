using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TrayMedia.Models;

namespace TrayMedia.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private Icon? _currentIcon;
    private string _lastIconKey = "";
    private bool _disposed;
    private Point _lastClickScreen = Point.Empty;

    public event EventHandler? LeftClick;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService()
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Settings", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Вийти", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu,
            Text = "Tray Media",
        };
        _notifyIcon.MouseUp += OnMouseUp;
    }

    public Point LastClickScreen => _lastClickScreen;

    public (IntPtr Hwnd, uint Id) GetNotifyIdentity()
    {
        try
        {
            var windowField = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);
            var idField = typeof(NotifyIcon).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var windowObj = windowField?.GetValue(_notifyIcon);
            if (windowObj is null || idField is null) return (IntPtr.Zero, 0);

            var handleProp = windowObj.GetType().GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);
            if (handleProp?.GetValue(windowObj) is not IntPtr hwnd) return (IntPtr.Zero, 0);
            var id = Convert.ToUInt32(idField.GetValue(_notifyIcon));
            return (hwnd, id);
        }
        catch
        {
            return (IntPtr.Zero, 0);
        }
    }

    public void Update(MediaPlaybackState state, bool darkTaskbar)
    {
        var suffix = darkTaskbar ? "" : "-light";
        string file;
        if (state.IsPlaying)
        {
            file = $"tray-pause{suffix}.png";
        }
        else if (!state.IsEmpty)
        {
            file = $"tray-play{suffix}.png";
        }
        else
        {
            file = $"tray-play-empty{suffix}.png";
        }

        var key = $"{file}|{(darkTaskbar ? "dark" : "light")}";
        if (key != _lastIconKey)
        {
            _lastIconKey = key;
            var next = LoadIcon(file);
            _notifyIcon.Icon = next;
            _currentIcon?.Dispose();
            _currentIcon = next;
        }

        if (state.IsEmpty)
        {
            _notifyIcon.Text = Truncate("Tray Media — нічого не відтворюється");
        }
        else if (state.IsPlaying)
        {
            _notifyIcon.Text = Truncate($"Пауза — {state.Title}");
        }
        else
        {
            _notifyIcon.Text = Truncate($"Відтворити — {state.Title}");
        }
    }

    public Rectangle GetBounds()
    {
        var fromApi = TryGetNotifyIconRect();
        if (fromApi is Rectangle apiRect && apiRect.Width > 0 && apiRect.Height > 0)
        {
            return apiRect;
        }

        if (_lastClickScreen != Point.Empty)
        {
            return new Rectangle(_lastClickScreen.X - 12, _lastClickScreen.Y - 12, 24, 24);
        }

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        return new Rectangle(screen.Right - 24, screen.Bottom - 24, 24, 24);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
        _menu.Dispose();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _lastClickScreen = Control.MousePosition;
        LeftClick?.Invoke(this, EventArgs.Empty);
    }

    private Rectangle? TryGetNotifyIconRect()
    {
        try
        {
            var (hwnd, id) = GetNotifyIdentity();
            if (hwnd == IntPtr.Zero) return null;

            var identifier = new NotifyIconIdentifier
            {
                CbSize = (uint)Marshal.SizeOf<NotifyIconIdentifier>(),
                HWnd = hwnd,
                UId = id,
                GuidItem = Guid.Empty,
            };

            if (Shell_NotifyIconGetRect(ref identifier, out var rect) == 0)
            {
                var result = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                if (result.Width > 0 && result.Height > 0) return result;
            }
        }
        catch
        {
            // fallback
        }

        return null;
    }

    private static Icon LoadIcon(string fileName)
    {
        var uri = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
        var streamInfo = System.Windows.Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException($"Missing tray asset: {fileName}");
        using var bitmap = new Bitmap(streamInfo.Stream);
        var hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static string Truncate(string text) =>
        text.Length <= 63 ? text : text[..60] + "...";

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int Shell_NotifyIconGetRect(ref NotifyIconIdentifier identifier, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential)]
    private struct NotifyIconIdentifier
    {
        public uint CbSize;
        public IntPtr HWnd;
        public uint UId;
        public Guid GuidItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
