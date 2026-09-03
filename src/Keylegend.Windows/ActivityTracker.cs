using Keylegend.Core.Devices;

namespace Keylegend.Windows;

/// <summary>
/// Notices that the user is using the keyboard, so the lighting can be taken over on the
/// first keypress and handed back once typing stops.
/// </summary>
/// <remarks>
/// <para>
/// Like <see cref="KeyboardStateReader"/> this only asks whether keys are currently down. It
/// polls: no hook is installed, nothing is intercepted, forwarded or written down, and it never
/// sees a keystroke the user has already finished.
/// </para>
/// <para>
/// It can also name which of them are down, which is what the keystroke effects are made of. That
/// is asked for only while an effect is selected — with none, <see cref="PressedKeys"/> is never
/// called and nothing beyond "is anybody typing" is ever looked at.
/// </para>
/// </remarks>
public sealed class ActivityTracker
{
    private readonly (string Id, int VirtualKey)[] _watched;
    private readonly Func<int, bool> _down;

    /// <summary>
    /// Watches the keys the attached keyboard actually has, rather than sweeping all 256 codes.
    /// </summary>
    /// <param name="down">
    /// How to ask whether one key is down. Defaults to asking Windows; handed in so the mapping
    /// from key ids to what is polled can be tested without a keyboard under the fingers.
    /// </param>
    public ActivityTracker(AttachedKeyboard keyboard, Func<int, bool>? down = null)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        _down = down ?? NativeMethods.IsDown;

        var watched = new List<(string, int)>();
        var seen = new HashSet<int>();

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

            // Two ids landing on one virtual key would have the same key answer for both, and
            // the first one named wins — as it did before this could name them at all.
            if (virtualKey != 0 && seen.Add(virtualKey))
            {
                watched.Add((key.Id, virtualKey));
            }
        }

        _watched = [.. watched];
    }

    /// <summary>Whether any watched key is held down at this moment.</summary>
    public bool AnyKeyDown()
    {
        foreach (var (_, virtualKey) in _watched)
        {
            if (_down(virtualKey))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Which watched keys are held down at this moment, by key id.
    /// </summary>
    /// <remarks>
    /// Asked only while a keystroke effect is selected. The same poll as
    /// <see cref="AnyKeyDown"/>, carried through to the end instead of stopping at the first
    /// key that answers.
    /// </remarks>
    public IReadOnlyList<string> PressedKeys()
    {
        List<string>? down = null;

        foreach (var (id, virtualKey) in _watched)
        {
            if (_down(virtualKey))
            {
                (down ??= []).Add(id);
            }
        }

        return down ?? [];
    }
}
