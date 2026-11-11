using System.Runtime.InteropServices;
using Microsoft.Win32;
using TrayMedia.Models;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace TrayMedia.Services;

public static class WindowsChrome
{
    private static readonly MediaColor FallbackDark = MediaColor.FromRgb(31, 31, 31);
    private static readonly MediaColor FallbackLight = MediaColor.FromRgb(243, 243, 243);
    private static readonly MediaColor DefaultAccent = MediaColor.FromRgb(0, 120, 212);

    /// <summary>Apps theme (Settings / WPF windows), not taskbar chrome.</summary>
    public static bool AppsUseDarkTheme()
    {
        var appsUseLightTheme = ReadDword(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme");
        return appsUseLightTheme is null or 0;
    }

    public static ChromeTheme Read()
    {
        var enableTransparency = ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency");
        var colorPrevalence = ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "ColorPrevalence");
        var systemUsesLightTheme = ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        var colorizationColor = ReadDword(@"Software\Microsoft\Windows\DWM", "ColorizationColor");

        var transparency = enableTransparency == 1;
        var accentOnTaskbar = colorPrevalence == 1;
        var isDark = systemUsesLightTheme is null or 0;

        var accent = ReadAccentColor() ?? DefaultAccent;
        var colorization = colorizationColor is int dword ? ParseArgb(dword) : (MediaColor?)null;

        var taskbar = isDark ? FallbackDark : FallbackLight;
        if (accentOnTaskbar && colorization is MediaColor c)
        {
            taskbar = MediaColor.FromRgb(c.R, c.G, c.B);
        }
        else if (accentOnTaskbar)
        {
            taskbar = accent;
        }

        var darkChrome = Luminance(taskbar) < 0.45;
        // WPF paints this fill itself (AllowsTransparency). Slight alpha when system
        // transparency is on — enough to feel like taskbar tint without DWM acrylic bugs.
        var fillAlpha = transparency ? (darkChrome ? (byte)210 : (byte)220) : (byte)255;
        var fill = MediaColor.FromArgb(fillAlpha, taskbar.R, taskbar.G, taskbar.B);

        return new ChromeTheme
        {
            Transparency = transparency,
            IsDark = darkChrome,
            Accent = accent,
            TaskbarRgb = taskbar,
            Background = Freeze(new SolidColorBrush(fill)),
            FallbackBackground = Freeze(new SolidColorBrush(fill)),
            Border = Freeze(new SolidColorBrush(darkChrome
                ? MediaColor.FromArgb(41, 255, 255, 255)
                : MediaColor.FromArgb(41, 0, 0, 0))),
            Text = Freeze(new SolidColorBrush(darkChrome ? MediaColors.White : MediaColor.FromRgb(26, 26, 26))),
            TextSecondary = Freeze(new SolidColorBrush(darkChrome
                ? MediaColor.FromRgb(200, 200, 200)
                : MediaColor.FromRgb(93, 93, 93))),
            ButtonHover = Freeze(new SolidColorBrush(darkChrome
                ? MediaColor.FromArgb(20, 255, 255, 255)
                : MediaColor.FromArgb(15, 0, 0, 0))),
            ButtonActive = Freeze(new SolidColorBrush(darkChrome
                ? MediaColor.FromArgb(36, 255, 255, 255)
                : MediaColor.FromArgb(26, 0, 0, 0))),
            ProgressTrack = Freeze(new SolidColorBrush(darkChrome
                ? MediaColor.FromArgb(56, 255, 255, 255)
                : MediaColor.FromArgb(46, 0, 0, 0))),
            AccentBrush = Freeze(new SolidColorBrush(accent)),
            BlurPx = transparency ? 28 : 0,
        };
    }

    private static MediaBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    private static int? ReadDword(string subKey, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey);
            var value = key?.GetValue(name);
            return value switch
            {
                int i => i,
                uint u => unchecked((int)u),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    private static MediaColor ParseArgb(int dword)
    {
        var u = unchecked((uint)dword);
        return MediaColor.FromArgb(
            (byte)((u >> 24) & 0xff),
            (byte)((u >> 16) & 0xff),
            (byte)((u >> 8) & 0xff),
            (byte)(u & 0xff));
    }

    private static MediaColor ParseAbgr(int dword)
    {
        var u = unchecked((uint)dword);
        // Windows AccentColor is stored as 0xAABBGGRR
        return MediaColor.FromArgb(
            (byte)((u >> 24) & 0xff),
            (byte)(u & 0xff),
            (byte)((u >> 8) & 0xff),
            (byte)((u >> 16) & 0xff));
    }

    private static double Luminance(MediaColor c) =>
        (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    private static MediaColor? ReadAccentColor()
    {
        // 1) WinRT UISettings — canonical accent
        try
        {
            var settings = new Windows.UI.ViewManagement.UISettings();
            var c = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
            return MediaColor.FromArgb(c.A, c.R, c.G, c.B);
        }
        catch
        {
            // continue
        }

        // 2) HKCU\...\DWM\AccentColor (ABGR)
        var accentReg = ReadDword(@"Software\Microsoft\Windows\DWM", "AccentColor");
        if (accentReg is int accentDword)
        {
            var c = ParseAbgr(accentDword);
            return MediaColor.FromRgb(c.R, c.G, c.B);
        }

        // 3) DwmGetColorizationColor (ARGB)
        try
        {
            if (DwmGetColorizationColor(out var color, out _) == 0)
            {
                var c = ParseArgb(unchecked((int)color));
                return MediaColor.FromRgb(c.R, c.G, c.B);
            }
        }
        catch
        {
            // continue
        }

        // 4) AccentPalette first color (RGB)
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentPalette") is byte[] palette && palette.Length >= 4)
            {
                return MediaColor.FromRgb(palette[0], palette[1], palette[2]);
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
}
