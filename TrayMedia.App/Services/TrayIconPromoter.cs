using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TrayMedia.Services;

/// <summary>
/// Promotes/demotes the notification icon between the visible tray and the overflow.
/// Win10: undocumented ITrayNotify.SetPreference.
/// Win11: HKCU\Control Panel\NotifyIconSettings\*\IsPromoted.
/// </summary>
public static class TrayIconPromoter
{
    private const int PreferenceShowWhenActive = 0; // overflow / hidden apps
    private const int PreferenceShowAlways = 2;     // pinned to tray

    public static void Apply(bool pinToTray, IntPtr hwnd, uint iconId)
    {
        try
        {
            if (IsWindows11OrNewer())
            {
                ApplyWin11(pinToTray);
            }

            ApplyWin10Com(pinToTray, hwnd, iconId);
        }
        catch
        {
            // best-effort; Explorer schemas vary
        }
    }

    private static bool IsWindows11OrNewer()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var buildStr = key?.GetValue("CurrentBuildNumber") as string
                           ?? key?.GetValue("CurrentBuild") as string;
            return int.TryParse(buildStr, out var build) && build >= 22000;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyWin11(bool pinToTray)
    {
        const string root = @"Control Panel\NotifyIconSettings";
        using var parent = Registry.CurrentUser.OpenSubKey(root, writable: true);
        if (parent is null) return;

        var exePath = (Environment.ProcessPath
                       ?? Process.GetCurrentProcess().MainModule?.FileName
                       ?? "").ToLowerInvariant();
        var exeName = Path.GetFileNameWithoutExtension(exePath);

        foreach (var subName in parent.GetSubKeyNames())
        {
            using var sub = parent.OpenSubKey(subName, writable: true);
            if (sub is null) continue;

            var path = (sub.GetValue("ExecutablePath") as string ?? "").ToLowerInvariant();
            var match = (!string.IsNullOrEmpty(exePath) && path.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrEmpty(exeName) && path.Contains(exeName, StringComparison.OrdinalIgnoreCase));
            if (!match) continue;

            // Respect an existing explicit hide only when we're not forcing pin from Settings.
            sub.SetValue("IsPromoted", pinToTray ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    private static void ApplyWin10Com(bool pinToTray, IntPtr hwnd, uint iconId)
    {
        var preference = pinToTray ? PreferenceShowAlways : PreferenceShowWhenActive;
        var instance = new TrayNotify();
        try
        {
            var notifier = (ITrayNotify)instance;

            // Direct set by hwnd/uID (ipmsg-style) — works when Explorer already knows the icon.
            var direct = new NotifyItemWritable
            {
                hwnd = hwnd,
                preference = preference,
                id = iconId,
            };
            try
            {
                notifier.SetPreference(ref direct);
            }
            catch
            {
                // fall through to enumerate
            }

            // Enumerate and match our process, then set preference.
            var callback = new NotificationCb();
            notifier.RegisterCallback(callback, out var handle);
            notifier.UnregisterCallback(handle);

            var exeHint = Path.GetFileNameWithoutExtension(
                Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? "TrayMedia").ToLowerInvariant();

            foreach (var item in callback.Items)
            {
                var exe = (item.exe_name ?? "").ToLowerInvariant();
                var sameHwnd = item.hwnd == hwnd && item.id == iconId;
                var sameExe = !string.IsNullOrEmpty(exeHint)
                              && exe.IndexOf(exeHint, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!sameHwnd && !sameExe) continue;

                var writable = NotifyItemWritable.FromNotifyItem(item);
                writable.preference = preference;
                notifier.SetPreference(ref writable);
                FreeWritable(ref writable);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(instance);
        }
    }

    private static void FreeWritable(ref NotifyItemWritable item)
    {
        if (item.exe_name != IntPtr.Zero) Marshal.FreeCoTaskMem(item.exe_name);
        if (item.tip != IntPtr.Zero) Marshal.FreeCoTaskMem(item.tip);
        item.exe_name = IntPtr.Zero;
        item.tip = IntPtr.Zero;
    }

    private sealed class NotificationCb : INotificationCb
    {
        public List<NotifyItem> Items { get; } = new();

        public void Notify([In] uint nEvent, [In] ref NotifyItem notifyItem) => Items.Add(notifyItem);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyItem
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string exe_name;
        [MarshalAs(UnmanagedType.LPWStr)] public string tip;
        public IntPtr icon;
        public IntPtr hwnd;
        public int preference;
        public uint id;
        public Guid guid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NotifyItemWritable
    {
        public IntPtr exe_name;
        public IntPtr tip;
        public IntPtr icon;
        public IntPtr hwnd;
        public int preference;
        public uint id;
        public Guid guid;
        // Win10 10586+ may read extra slots; keep them null.
        public IntPtr extra0;
        public IntPtr extra1;
        public IntPtr extra2;
        public IntPtr extra3;
        public IntPtr extra4;
        public IntPtr extra5;
        public IntPtr extra6;
        public IntPtr extra7;

        public static NotifyItemWritable FromNotifyItem(NotifyItem item) => new()
        {
            exe_name = Marshal.StringToCoTaskMemUni(item.exe_name),
            tip = Marshal.StringToCoTaskMemUni(item.tip),
            icon = item.icon,
            hwnd = item.hwnd,
            preference = item.preference,
            id = item.id,
            guid = item.guid,
        };
    }

    [ComImport]
    [Guid("D782CCBA-AFB0-43F1-94DB-FDA3779EACCB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface INotificationCb
    {
        void Notify([In] uint nEvent, [In] ref NotifyItem notifyItem);
    }

    [ComImport]
    [Guid("D133CE13-3537-48BA-93A7-AFCD5D2053B4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITrayNotify
    {
        void RegisterCallback([MarshalAs(UnmanagedType.Interface)] INotificationCb callback, out ulong handle);
        void UnregisterCallback([In] ulong handle);
        void SetPreference([In] ref NotifyItemWritable notifyItem);
        void EnableAutoTray([In] bool enabled);
        void DoAction([In] bool enabled);
    }

    [ComImport, Guid("25DEAD04-1EAC-4911-9E3A-AD0A4AB560FD")]
    private class TrayNotify
    {
    }
}
