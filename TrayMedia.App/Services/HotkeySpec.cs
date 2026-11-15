using System.Text;
using System.Windows.Input;

namespace TrayMedia.Services;

/// <summary>
/// Serializable keyboard chord. Storage form: <c>RControl+PageUp</c>.
/// Distinguishes left/right modifiers so defaults can require Right Ctrl specifically.
/// </summary>
public sealed class HotkeySpec : IEquatable<HotkeySpec>
{
    public const string DefaultVolumeUp = "RControl+PageUp";
    public const string DefaultVolumeDown = "RControl+PageDown";

    private static readonly Dictionary<string, int> NameToVk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["Prior"] = 0x21,
        ["Next"] = 0x22,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Up"] = 0x26,
        ["Down"] = 0x28,
        ["Left"] = 0x25,
        ["Right"] = 0x27,
        ["Space"] = 0x20,
        ["Tab"] = 0x09,
        ["OemPlus"] = 0xBB,
        ["OemMinus"] = 0xBD,
        ["Add"] = 0x6B,
        ["Subtract"] = 0x6D,
        ["VolumeUp"] = 0xAF,
        ["VolumeDown"] = 0xAE,
    };

    private static readonly Dictionary<int, string> VkToName = new()
    {
        [0x21] = "PageUp",
        [0x22] = "PageDown",
        [0x24] = "Home",
        [0x23] = "End",
        [0x2D] = "Insert",
        [0x2E] = "Delete",
        [0x26] = "Up",
        [0x28] = "Down",
        [0x25] = "Left",
        [0x27] = "Right",
        [0x20] = "Space",
        [0x09] = "Tab",
        [0xBB] = "OemPlus",
        [0xBD] = "OemMinus",
        [0x6B] = "Add",
        [0x6D] = "Subtract",
    };

    public bool LeftCtrl { get; init; }
    public bool RightCtrl { get; init; }
    public bool LeftAlt { get; init; }
    public bool RightAlt { get; init; }
    public bool LeftShift { get; init; }
    public bool RightShift { get; init; }
    public bool Win { get; init; }
    public int VirtualKey { get; init; }

    public bool HasModifier =>
        LeftCtrl || RightCtrl || LeftAlt || RightAlt || LeftShift || RightShift || Win;

    public static HotkeySpec ParseOrDefault(string? value, string fallback)
    {
        if (TryParse(value, out var spec) && spec.IsValid()) return spec;
        return TryParse(fallback, out var def) ? def : new HotkeySpec { RightCtrl = true, VirtualKey = 0x21 };
    }

    public static bool TryParse(string? value, out HotkeySpec spec)
    {
        spec = new HotkeySpec();
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;

        var leftCtrl = false;
        var rightCtrl = false;
        var eitherCtrl = false;
        var leftAlt = false;
        var rightAlt = false;
        var eitherAlt = false;
        var leftShift = false;
        var rightShift = false;
        var eitherShift = false;
        var win = false;
        int? key = null;

        foreach (var raw in parts)
        {
            var part = raw.Trim();
            if (part.Equals("RControl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("RightCtrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("RightControl", StringComparison.OrdinalIgnoreCase))
            {
                rightCtrl = true;
            }
            else if (part.Equals("LControl", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("LeftCtrl", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("LeftControl", StringComparison.OrdinalIgnoreCase))
            {
                leftCtrl = true;
            }
            else if (part.Equals("Control", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                eitherCtrl = true;
            }
            else if (part.Equals("RAlt", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("RightAlt", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("AltGr", StringComparison.OrdinalIgnoreCase))
            {
                rightAlt = true;
            }
            else if (part.Equals("LAlt", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("LeftAlt", StringComparison.OrdinalIgnoreCase))
            {
                leftAlt = true;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                eitherAlt = true;
            }
            else if (part.Equals("RShift", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("RightShift", StringComparison.OrdinalIgnoreCase))
            {
                rightShift = true;
            }
            else if (part.Equals("LShift", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("LeftShift", StringComparison.OrdinalIgnoreCase))
            {
                leftShift = true;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                eitherShift = true;
            }
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                     part.Equals("Meta", StringComparison.OrdinalIgnoreCase))
            {
                win = true;
            }
            else if (TryResolveKey(part, out var vk))
            {
                if (key is not null) return false;
                key = vk;
            }
            else
            {
                return false;
            }
        }

        if (key is null) return false;

        // "Control" means either side; side-specific flags stay exclusive when set.
        if (eitherCtrl && !leftCtrl && !rightCtrl)
        {
            leftCtrl = true;
            rightCtrl = true;
        }

        if (eitherAlt && !leftAlt && !rightAlt)
        {
            leftAlt = true;
            rightAlt = true;
        }

        if (eitherShift && !leftShift && !rightShift)
        {
            leftShift = true;
            rightShift = true;
        }

        spec = new HotkeySpec
        {
            LeftCtrl = leftCtrl,
            RightCtrl = rightCtrl,
            LeftAlt = leftAlt,
            RightAlt = rightAlt,
            LeftShift = leftShift,
            RightShift = rightShift,
            Win = win,
            VirtualKey = key.Value,
        };
        return spec.IsValid();
    }

    public static HotkeySpec? FromKeyEvent(Key key, ModifierKeys modifiers, bool leftCtrl, bool rightCtrl,
        bool leftAlt, bool rightAlt, bool leftShift, bool rightShift, bool win)
    {
        if (IsModifierKey(key)) return null;

        var vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk <= 0) return null;

        // Prefer explicit L/R state; if ModifierKeys says Control/Alt/Shift but sides are unknown, accept either.
        ResolveSides(modifiers & ModifierKeys.Control, ref leftCtrl, ref rightCtrl);
        ResolveSides(modifiers & ModifierKeys.Alt, ref leftAlt, ref rightAlt);
        ResolveSides(modifiers & ModifierKeys.Shift, ref leftShift, ref rightShift);
        if ((modifiers & ModifierKeys.Windows) != 0) win = true;

        var spec = new HotkeySpec
        {
            LeftCtrl = leftCtrl,
            RightCtrl = rightCtrl,
            LeftAlt = leftAlt,
            RightAlt = rightAlt,
            LeftShift = leftShift,
            RightShift = rightShift,
            Win = win,
            VirtualKey = vk,
        };

        return spec.IsValid() ? spec : null;
    }

    private static void ResolveSides(ModifierKeys present, ref bool left, ref bool right)
    {
        if (present == 0) return;
        if (!left && !right)
        {
            left = true;
            right = true;
        }
    }

    public bool IsValid() => VirtualKey > 0 && HasModifier && !IsModifierVk(VirtualKey);

    public string ToStorageString()
    {
        var sb = new StringBuilder();
        AppendMods(sb, storage: true);
        if (sb.Length > 0) sb.Append('+');
        sb.Append(KeyName(VirtualKey));
        return sb.ToString();
    }

    public string ToDisplayString()
    {
        var sb = new StringBuilder();
        AppendMods(sb, storage: false);
        if (sb.Length > 0) sb.Append(" + ");
        sb.Append(DisplayKeyName(VirtualKey));
        return sb.ToString();
    }

    /// <summary>
    /// Returns true when <paramref name="vkCode"/> is the chord's key and required modifiers are down.
    /// </summary>
    public bool Matches(int vkCode, Func<int, bool> isKeyDown)
    {
        if (vkCode != VirtualKey) return false;
        if (!HasModifier) return false;

        if (!MatchSide(LeftCtrl, RightCtrl, isKeyDown(0xA2), isKeyDown(0xA3))) return false;
        if (!MatchSide(LeftAlt, RightAlt, isKeyDown(0xA4), isKeyDown(0xA5))) return false;
        if (!MatchSide(LeftShift, RightShift, isKeyDown(0xA0), isKeyDown(0xA1))) return false;

        if (Win)
        {
            if (!isKeyDown(0x5B) && !isKeyDown(0x5C)) return false;
        }

        return true;
    }

    public bool Equals(HotkeySpec? other)
    {
        if (other is null) return false;
        return LeftCtrl == other.LeftCtrl
               && RightCtrl == other.RightCtrl
               && LeftAlt == other.LeftAlt
               && RightAlt == other.RightAlt
               && LeftShift == other.LeftShift
               && RightShift == other.RightShift
               && Win == other.Win
               && VirtualKey == other.VirtualKey;
    }

    public override bool Equals(object? obj) => obj is HotkeySpec other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(LeftCtrl, RightCtrl, LeftAlt, RightAlt, LeftShift, RightShift, Win, VirtualKey);

    public override string ToString() => ToStorageString();

    private void AppendMods(StringBuilder sb, bool storage)
    {
        void Add(string token)
        {
            if (sb.Length > 0) sb.Append(storage ? "+" : " + ");
            sb.Append(token);
        }

        if (LeftCtrl && RightCtrl) Add(storage ? "Control" : "Ctrl");
        else if (RightCtrl) Add(storage ? "RControl" : "Right Ctrl");
        else if (LeftCtrl) Add(storage ? "LControl" : "Left Ctrl");

        if (LeftAlt && RightAlt) Add("Alt");
        else if (RightAlt) Add(storage ? "RAlt" : "Right Alt");
        else if (LeftAlt) Add(storage ? "LAlt" : "Left Alt");

        if (LeftShift && RightShift) Add("Shift");
        else if (RightShift) Add(storage ? "RShift" : "Right Shift");
        else if (LeftShift) Add(storage ? "LShift" : "Left Shift");

        if (Win) Add(storage ? "Win" : "Win");
    }

    private static bool MatchSide(bool wantLeft, bool wantRight, bool leftDown, bool rightDown)
    {
        if (!wantLeft && !wantRight) return true;
        if (wantLeft && wantRight) return leftDown || rightDown;
        if (wantLeft) return leftDown;
        return rightDown;
    }

    private static bool TryResolveKey(string part, out int vk)
    {
        if (NameToVk.TryGetValue(part, out vk)) return true;

        if (part.Length == 1)
        {
            var ch = char.ToUpperInvariant(part[0]);
            if (ch is >= 'A' and <= 'Z')
            {
                vk = ch;
                return true;
            }

            if (ch is >= '0' and <= '9')
            {
                vk = ch;
                return true;
            }
        }

        if (part.StartsWith("F", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(part[1..], out var f)
            && f is >= 1 and <= 24)
        {
            vk = 0x70 + (f - 1);
            return true;
        }

        if (part.StartsWith("D", StringComparison.OrdinalIgnoreCase)
            && part.Length == 2
            && part[1] is >= '0' and <= '9')
        {
            vk = part[1];
            return true;
        }

        if (Enum.TryParse<Key>(part, ignoreCase: true, out var key) && !IsModifierKey(key))
        {
            vk = KeyInterop.VirtualKeyFromKey(key);
            return vk > 0;
        }

        vk = 0;
        return false;
    }

    private static string KeyName(int vk)
    {
        if (VkToName.TryGetValue(vk, out var name)) return name;
        if (vk is >= 0x70 and <= 0x87) return "F" + (vk - 0x70 + 1);
        if (vk is >= 'A' and <= 'Z') return ((char)vk).ToString();
        if (vk is >= '0' and <= '9') return ((char)vk).ToString();
        return "Vk" + vk.ToString("X2");
    }

    private static string DisplayKeyName(int vk)
    {
        return vk switch
        {
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x20 => "Space",
            0x2E => "Delete",
            0x2D => "Insert",
            _ => KeyName(vk) switch
            {
                "OemPlus" => "+",
                "OemMinus" => "-",
                "Add" => "Num +",
                "Subtract" => "Num -",
                var n => n,
            },
        };
    }

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.System;

    private static bool IsModifierVk(int vk) =>
        vk is 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C
            or 0x10 or 0x11 or 0x12;
}
