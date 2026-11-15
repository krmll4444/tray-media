using System.Runtime.InteropServices;

namespace TrayMedia.Services;

/// <summary>
/// Global volume chords via a low-level keyboard hook (needed for Right Ctrl specifically;
/// <c>RegisterHotKey</c> cannot distinguish left/right Control).
/// </summary>
public sealed class VolumeHotkeyService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const uint LlkhfInjected = 0x10;

    private readonly object _gate = new();
    private IntPtr _hook = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private HotkeySpec _up = HotkeySpec.ParseOrDefault(null, HotkeySpec.DefaultVolumeUp);
    private HotkeySpec _down = HotkeySpec.ParseOrDefault(null, HotkeySpec.DefaultVolumeDown);
    private bool _enabled;
    private bool _paused;
    private bool _disposed;

    public void Apply(bool enabled, HotkeySpec volumeUp, HotkeySpec volumeDown)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _up = volumeUp;
            _down = volumeDown;
            _enabled = enabled;
            if (enabled && !_paused) EnsureHook();
            else RemoveHook();
        }
    }

    public void SetPaused(bool paused)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _paused = paused;
            if (_enabled && !_paused) EnsureHook();
            else RemoveHook();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _enabled = false;
            RemoveHook();
        }
    }

    private void EnsureHook()
    {
        if (_hook != IntPtr.Zero) return;
        _proc = HookCallback;
        // WH_KEYBOARD_LL is global; module handle of this process is enough on modern Windows.
        var hMod = GetModuleHandle(null);
        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, hMod, 0);
        if (_hook == IntPtr.Zero)
        {
            _proc = null;
        }
    }

    private void RemoveHook()
    {
        if (_hook == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            if (msg is WmKeydown or WmSyskeydown)
            {
                var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                if ((info.Flags & LlkhfInjected) == 0)
                {
                    HotkeySpec up;
                    HotkeySpec down;
                    lock (_gate)
                    {
                        if (!_enabled || _paused || _disposed)
                        {
                            return CallNextHookEx(_hook, nCode, wParam, lParam);
                        }

                        up = _up;
                        down = _down;
                    }

                    bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

                    if (up.Matches(info.VkCode, Down))
                    {
                        _ = SystemVolumeService.StepUp();
                        return (IntPtr)1;
                    }

                    if (down.Matches(info.VkCode, Down))
                    {
                        _ = SystemVolumeService.StepDown();
                        return (IntPtr)1;
                    }
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int VkCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
