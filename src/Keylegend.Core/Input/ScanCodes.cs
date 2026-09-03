namespace Keylegend.Core.Input;

/// <summary>
/// Maps key identifiers to Set 1 scan codes. A scan code describes a <em>physical position</em>
/// on the keyboard and is therefore independent of the software layout — which is exactly what
/// is needed before asking Windows what a key currently produces.
/// </summary>
/// <remarks>
/// The table follows the US layout, matching the naming convention used for key ids. Where a
/// physical layout disagrees — on ISO keyboards the tall Enter covers the position ANSI uses
/// for backslash — the key carries an explicit <c>scanCode</c> that wins over this table.
/// </remarks>
public static class ScanCodes
{
    /// <summary>Extended keys are prefixed with 0xE0 in the high byte.</summary>
    public const ushort ExtendedPrefix = 0xE000;

    private static readonly Dictionary<string, ushort> Table = new(StringComparer.Ordinal)
    {
        ["Keyboard_Escape"] = 0x01,
        ["Keyboard_1"] = 0x02, ["Keyboard_2"] = 0x03, ["Keyboard_3"] = 0x04,
        ["Keyboard_4"] = 0x05, ["Keyboard_5"] = 0x06, ["Keyboard_6"] = 0x07,
        ["Keyboard_7"] = 0x08, ["Keyboard_8"] = 0x09, ["Keyboard_9"] = 0x0A,
        ["Keyboard_0"] = 0x0B,
        ["Keyboard_MinusAndUnderscore"] = 0x0C,
        ["Keyboard_EqualsAndPlus"] = 0x0D,
        ["Keyboard_Backspace"] = 0x0E,
        ["Keyboard_Tab"] = 0x0F,
        ["Keyboard_Q"] = 0x10, ["Keyboard_W"] = 0x11, ["Keyboard_E"] = 0x12,
        ["Keyboard_R"] = 0x13, ["Keyboard_T"] = 0x14, ["Keyboard_Y"] = 0x15,
        ["Keyboard_U"] = 0x16, ["Keyboard_I"] = 0x17, ["Keyboard_O"] = 0x18,
        ["Keyboard_P"] = 0x19,
        ["Keyboard_BracketLeft"] = 0x1A,
        ["Keyboard_BracketRight"] = 0x1B,
        ["Keyboard_Enter"] = 0x1C,
        ["Keyboard_LeftCtrl"] = 0x1D,
        ["Keyboard_A"] = 0x1E, ["Keyboard_S"] = 0x1F, ["Keyboard_D"] = 0x20,
        ["Keyboard_F"] = 0x21, ["Keyboard_G"] = 0x22, ["Keyboard_H"] = 0x23,
        ["Keyboard_J"] = 0x24, ["Keyboard_K"] = 0x25, ["Keyboard_L"] = 0x26,
        ["Keyboard_SemicolonAndColon"] = 0x27,
        ["Keyboard_ApostropheAndDoubleQuote"] = 0x28,
        ["Keyboard_GraveAccentAndTilde"] = 0x29,
        ["Keyboard_LeftShift"] = 0x2A,
        ["Keyboard_Backslash"] = 0x2B,
        ["Keyboard_NonUsTilde"] = 0x2B,     // ISO: the # key sits on the ANSI backslash position
        ["Keyboard_Z"] = 0x2C, ["Keyboard_X"] = 0x2D, ["Keyboard_C"] = 0x2E,
        ["Keyboard_V"] = 0x2F, ["Keyboard_B"] = 0x30, ["Keyboard_N"] = 0x31,
        ["Keyboard_M"] = 0x32,
        ["Keyboard_CommaAndLessThan"] = 0x33,
        ["Keyboard_PeriodAndBiggerThan"] = 0x34,
        ["Keyboard_SlashAndQuestionMark"] = 0x35,
        ["Keyboard_RightShift"] = 0x36,
        ["Keyboard_NumAsterisk"] = 0x37,
        ["Keyboard_LeftAlt"] = 0x38,
        ["Keyboard_Space"] = 0x39,
        ["Keyboard_CapsLock"] = 0x3A,
        ["Keyboard_F1"] = 0x3B, ["Keyboard_F2"] = 0x3C, ["Keyboard_F3"] = 0x3D,
        ["Keyboard_F4"] = 0x3E, ["Keyboard_F5"] = 0x3F, ["Keyboard_F6"] = 0x40,
        ["Keyboard_F7"] = 0x41, ["Keyboard_F8"] = 0x42, ["Keyboard_F9"] = 0x43,
        ["Keyboard_F10"] = 0x44,
        ["Keyboard_NumLock"] = 0x45,
        ["Keyboard_ScrollLock"] = 0x46,
        ["Keyboard_Num7"] = 0x47, ["Keyboard_Num8"] = 0x48, ["Keyboard_Num9"] = 0x49,
        ["Keyboard_NumMinus"] = 0x4A,
        ["Keyboard_Num4"] = 0x4B, ["Keyboard_Num5"] = 0x4C, ["Keyboard_Num6"] = 0x4D,
        ["Keyboard_NumPlus"] = 0x4E,
        ["Keyboard_Num1"] = 0x4F, ["Keyboard_Num2"] = 0x50, ["Keyboard_Num3"] = 0x51,
        ["Keyboard_Num0"] = 0x52,
        ["Keyboard_NumPeriodAndDelete"] = 0x53,
        ["Keyboard_NonUsBackslash"] = 0x56,   // the extra ISO key left of Y/Z
        ["Keyboard_F11"] = 0x57,
        ["Keyboard_F12"] = 0x58,

        // Extended keys
        ["Keyboard_NumEnter"] = ExtendedPrefix | 0x1C,
        ["Keyboard_RightCtrl"] = ExtendedPrefix | 0x1D,
        ["Keyboard_NumSlash"] = ExtendedPrefix | 0x35,
        ["Keyboard_PrintScreen"] = ExtendedPrefix | 0x37,
        ["Keyboard_RightAlt"] = ExtendedPrefix | 0x38,
        ["Keyboard_Home"] = ExtendedPrefix | 0x47,
        ["Keyboard_ArrowUp"] = ExtendedPrefix | 0x48,
        ["Keyboard_PageUp"] = ExtendedPrefix | 0x49,
        ["Keyboard_ArrowLeft"] = ExtendedPrefix | 0x4B,
        ["Keyboard_ArrowRight"] = ExtendedPrefix | 0x4D,
        ["Keyboard_End"] = ExtendedPrefix | 0x4F,
        ["Keyboard_ArrowDown"] = ExtendedPrefix | 0x50,
        ["Keyboard_PageDown"] = ExtendedPrefix | 0x51,
        ["Keyboard_Insert"] = ExtendedPrefix | 0x52,
        ["Keyboard_Delete"] = ExtendedPrefix | 0x53,
        ["Keyboard_LeftGui"] = ExtendedPrefix | 0x5B,
        ["Keyboard_RightGui"] = ExtendedPrefix | 0x5C,
        ["Keyboard_Application"] = ExtendedPrefix | 0x5D,

        ["Keyboard_PauseBreak"] = PauseSequence
    };

    /// <summary>All known mappings, for diagnostics and tests.</summary>
    public static IReadOnlyDictionary<string, ushort> All => Table;

    /// <summary>
    /// Looks up the scan code for a key id. Unknown ids return <c>false</c> rather than
    /// throwing: a keyboard may legitimately have keys nothing can be typed with, such as media
    /// keys or macro keys.
    /// </summary>
    public static bool TryGet(string keyId, out ushort scanCode)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        return Table.TryGetValue(keyId, out scanCode);
    }

    /// <summary>
    /// The scan code for the Pause key: <c>0x1D</c>, left control's code, behind an <c>E1</c>
    /// prefix that only Pause sends. That is how Windows itself names it — mapping it to a
    /// virtual key answers <c>VK_PAUSE</c> — and it must not be filed under the <c>0x45</c> that
    /// follows in the same sequence, because that is Num Lock's plain code and the two would be
    /// one key.
    /// </summary>
    public const ushort PauseSequence = 0xE11D;
}
