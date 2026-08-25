namespace Keylegend.Core.Devices;

/// <summary>
/// Where each key sits in the lighting matrix. This is a property of the lighting protocol, not
/// of any one keyboard: the same table holds for every model the vendor makes.
/// </summary>
/// <remarks>
/// <para>
/// The vendor's SDK publishes these positions as a key enumeration whose values encode the cell
/// directly — <c>(row &lt;&lt; 8) | column</c>, so <c>0x0302</c> is row 3, column 2. The table
/// below is that enumeration, transcribed.
/// </para>
/// <para>
/// Two properties of the table surprise at first sight and are both deliberate. Column 0 belongs
/// to the macro keys that models like the BlackWidow carry down their left edge; on a keyboard
/// without them it simply stays dark, which is why the ordinary keys all start at column 1. And
/// the Japanese and Korean keys share cells with each other, because no keyboard is both — the
/// layout decides which of the two a given cell means.
/// </para>
/// <para>
/// It is worth being precise about what this replaces. A device profile used to carry a
/// <c>row</c> and <c>column</c> for every key, derived from the keyboard's firmware. But firmware
/// describes how the board is <em>wired</em>, and that is not the matrix a custom frame is
/// addressed by — the two agree on some models and not on others. Measuring at the device
/// settled it: the measured mapping of the DeathStalker V2 matches this table on all 105 keys,
/// including the three that no derived rule got right. Positions therefore come from here, and a
/// profile only has to say which keys a device <em>has</em>.
/// </para>
/// </remarks>
public static class StandardKeyMatrix
{
    /// <summary>Rows in the lighting matrix.</summary>
    public const int Rows = 6;

    /// <summary>Columns in the lighting matrix. Column 0 stays unused.</summary>
    public const int Columns = 22;

    private static readonly Dictionary<string, (int Row, int Column)> Cells = new(StringComparer.Ordinal)
    {
        // Reihe 0
        ["Keyboard_Escape"] = (0, 1),               // RZKEY_ESC
        ["Keyboard_F1"] = (0, 3),                   // RZKEY_F1
        ["Keyboard_F2"] = (0, 4),                   // RZKEY_F2
        ["Keyboard_F3"] = (0, 5),                   // RZKEY_F3
        ["Keyboard_F4"] = (0, 6),                   // RZKEY_F4
        ["Keyboard_F5"] = (0, 7),                   // RZKEY_F5
        ["Keyboard_F6"] = (0, 8),                   // RZKEY_F6
        ["Keyboard_F7"] = (0, 9),                   // RZKEY_F7
        ["Keyboard_F8"] = (0, 10),                  // RZKEY_F8
        ["Keyboard_F9"] = (0, 11),                  // RZKEY_F9
        ["Keyboard_F10"] = (0, 12),                 // RZKEY_F10
        ["Keyboard_F11"] = (0, 13),                 // RZKEY_F11
        ["Keyboard_F12"] = (0, 14),                 // RZKEY_F12
        ["Keyboard_PrintScreen"] = (0, 15),         // RZKEY_PRINTSCREEN
        ["Keyboard_ScrollLock"] = (0, 16),          // RZKEY_SCROLL
        ["Keyboard_PauseBreak"] = (0, 17),          // RZKEY_PAUSE
        ["Keyboard_JpYen"] = (0, 21),               // RZKEY_JPN_1
        ["Keyboard_Kor1"] = (0, 21),                // RZKEY_KOR_1

        // Reihe 1
        ["Keyboard_Macro1"] = (1, 0),               // RZKEY_MACRO1
        ["Keyboard_GraveAccentAndTilde"] = (1, 1),  // RZKEY_OEM_1
        ["Keyboard_1"] = (1, 2),                    // RZKEY_1
        ["Keyboard_2"] = (1, 3),                    // RZKEY_2
        ["Keyboard_3"] = (1, 4),                    // RZKEY_3
        ["Keyboard_4"] = (1, 5),                    // RZKEY_4
        ["Keyboard_5"] = (1, 6),                    // RZKEY_5
        ["Keyboard_6"] = (1, 7),                    // RZKEY_6
        ["Keyboard_7"] = (1, 8),                    // RZKEY_7
        ["Keyboard_8"] = (1, 9),                    // RZKEY_8
        ["Keyboard_9"] = (1, 10),                   // RZKEY_9
        ["Keyboard_0"] = (1, 11),                   // RZKEY_0
        ["Keyboard_MinusAndUnderscore"] = (1, 12),  // RZKEY_OEM_2
        ["Keyboard_EqualsAndPlus"] = (1, 13),       // RZKEY_OEM_3
        ["Keyboard_Backspace"] = (1, 14),           // RZKEY_BACKSPACE
        ["Keyboard_Insert"] = (1, 15),              // RZKEY_INSERT
        ["Keyboard_Home"] = (1, 16),                // RZKEY_HOME
        ["Keyboard_PageUp"] = (1, 17),              // RZKEY_PAGEUP
        ["Keyboard_NumLock"] = (1, 18),             // RZKEY_NUMLOCK
        ["Keyboard_NumSlash"] = (1, 19),            // RZKEY_NUMPAD_DIVIDE
        ["Keyboard_NumAsterisk"] = (1, 20),         // RZKEY_NUMPAD_MULTIPLY
        ["Keyboard_NumMinus"] = (1, 21),            // RZKEY_NUMPAD_SUBTRACT

        // Reihe 2
        ["Keyboard_Macro2"] = (2, 0),               // RZKEY_MACRO2
        ["Keyboard_Tab"] = (2, 1),                  // RZKEY_TAB
        ["Keyboard_Q"] = (2, 2),                    // RZKEY_Q
        ["Keyboard_W"] = (2, 3),                    // RZKEY_W
        ["Keyboard_E"] = (2, 4),                    // RZKEY_E
        ["Keyboard_R"] = (2, 5),                    // RZKEY_R
        ["Keyboard_T"] = (2, 6),                    // RZKEY_T
        ["Keyboard_Y"] = (2, 7),                    // RZKEY_Y
        ["Keyboard_U"] = (2, 8),                    // RZKEY_U
        ["Keyboard_I"] = (2, 9),                    // RZKEY_I
        ["Keyboard_O"] = (2, 10),                   // RZKEY_O
        ["Keyboard_P"] = (2, 11),                   // RZKEY_P
        ["Keyboard_BracketLeft"] = (2, 12),         // RZKEY_OEM_4
        ["Keyboard_BracketRight"] = (2, 13),        // RZKEY_OEM_5
        ["Keyboard_Backslash"] = (2, 14),           // RZKEY_OEM_6
        ["Keyboard_Delete"] = (2, 15),              // RZKEY_DELETE
        ["Keyboard_End"] = (2, 16),                 // RZKEY_END
        ["Keyboard_PageDown"] = (2, 17),            // RZKEY_PAGEDOWN
        ["Keyboard_Num7"] = (2, 18),                // RZKEY_NUMPAD7
        ["Keyboard_Num8"] = (2, 19),                // RZKEY_NUMPAD8
        ["Keyboard_Num9"] = (2, 20),                // RZKEY_NUMPAD9
        ["Keyboard_NumPlus"] = (2, 21),             // RZKEY_NUMPAD_ADD

        // Reihe 3
        ["Keyboard_Macro3"] = (3, 0),               // RZKEY_MACRO3
        ["Keyboard_CapsLock"] = (3, 1),             // RZKEY_CAPSLOCK
        ["Keyboard_A"] = (3, 2),                    // RZKEY_A
        ["Keyboard_S"] = (3, 3),                    // RZKEY_S
        ["Keyboard_D"] = (3, 4),                    // RZKEY_D
        ["Keyboard_F"] = (3, 5),                    // RZKEY_F
        ["Keyboard_G"] = (3, 6),                    // RZKEY_G
        ["Keyboard_H"] = (3, 7),                    // RZKEY_H
        ["Keyboard_J"] = (3, 8),                    // RZKEY_J
        ["Keyboard_K"] = (3, 9),                    // RZKEY_K
        ["Keyboard_L"] = (3, 10),                   // RZKEY_L
        ["Keyboard_SemicolonAndColon"] = (3, 11),   // RZKEY_OEM_7
        ["Keyboard_ApostropheAndDoubleQuote"] = (3, 12),// RZKEY_OEM_8
        ["Keyboard_Kor2"] = (3, 13),                // RZKEY_KOR_2
        ["Keyboard_NonUsTilde"] = (3, 13),          // RZKEY_EUR_1
        ["Keyboard_Enter"] = (3, 14),               // RZKEY_ENTER
        ["Keyboard_Num4"] = (3, 18),                // RZKEY_NUMPAD4
        ["Keyboard_Num5"] = (3, 19),                // RZKEY_NUMPAD5
        ["Keyboard_Num6"] = (3, 20),                // RZKEY_NUMPAD6

        // Reihe 4
        ["Keyboard_Macro4"] = (4, 0),               // RZKEY_MACRO4
        ["Keyboard_LeftShift"] = (4, 1),            // RZKEY_LSHIFT
        ["Keyboard_Kor3"] = (4, 2),                 // RZKEY_KOR_3
        ["Keyboard_NonUsBackslash"] = (4, 2),       // RZKEY_EUR_2
        ["Keyboard_Z"] = (4, 3),                    // RZKEY_Z
        ["Keyboard_X"] = (4, 4),                    // RZKEY_X
        ["Keyboard_C"] = (4, 5),                    // RZKEY_C
        ["Keyboard_V"] = (4, 6),                    // RZKEY_V
        ["Keyboard_B"] = (4, 7),                    // RZKEY_B
        ["Keyboard_N"] = (4, 8),                    // RZKEY_N
        ["Keyboard_M"] = (4, 9),                    // RZKEY_M
        ["Keyboard_CommaAndLessThan"] = (4, 10),    // RZKEY_OEM_9
        ["Keyboard_PeriodAndBiggerThan"] = (4, 11), // RZKEY_OEM_10
        ["Keyboard_SlashAndQuestionMark"] = (4, 12),// RZKEY_OEM_11
        ["Keyboard_JpRo"] = (4, 13),                // RZKEY_JPN_2
        ["Keyboard_Kor4"] = (4, 13),                // RZKEY_KOR_4
        ["Keyboard_RightShift"] = (4, 14),          // RZKEY_RSHIFT
        ["Keyboard_ArrowUp"] = (4, 16),             // RZKEY_UP
        ["Keyboard_Num1"] = (4, 18),                // RZKEY_NUMPAD1
        ["Keyboard_Num2"] = (4, 19),                // RZKEY_NUMPAD2
        ["Keyboard_Num3"] = (4, 20),                // RZKEY_NUMPAD3
        ["Keyboard_NumEnter"] = (4, 21),            // RZKEY_NUMPAD_ENTER

        // Reihe 5
        ["Keyboard_Macro5"] = (5, 0),               // RZKEY_MACRO5
        ["Keyboard_LeftCtrl"] = (5, 1),             // RZKEY_LCTRL
        ["Keyboard_LeftGui"] = (5, 2),              // RZKEY_LWIN
        ["Keyboard_LeftAlt"] = (5, 3),              // RZKEY_LALT
        ["Keyboard_JpMuhenkan"] = (5, 4),           // RZKEY_JPN_3
        ["Keyboard_Kor5"] = (5, 4),                 // RZKEY_KOR_5
        ["Keyboard_Space"] = (5, 7),                // RZKEY_SPACE
        ["Keyboard_JpHenkan"] = (5, 9),             // RZKEY_JPN_4
        ["Keyboard_Kor6"] = (5, 9),                 // RZKEY_KOR_6
        ["Keyboard_JpKana"] = (5, 10),              // RZKEY_JPN_5
        ["Keyboard_Kor7"] = (5, 10),                // RZKEY_KOR_7
        ["Keyboard_RightAlt"] = (5, 11),            // RZKEY_RALT
        ["Keyboard_Function"] = (5, 12),            // RZKEY_FN
        // A standard keyboard carries the right Windows key here; these keyboards carry fn. The
        // protocol has no code for a right Windows key at all, which is the giveaway. Mapping it
        // to the same cell lets a layout drawn for ordinary hardware light the right key.
        ["Keyboard_RightGui"] = (5, 12),            // RZKEY_FN
        ["Keyboard_Application"] = (5, 13),         // RZKEY_RMENU
        ["Keyboard_RightCtrl"] = (5, 14),           // RZKEY_RCTRL
        ["Keyboard_ArrowLeft"] = (5, 15),           // RZKEY_LEFT
        ["Keyboard_ArrowDown"] = (5, 16),           // RZKEY_DOWN
        ["Keyboard_ArrowRight"] = (5, 17),          // RZKEY_RIGHT
        ["Keyboard_Num0"] = (5, 19),                // RZKEY_NUMPAD0
        ["Keyboard_NumPeriodAndDelete"] = (5, 20),  // RZKEY_NUMPAD_DECIMAL
    };

    /// <summary>The cell a key occupies, or <c>null</c> for a key the protocol does not place.</summary>
    public static (int Row, int Column)? Cell(string keyId)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        return Cells.TryGetValue(keyId, out var cell) ? cell : null;
    }

    /// <summary>Every key the protocol places, for validation and for building profiles.</summary>
    public static IReadOnlyDictionary<string, (int Row, int Column)> All => Cells;

    /// <summary>
    /// Every key id the protocol knows, which is every key that can be lit on any Razer keyboard.
    /// </summary>
    /// <remarks>
    /// This is the vocabulary of key ids for the whole program now that no device profile is
    /// shipped: it is what a drawn key name is resolved against, and what an application profile's
    /// key ids are checked against. Being the protocol's own table rather than the union of some
    /// set of files, it cannot go out of date as models appear.
    /// </remarks>
    public static IReadOnlyCollection<string> Ids => Cells.Keys;
}
