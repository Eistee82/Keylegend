namespace Keylegend.Core.Input;

/// <summary>
/// Which virtual key a number pad key stands for, depending on Num Lock.
/// </summary>
/// <param name="WithNumLock">Virtual key while Num Lock is on — the digit.</param>
/// <param name="WithoutNumLock">
/// Virtual key while Num Lock is off — the navigation command, or <c>null</c> where the key
/// has no function at all. The centre key is the only such case: with Num Lock off it reports
/// "clear", which does nothing, so it should not light up.
/// </param>
public readonly record struct NumpadKey(int WithNumLock, int? WithoutNumLock);

/// <summary>
/// The number pad needs its own table because the scan-code-to-virtual-key mapping Windows
/// offers does not take Num Lock into account: asked about the "7" key it always answers
/// "Home", so the digit layer would never appear. Resolving it explicitly is what makes the
/// pad recolour between digits and navigation as the toggle is flipped.
/// </summary>
public static class NumpadKeys
{
    // Digits and decimal separator
    private const int VK_NUMPAD0 = 0x60;
    private const int VK_DECIMAL = 0x6E;

    // Navigation equivalents
    private const int VK_PRIOR = 0x21;   // Page Up
    private const int VK_NEXT = 0x22;    // Page Down
    private const int VK_END = 0x23;
    private const int VK_HOME = 0x24;
    private const int VK_LEFT = 0x25;
    private const int VK_UP = 0x26;
    private const int VK_RIGHT = 0x27;
    private const int VK_DOWN = 0x28;
    private const int VK_INSERT = 0x2D;
    private const int VK_DELETE = 0x2E;

    // Operators - unaffected by Num Lock
    private const int VK_MULTIPLY = 0x6A;
    private const int VK_ADD = 0x6B;
    private const int VK_SUBTRACT = 0x6D;
    private const int VK_DIVIDE = 0x6F;
    private const int VK_RETURN = 0x0D;

    private static readonly Dictionary<string, NumpadKey> Table = new(StringComparer.Ordinal)
    {
        ["Keyboard_Num0"] = new(VK_NUMPAD0 + 0, VK_INSERT),
        ["Keyboard_Num1"] = new(VK_NUMPAD0 + 1, VK_END),
        ["Keyboard_Num2"] = new(VK_NUMPAD0 + 2, VK_DOWN),
        ["Keyboard_Num3"] = new(VK_NUMPAD0 + 3, VK_NEXT),
        ["Keyboard_Num4"] = new(VK_NUMPAD0 + 4, VK_LEFT),

        // The centre key has no meaning with Num Lock off.
        ["Keyboard_Num5"] = new(VK_NUMPAD0 + 5, null),

        ["Keyboard_Num6"] = new(VK_NUMPAD0 + 6, VK_RIGHT),
        ["Keyboard_Num7"] = new(VK_NUMPAD0 + 7, VK_HOME),
        ["Keyboard_Num8"] = new(VK_NUMPAD0 + 8, VK_UP),
        ["Keyboard_Num9"] = new(VK_NUMPAD0 + 9, VK_PRIOR),
        ["Keyboard_NumPeriodAndDelete"] = new(VK_DECIMAL, VK_DELETE),

        // Operators keep their meaning either way.
        ["Keyboard_NumSlash"] = new(VK_DIVIDE, VK_DIVIDE),
        ["Keyboard_NumAsterisk"] = new(VK_MULTIPLY, VK_MULTIPLY),
        ["Keyboard_NumMinus"] = new(VK_SUBTRACT, VK_SUBTRACT),
        ["Keyboard_NumPlus"] = new(VK_ADD, VK_ADD),
        ["Keyboard_NumEnter"] = new(VK_RETURN, VK_RETURN)
    };

    /// <summary>All number pad keys, for diagnostics and tests.</summary>
    public static IReadOnlyDictionary<string, NumpadKey> All => Table;

    /// <summary>Whether this key belongs to the number pad.</summary>
    public static bool IsNumpadKey(string keyId) => Table.ContainsKey(keyId);

    /// <summary>
    /// Resolves the virtual key for the current Num Lock state.
    /// </summary>
    /// <returns>
    /// <c>false</c> if this is not a number pad key at all. When it is, <paramref name="virtualKey"/>
    /// is <c>null</c> for a key that has no function in the current state.
    /// </returns>
    public static bool TryGetVirtualKey(string keyId, bool numLock, out int? virtualKey)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        if (!Table.TryGetValue(keyId, out var entry))
        {
            virtualKey = null;
            return false;
        }

        virtualKey = numLock ? entry.WithNumLock : entry.WithoutNumLock;

        return true;
    }
}
