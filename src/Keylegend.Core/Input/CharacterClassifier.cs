using System.Globalization;

namespace Keylegend.Core.Input;

/// <summary>
/// Decides which category a produced character belongs to. Deliberately based on Unicode
/// properties rather than a character list, so that every keyboard layout is covered.
/// </summary>
public static class CharacterClassifier
{
    /// <param name="character">What the key produces, or null/empty if nothing.</param>
    /// <param name="isDeadKey">Whether the key only modifies the following keystroke.</param>
    public static KeyCategory Classify(string? character, bool isDeadKey = false)
    {
        if (isDeadKey)
        {
            return KeyCategory.DeadKey;
        }

        if (string.IsNullOrEmpty(character))
        {
            return KeyCategory.Unassigned;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory(character, 0);

        return category switch
        {
            // Control characters are what Windows reports for Escape, Tab, Enter and friends.
            UnicodeCategory.Control => KeyCategory.Control,
            UnicodeCategory.DecimalDigitNumber => KeyCategory.Digit,
            UnicodeCategory.LowercaseLetter => KeyCategory.Lowercase,
            UnicodeCategory.UppercaseLetter or UnicodeCategory.TitlecaseLetter => KeyCategory.Uppercase,

            // Everything else - punctuation, currency, signs, space, and letters without case -
            // reads as a symbol rather than as a wrongly guessed case.
            _ => KeyCategory.Symbol
        };
    }
}
