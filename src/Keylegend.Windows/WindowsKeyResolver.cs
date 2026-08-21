using System.Text;
using Keylegend.Core.Input;

namespace Keylegend.Windows;

/// <summary>
/// Asks Windows what a key would produce in a given keyboard state.
/// </summary>
/// <remarks>
/// This is what removes the need to ship layout tables: the active layout answers for itself,
/// so German, US, French or Dvorak all work without a line of code between them. It is also
/// why Shift, Caps Lock and Num Lock need no special handling anywhere else — the same key
/// simply reports a different character.
/// </remarks>
public sealed class WindowsKeyResolver : IKeyResolver
{
    private readonly StringBuilder _buffer = new(8);
    private readonly byte[] _keyState = new byte[256];
    private readonly Dictionary<CacheKey, KeyMeaning> _cache = [];

    private IntPtr _layout = NativeMethods.GetKeyboardLayout(0);

    /// <summary>
    /// Re-reads the active keyboard layout. Call when the foreground window changes, since the
    /// layout is per-thread and the user may have switched it.
    /// </summary>
    public void RefreshLayout()
    {
        var thread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
        var current = NativeMethods.GetKeyboardLayout(thread);

        if (current != _layout)
        {
            _layout = current;
            _cache.Clear();
        }
    }

    public KeyMeaning Resolve(string keyId, int? scanCodeOverride, KeyboardState state)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        var scanCode = scanCodeOverride is { } given
            ? (ushort)given
            : ScanCodes.TryGet(keyId, out var known) ? known : (ushort)0;

        if (scanCode == 0)
        {
            return KeyMeaning.Unassigned;
        }

        // Only the parts of the state that change the outcome belong in the cache key.
        var cacheKey = new CacheKey(
            scanCode,
            state.Shift,
            state.AltGr,
            state.Locks.CapsLock,
            state.Locks.NumLock);

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var meaning = Query(keyId, scanCode, state);
        _cache[cacheKey] = meaning;

        return meaning;
    }

    private KeyMeaning Query(string keyId, ushort scanCode, KeyboardState state)
    {
        uint virtualKey;

        // The number pad has to be resolved explicitly: Windows' scan-code mapping ignores
        // Num Lock and always answers with the navigation key, so the digit layer would never
        // show up. NumpadDigitsActive rather than the raw lock state, because holding Shift
        // suspends Num Lock.
        if (NumpadKeys.TryGetVirtualKey(keyId, state.NumpadDigitsActive, out var numpadKey))
        {
            if (numpadKey is null)
            {
                // The centre key with Num Lock off - genuinely does nothing.
                return KeyMeaning.Unassigned;
            }

            virtualKey = (uint)numpadKey.Value;
        }
        else
        {
            virtualKey = NativeMethods.MapVirtualKeyEx(scanCode, NativeMethods.MAPVK_VSC_TO_VK_EX, _layout);
        }

        if (virtualKey == 0)
        {
            return KeyMeaning.Unassigned;
        }

        Array.Clear(_keyState);

        if (state.Shift)
        {
            _keyState[NativeMethods.VK_SHIFT] = 0x80;
            _keyState[NativeMethods.VK_LSHIFT] = 0x80;
        }

        if (state.AltGr)
        {
            // AltGr is Ctrl + Alt as far as the layout is concerned; this is how Windows
            // itself represents it, so this is how it has to be asked.
            _keyState[NativeMethods.VK_CONTROL] = 0x80;
            _keyState[NativeMethods.VK_LCONTROL] = 0x80;
            _keyState[NativeMethods.VK_MENU] = 0x80;
            _keyState[NativeMethods.VK_RMENU] = 0x80;
        }

        if (state.Locks.CapsLock)
        {
            _keyState[NativeMethods.VK_CAPITAL] = 0x01;
        }

        if (state.Locks.NumLock)
        {
            _keyState[NativeMethods.VK_NUMLOCK] = 0x01;
        }

        _buffer.Clear();

        var result = NativeMethods.ToUnicodeEx(
            virtualKey,
            // Only the hardware scan code, without the 0xE0 marker that identifies extended
            // keys. MapVirtualKeyEx needs that marker to tell Home from numpad 7, but
            // ToUnicodeEx does not understand it and answers "no character" when it is
            // present - which is why the numpad divide key produced nothing.
            (uint)(scanCode & 0xFF),
            _keyState,
            _buffer,
            _buffer.Capacity,
            NativeMethods.TOUNICODE_DO_NOT_CHANGE_KEYBOARD_STATE,
            _layout);

        return result switch
        {
            // A dead key: produces nothing on its own, modifies the next keystroke.
            < 0 => new KeyMeaning(_buffer.ToString(), KeyCategory.DeadKey),

            // No character at all - a pure control key such as an arrow or a function key.
            0 => new KeyMeaning(null, KeyCategory.Control),

            _ => Classify(_buffer.ToString(0, Math.Min(result, _buffer.Length)))
        };
    }

    private static KeyMeaning Classify(string produced)
        => new(produced, CharacterClassifier.Classify(produced));

    private readonly record struct CacheKey(
        ushort ScanCode,
        bool Shift,
        bool AltGr,
        bool CapsLock,
        bool NumLock);
}
