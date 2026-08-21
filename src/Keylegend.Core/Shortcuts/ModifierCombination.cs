using Keylegend.Core.Input;

namespace Keylegend.Core.Shortcuts;

/// <summary>
/// Reads and writes modifier combinations as text, for settings files and the interface.
/// </summary>
/// <remarks>
/// Combinations are stored as <c>Win+Shift</c> rather than as the flags enum's numeric value.
/// A saved file then survives reordering the enum, and someone editing it by hand can see what
/// they are changing. Only the canonical, side-collapsed form is produced — left and right Ctrl
/// mean the same thing to a shortcut, so there is nothing to distinguish.
/// </remarks>
public static class ModifierCombination
{
    /// <summary>The combinations the interface offers, in a sensible reading order.</summary>
    public static IReadOnlyList<ModifierKeys> Known { get; } =
    [
        ModifierKeys.LeftWin,
        ModifierKeys.LeftWin | ModifierKeys.LeftShift,
        ModifierKeys.LeftWin | ModifierKeys.LeftCtrl,
        ModifierKeys.LeftCtrl,
        ModifierKeys.LeftCtrl | ModifierKeys.LeftShift,
        ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt,
        ModifierKeys.LeftAlt,
        ModifierKeys.RightAlt
    ];

    /// <summary>Renders a combination as text, e.g. <c>Ctrl+Shift</c>.</summary>
    public static string Format(ModifierKeys modifiers)
    {
        if (modifiers == ModifierKeys.None)
        {
            return "None";
        }

        // AltGr is its own layer and never combines here: Windows reports it as Ctrl plus right
        // Alt, so writing it out as "Ctrl+AltGr" would suggest a combination that cannot be
        // pressed deliberately.
        if ((modifiers & ModifierKeys.RightAlt) != 0)
        {
            return "AltGr";
        }

        var parts = new List<string>(4);

        if ((modifiers & (ModifierKeys.LeftWin | ModifierKeys.RightWin)) != 0) { parts.Add("Win"); }
        if ((modifiers & (ModifierKeys.LeftCtrl | ModifierKeys.RightCtrl)) != 0) { parts.Add("Ctrl"); }
        if ((modifiers & ModifierKeys.LeftAlt) != 0) { parts.Add("Alt"); }
        if ((modifiers & (ModifierKeys.LeftShift | ModifierKeys.RightShift)) != 0) { parts.Add("Shift"); }

        return parts.Count == 0 ? "None" : string.Join("+", parts);
    }

    /// <summary>
    /// Parses text produced by <see cref="Format"/>. Unknown words are ignored rather than
    /// rejected, so a typo in a hand-edited file costs one modifier instead of the whole set.
    /// </summary>
    public static bool TryParse(string? text, out ModifierKeys modifiers)
    {
        modifiers = ModifierKeys.None;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var part in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "win" or "windows": modifiers |= ModifierKeys.LeftWin; break;
                case "ctrl" or "control" or "strg": modifiers |= ModifierKeys.LeftCtrl; break;
                case "alt": modifiers |= ModifierKeys.LeftAlt; break;
                case "shift" or "umschalt": modifiers |= ModifierKeys.LeftShift; break;
                case "altgr" or "alt gr": modifiers |= ModifierKeys.RightAlt | ModifierKeys.LeftCtrl; break;
                default: break;
            }
        }

        return modifiers != ModifierKeys.None;
    }
}
