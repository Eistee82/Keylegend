using Keylegend.Core.Devices;

namespace Keylegend.Windows;

/// <summary>
/// Notices that the user is using the keyboard, so the lighting can be taken over on the
/// first keypress and handed back once typing stops.
/// </summary>
/// <remarks>
/// Like <see cref="KeyboardStateReader"/> this only asks whether keys are currently down. It
/// does not record which key, does not keep a history, and never sees a keystroke the user
/// has already finished.
/// </remarks>
public sealed class ActivityTracker
{
    private readonly int[] _virtualKeys;

    /// <summary>
    /// Watches the keys the attached keyboard actually has, rather than sweeping all 256 codes.
    /// </summary>
    public ActivityTracker(AttachedKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        var keys = new HashSet<int>();

        foreach (var key in keyboard.Keys)
        {
            var scanCode = key.ScanCode is { } given
                ? (ushort)given
                : Core.Input.ScanCodes.TryGet(key.Id, out var known) ? known : (ushort)0;

            if (scanCode == 0)
            {
                continue;
            }

            var virtualKey = (int)NativeMethods.MapVirtualKeyEx(
                scanCode, NativeMethods.MAPVK_VSC_TO_VK_EX, NativeMethods.GetKeyboardLayout(0));

            if (virtualKey != 0)
            {
                keys.Add(virtualKey);
            }
        }

        _virtualKeys = [.. keys];
    }

    /// <summary>Whether any watched key is held down at this moment.</summary>
    public bool AnyKeyDown()
    {
        foreach (var virtualKey in _virtualKeys)
        {
            if (NativeMethods.IsDown(virtualKey))
            {
                return true;
            }
        }

        return false;
    }
}
