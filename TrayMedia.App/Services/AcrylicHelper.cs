using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TrayMedia.Services;

/// <summary>
/// Window chrome helpers. DWM AccentPolicy acrylic is intentionally NOT used:
/// on WPF it causes partial invalidation artifacts (UI only appears under the cursor).
/// Background tint is painted by WPF itself from WindowsChrome brushes.
/// </summary>
public static class AcrylicHelper
{
    private const int GwlExstyle = -20;
    private const int WsExToolwindow = 0x00000080;

    public static bool Apply(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            if (hwnd == IntPtr.Zero) return false;

            var ex = GetWindowLong(hwnd, GwlExstyle);
            if ((ex & WsExToolwindow) == 0)
            {
                SetWindowLong(hwnd, GwlExstyle, ex | WsExToolwindow);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
