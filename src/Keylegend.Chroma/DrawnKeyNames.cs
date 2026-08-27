namespace Keylegend.Chroma;

/// <summary>
/// Translates the key names in the vendor's drawings into the ids used here.
/// </summary>
/// <remarks>
/// <para>
/// The drawings name every key — <c>data-assumed-key-name</c> — and both naming schemes follow the
/// US layout, so the two lists line up one to one. What they do not share is spelling:
/// <c>Caps</c> against <c>Keyboard_CapsLock</c>, <c>NumPadDot</c> against
/// <c>Keyboard_NumPeriodAndDelete</c>, <c>Window</c> against <c>Keyboard_LeftGui</c>.
/// </para>
/// <para>
/// Three names mean different keys on different layouts, which is why each one maps to a list
/// rather than a single id and the first that the layout actually has wins. <c>Backslash</c> is
/// the key after the left Shift on ISO and the one above Enter on ANSI; <c>Extra1</c> is the extra
/// key beside Enter that ANSI does not have at all. Both are named in the drawing, and taking
/// the name is the point: guessing either from where it sits in the picture is exactly where
/// matching by position goes wrong.
/// </para>
/// <para>
/// One entry is not a translation but a correction: the vendor calls the key right of the right
/// Alt <c>Function</c>, and that is what it is on these keyboards. Ordinary layouts draw a second
/// Windows key there, which is why it maps onto <c>Keyboard_RightGui</c>.
/// </para>
/// </remarks>
public static class DrawnKeyNames
{
    private static readonly Dictionary<string, string[]> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Esc"] = ["Keyboard_Escape"],
        ["PrintScreen"] = ["Keyboard_PrintScreen"],
        ["ScrollLock"] = ["Keyboard_ScrollLock"],
        ["PauseBreak"] = ["Keyboard_PauseBreak"],

        ["Tilde"] = ["Keyboard_GraveAccentAndTilde"],
        ["Dash"] = ["Keyboard_MinusAndUnderscore"],
        ["Equal"] = ["Keyboard_EqualsAndPlus"],
        ["Backspace"] = ["Keyboard_Backspace"],

        ["Tab"] = ["Keyboard_Tab"],
        ["StartSquareBracket"] = ["Keyboard_BracketLeft"],
        ["EndSquareBracket"] = ["Keyboard_BracketRight"],
        ["Enter"] = ["Keyboard_Enter"],

        ["Caps"] = ["Keyboard_CapsLock"],
        ["SemiColon"] = ["Keyboard_SemicolonAndColon"],
        ["Apostrophe"] = ["Keyboard_ApostropheAndDoubleQuote"],

        // Layout-dependent, so ordered by which layout has which.
        ["Extra1"] = ["Keyboard_NonUsTilde"],
        ["Backslash"] = ["Keyboard_NonUsBackslash", "Keyboard_Backslash"],

        ["LeftShift"] = ["Keyboard_LeftShift"],
        ["RightShift"] = ["Keyboard_RightShift"],
        ["Comma"] = ["Keyboard_CommaAndLessThan"],
        ["Dot"] = ["Keyboard_PeriodAndBiggerThan"],
        ["ForwardSlash"] = ["Keyboard_SlashAndQuestionMark"],

        ["LeftCtrl"] = ["Keyboard_LeftCtrl"],
        ["RightCtrl"] = ["Keyboard_RightCtrl"],
        ["LeftAlt"] = ["Keyboard_LeftAlt"],
        ["RightAlt"] = ["Keyboard_RightAlt"],
        ["Window"] = ["Keyboard_LeftGui"],
        ["Function"] = ["Keyboard_RightGui"],
        ["Menu"] = ["Keyboard_Application"],
        ["Space"] = ["Keyboard_Space"],

        ["Insert"] = ["Keyboard_Insert"],
        ["Home"] = ["Keyboard_Home"],
        ["PageUp"] = ["Keyboard_PageUp"],
        ["Delete"] = ["Keyboard_Delete"],
        ["End"] = ["Keyboard_End"],
        ["PageDown"] = ["Keyboard_PageDown"],

        ["UpArrow"] = ["Keyboard_ArrowUp"],
        ["DownArrow"] = ["Keyboard_ArrowDown"],
        ["LeftArrow"] = ["Keyboard_ArrowLeft"],
        ["RightArrow"] = ["Keyboard_ArrowRight"],

        ["NumPad"] = ["Keyboard_NumLock"],
        ["NumPadForwardSlash"] = ["Keyboard_NumSlash"],
        ["NumPadAsterisk"] = ["Keyboard_NumAsterisk"],
        ["NumPadMinus"] = ["Keyboard_NumMinus"],
        ["NumPadPlus"] = ["Keyboard_NumPlus"],
        ["NumPadEnter"] = ["Keyboard_NumEnter"],
        ["NumPadDot"] = ["Keyboard_NumPeriodAndDelete"],

        // The num pad's digits shorten to Num here, so they are not the plain case below.
        ["NumPad0"] = ["Keyboard_Num0"],
        ["NumPad1"] = ["Keyboard_Num1"],
        ["NumPad2"] = ["Keyboard_Num2"],
        ["NumPad3"] = ["Keyboard_Num3"],
        ["NumPad4"] = ["Keyboard_Num4"],
        ["NumPad5"] = ["Keyboard_Num5"],
        ["NumPad6"] = ["Keyboard_Num6"],
        ["NumPad7"] = ["Keyboard_Num7"],
        ["NumPad8"] = ["Keyboard_Num8"],
        ["NumPad9"] = ["Keyboard_Num9"],

        // The Japanese keys, named the way the vendor's JIS drawings name them.
        ["Extra2"] = ["Keyboard_JpYen"],
        ["Extra3"] = ["Keyboard_JpRo", "Keyboard_AbntC1"],
        ["Extra4"] = ["Keyboard_JpMuhenkan"],
        ["Extra5"] = ["Keyboard_JpHenkan"],
        ["Extra6"] = ["Keyboard_JpKana"],
    };

    /// <summary>
    /// Every drawn name the table translates. For checking the table against the protocol's own
    /// ids: a name that resolves to an id nothing can address produces a key that is drawn and
    /// never lights, on the one keyboard that has it.
    /// </summary>
    public static IReadOnlyCollection<string> Translated => Names.Keys;

    /// <summary>
    /// The id this drawn key stands for, out of the ids a layout actually has, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Letters, digits and the function keys need no table: the drawing writes <c>Q</c>, <c>7</c>
    /// and <c>F11</c>, which are the ids with the prefix put back on. Only the names that differ
    /// are listed above, which keeps the table to what it is really for.
    /// </remarks>
    public static string? Resolve(string? drawn, IReadOnlySet<string> available)
    {
        ArgumentNullException.ThrowIfNull(available);

        if (string.IsNullOrWhiteSpace(drawn))
        {
            return null;
        }

        if (Names.TryGetValue(drawn, out var candidates))
        {
            foreach (var candidate in candidates)
            {
                if (available.Contains(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        // Letters, digits, F-keys and the num pad's digits: the id is the name with the prefix.
        var direct = "Keyboard_" + drawn;

        return available.Contains(direct) ? direct : null;
    }
}
