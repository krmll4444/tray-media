using System.Runtime.InteropServices;

namespace TrayMedia.Services;

/// <summary>
/// Steps system volume by synthesizing the hardware volume keys.
/// That path is what shows the Windows volume OSD ("качелька");
/// direct Core Audio <c>VolumeStepUp/Down</c> changes level silently.
/// </summary>
public static class SystemVolumeService
{
    private const byte VkVolumeMute = 0xAD;
    private const byte VkVolumeDown = 0xAE;
    private const byte VkVolumeUp = 0xAF;
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    public static bool StepUp() => Tap(VkVolumeUp);

    public static bool StepDown() => Tap(VkVolumeDown);

    public static bool ToggleMute() => Tap(VkVolumeMute);

    private static bool Tap(byte vk)
    {
        try
        {
            var inputs = new Input[2];
            inputs[0] = new Input
            {
                Type = InputKeyboard,
                U = new InputUnion
                {
                    Ki = new KeybdInput { WVk = vk },
                },
            };
            inputs[1] = new Input
            {
                Type = InputKeyboard,
                U = new InputUnion
                {
                    Ki = new KeybdInput { WVk = vk, DwFlags = KeyeventfKeyup },
                },
            };

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            return sent == inputs.Length;
        }
        catch
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeybdInput Ki;
        [FieldOffset(0)] public MouseInput Mi;
        [FieldOffset(0)] public HardwareInput Hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeybdInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint UMsg;
        public ushort WParamL;
        public ushort WParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
}
