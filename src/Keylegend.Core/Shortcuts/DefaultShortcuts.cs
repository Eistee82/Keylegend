using Keylegend.Core.Input;

namespace Keylegend.Core.Shortcuts;

/// <summary>
/// The shortcut sets shipped with the application.
/// </summary>
/// <remarks>
/// <para>
/// Letter and digit shortcuts are listed by the character they use, not by key position. Ctrl+Z
/// means "the key that types z" — on a German keyboard that is a different physical key than on
/// a US one, and listing positions would show undo and redo swapped. Keys that type nothing —
/// Escape, Tab, the arrows — are listed by position, where no such ambiguity exists.
/// </para>
/// <para>
/// Windows shortcuts are assigned system-wide and are therefore always accurate. Ctrl shortcuts
/// belong to whichever program is in front; what is listed here is only what holds across most
/// Windows software, and an application profile can replace the set where it does not.
/// </para>
/// </remarks>
public static class DefaultShortcuts
{
    public static ShortcutCatalogue Create()
    {
        var sets = new Dictionary<ModifierKeys, ShortcutSet>
        {
            // ---------------------------------------------------------------- Windows
            [ModifierKeys.LeftWin] = Set(
                characters: new()
                {
                    // Starting and opening
                    ["e"] = new(FunctionGroup.File, "Open File Explorer"),
                    ["r"] = new(FunctionGroup.File, "Open the Run dialog"),
                    ["s"] = new(FunctionGroup.Search, "Search"),
                    ["q"] = new(FunctionGroup.Search, "Search"),
                    ["t"] = new(FunctionGroup.Window, "Cycle through taskbar apps"),
                    ["1"] = new(FunctionGroup.File, "Open the first taskbar app"),
                    ["2"] = new(FunctionGroup.File, "Open the second taskbar app"),
                    ["3"] = new(FunctionGroup.File, "Open the third taskbar app"),
                    ["4"] = new(FunctionGroup.File, "Open the fourth taskbar app"),
                    ["5"] = new(FunctionGroup.File, "Open the fifth taskbar app"),
                    ["6"] = new(FunctionGroup.File, "Open the sixth taskbar app"),
                    ["7"] = new(FunctionGroup.File, "Open the seventh taskbar app"),
                    ["8"] = new(FunctionGroup.File, "Open the eighth taskbar app"),
                    ["9"] = new(FunctionGroup.File, "Open the ninth taskbar app"),
                    ["0"] = new(FunctionGroup.File, "Open the tenth taskbar app"),

                    // Windows and desktop
                    ["d"] = new(FunctionGroup.Window, "Show the desktop"),
                    ["m"] = new(FunctionGroup.Window, "Minimise all windows"),
                    ["z"] = new(FunctionGroup.Window, "Snap layouts"),
                    [","] = new(FunctionGroup.Window, "Peek at the desktop"),

                    // System and settings
                    ["i"] = new(FunctionGroup.System, "Open Settings"),
                    ["x"] = new(FunctionGroup.System, "Open the power user menu"),
                    ["a"] = new(FunctionGroup.System, "Open quick settings"),
                    ["n"] = new(FunctionGroup.System, "Open notifications"),
                    ["u"] = new(FunctionGroup.System, "Open accessibility settings"),
                    ["l"] = new(FunctionGroup.System, "Lock the computer"),
                    ["b"] = new(FunctionGroup.System, "Focus the notification area"),

                    // Tools and input
                    ["v"] = new(FunctionGroup.Tools, "Clipboard history"),
                    ["."] = new(FunctionGroup.Tools, "Emoji panel"),
                    ["h"] = new(FunctionGroup.Tools, "Start dictation"),
                    ["g"] = new(FunctionGroup.Tools, "Open the Game Bar"),
                    ["p"] = new(FunctionGroup.Tools, "Choose a presentation display"),
                    ["k"] = new(FunctionGroup.Tools, "Connect to a wireless display"),
                    ["c"] = new(FunctionGroup.Tools, "Open Copilot"),
                    ["w"] = new(FunctionGroup.Tools, "Open widgets"),

                    // View
                    ["+"] = new(FunctionGroup.View, "Magnifier: zoom in"),
                    ["-"] = new(FunctionGroup.View, "Magnifier: zoom out")
                },
                keys: new()
                {
                    ["Keyboard_Tab"] = new(FunctionGroup.Window, "Task view"),
                    ["Keyboard_Home"] = new(FunctionGroup.Window, "Minimise everything but this window"),
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Window, "Snap the window left"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Window, "Snap the window right"),
                    ["Keyboard_ArrowUp"] = new(FunctionGroup.Window, "Maximise the window"),
                    ["Keyboard_ArrowDown"] = new(FunctionGroup.Window, "Restore or minimise the window"),
                    ["Keyboard_PauseBreak"] = new(FunctionGroup.System, "Open system properties"),
                    ["Keyboard_NumPlus"] = new(FunctionGroup.View, "Magnifier: zoom in"),
                    ["Keyboard_NumMinus"] = new(FunctionGroup.View, "Magnifier: zoom out"),
                    ["Keyboard_Escape"] = new(FunctionGroup.View, "Close the magnifier")
                }),

            // ---------------------------------------------------------------- Windows + Shift
            [ModifierKeys.LeftWin | ModifierKeys.LeftShift] = Set(
                characters: new()
                {
                    ["s"] = new(FunctionGroup.Tools, "Snip part of the screen"),
                    ["m"] = new(FunctionGroup.Window, "Restore minimised windows")
                },
                keys: new()
                {
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Window, "Move the window to the left monitor"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Window, "Move the window to the right monitor"),
                    ["Keyboard_ArrowUp"] = new(FunctionGroup.Window, "Stretch the window to full height"),
                    ["Keyboard_ArrowDown"] = new(FunctionGroup.Window, "Restore the window height")
                }),

            // ---------------------------------------------------------------- Windows + Ctrl
            [ModifierKeys.LeftWin | ModifierKeys.LeftCtrl] = Set(
                characters: new()
                {
                    ["d"] = new(FunctionGroup.Window, "Add a virtual desktop")
                },
                keys: new()
                {
                    ["Keyboard_F4"] = new(FunctionGroup.Window, "Close the virtual desktop"),
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Navigation, "Previous virtual desktop"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Navigation, "Next virtual desktop"),
                    ["Keyboard_Enter"] = new(FunctionGroup.System, "Turn Narrator on or off")
                }),

            // ---------------------------------------------------------------- Alt
            [ModifierKeys.LeftAlt] = Set(
                characters: new()
                {
                    ["d"] = new(FunctionGroup.Navigation, "Focus the address bar")
                },
                keys: new()
                {
                    ["Keyboard_Tab"] = new(FunctionGroup.Window, "Switch window"),
                    ["Keyboard_Escape"] = new(FunctionGroup.Window, "Cycle through open windows"),
                    ["Keyboard_F4"] = new(FunctionGroup.Window, "Close the window"),
                    ["Keyboard_Space"] = new(FunctionGroup.Window, "Open the window menu"),
                    ["Keyboard_Enter"] = new(FunctionGroup.File, "Show properties"),
                    ["Keyboard_PrintScreen"] = new(FunctionGroup.Tools, "Capture this window"),
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Navigation, "Go back"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Navigation, "Go forward"),
                    ["Keyboard_ArrowUp"] = new(FunctionGroup.Navigation, "Go up one folder"),
                    ["Keyboard_Home"] = new(FunctionGroup.Navigation, "Go to the home page")
                }),

            // ---------------------------------------------------------------- Ctrl
            [ModifierKeys.LeftCtrl] = Set(
                characters: new()
                {
                    // Editing
                    ["x"] = new(FunctionGroup.Edit, "Cut"),
                    ["c"] = new(FunctionGroup.Edit, "Copy"),
                    ["v"] = new(FunctionGroup.Edit, "Paste"),
                    ["z"] = new(FunctionGroup.Edit, "Undo"),
                    ["y"] = new(FunctionGroup.Edit, "Redo"),
                    ["a"] = new(FunctionGroup.Edit, "Select all"),

                    // Formatting - near-universal wherever text can be styled
                    ["b"] = new(FunctionGroup.Edit, "Bold"),
                    ["i"] = new(FunctionGroup.Edit, "Italic"),
                    ["u"] = new(FunctionGroup.Edit, "Underline"),

                    // File
                    ["n"] = new(FunctionGroup.File, "New"),
                    ["o"] = new(FunctionGroup.File, "Open"),
                    ["s"] = new(FunctionGroup.File, "Save"),
                    ["p"] = new(FunctionGroup.File, "Print"),
                    ["w"] = new(FunctionGroup.File, "Close the tab or document"),

                    // Search
                    ["f"] = new(FunctionGroup.Search, "Find"),
                    ["h"] = new(FunctionGroup.Search, "Find and replace"),
                    ["g"] = new(FunctionGroup.Search, "Find next"),
                    ["e"] = new(FunctionGroup.Search, "Focus the search box"),

                    // View and navigation within a program
                    ["t"] = new(FunctionGroup.View, "New tab"),
                    ["r"] = new(FunctionGroup.View, "Reload"),
                    ["d"] = new(FunctionGroup.View, "Bookmark or duplicate"),
                    ["l"] = new(FunctionGroup.Navigation, "Focus the address bar"),
                    ["k"] = new(FunctionGroup.Tools, "Insert a link or open search"),
                    ["+"] = new(FunctionGroup.View, "Zoom in"),
                    ["-"] = new(FunctionGroup.View, "Zoom out"),
                    ["0"] = new(FunctionGroup.View, "Reset the zoom")
                },
                keys: new()
                {
                    ["Keyboard_Escape"] = new(FunctionGroup.System, "Open the Start menu"),
                    ["Keyboard_Tab"] = new(FunctionGroup.View, "Next tab"),
                    ["Keyboard_Home"] = new(FunctionGroup.Navigation, "Go to the start of the document"),
                    ["Keyboard_End"] = new(FunctionGroup.Navigation, "Go to the end of the document"),
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Navigation, "Move one word left"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Navigation, "Move one word right"),
                    ["Keyboard_ArrowUp"] = new(FunctionGroup.Navigation, "Move one paragraph up"),
                    ["Keyboard_ArrowDown"] = new(FunctionGroup.Navigation, "Move one paragraph down"),
                    ["Keyboard_Backspace"] = new(FunctionGroup.Edit, "Delete the word before the cursor"),
                    ["Keyboard_Delete"] = new(FunctionGroup.Edit, "Delete the word after the cursor"),
                    ["Keyboard_NumPlus"] = new(FunctionGroup.View, "Zoom in"),
                    ["Keyboard_NumMinus"] = new(FunctionGroup.View, "Zoom out")
                }),

            // ---------------------------------------------------------------- Ctrl + Shift
            [ModifierKeys.LeftCtrl | ModifierKeys.LeftShift] = Set(
                characters: new()
                {
                    ["n"] = new(FunctionGroup.File, "New folder or private window"),
                    ["s"] = new(FunctionGroup.File, "Save as"),
                    ["v"] = new(FunctionGroup.Edit, "Paste without formatting"),
                    ["z"] = new(FunctionGroup.Edit, "Redo"),   // where Ctrl+Y does not apply
                    ["t"] = new(FunctionGroup.View, "Reopen the closed tab")
                },
                keys: new()
                {
                    ["Keyboard_Escape"] = new(FunctionGroup.System, "Open Task Manager"),
                    ["Keyboard_Delete"] = new(FunctionGroup.System, "Clear browsing data"),
                    ["Keyboard_Tab"] = new(FunctionGroup.View, "Previous tab"),

                    // Selection. These come from Windows' own text editing rather than from any
                    // one program, so they hold almost everywhere a caret does - the most
                    // dependable entries on this layer.
                    ["Keyboard_ArrowLeft"] = new(FunctionGroup.Navigation, "Select the word to the left"),
                    ["Keyboard_ArrowRight"] = new(FunctionGroup.Navigation, "Select the word to the right"),
                    ["Keyboard_ArrowUp"] = new(FunctionGroup.Navigation, "Select to the paragraph above"),
                    ["Keyboard_ArrowDown"] = new(FunctionGroup.Navigation, "Select to the paragraph below"),
                    ["Keyboard_Home"] = new(FunctionGroup.Navigation, "Select to the start of the document"),
                    ["Keyboard_End"] = new(FunctionGroup.Navigation, "Select to the end of the document")
                }),

            // ---------------------------------------------------------------- Ctrl + Alt
            // Kept deliberately sparse: on German and most European layouts this combination is
            // AltGr, so nearly every letter here would collide with a character assignment.
            [ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt] = Set(
                characters: [],
                keys: new()
                {
                    ["Keyboard_Delete"] = new(FunctionGroup.System, "Open the security options screen"),
                    ["Keyboard_Tab"] = new(FunctionGroup.Window, "Task view, stays open")
                })
        };

        return new ShortcutCatalogue(sets);
    }

    private static ShortcutSet Set(
        Dictionary<string, Shortcut> characters,
        Dictionary<string, Shortcut> keys)
        => new(
            new Dictionary<string, Shortcut>(characters, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Shortcut>(keys, StringComparer.Ordinal));
}
