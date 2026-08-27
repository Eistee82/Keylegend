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
                    ["o"] = new(FunctionGroup.System, "Lock the device orientation"),

                    // Tools and input
                    ["v"] = new(FunctionGroup.Tools, "Clipboard history"),
                    ["f"] = new(FunctionGroup.Tools, "Open the Feedback Hub"),
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
                keys: WithTaskbarPositions(
                    new()
                    {
                        ["Keyboard_Tab"] = new(FunctionGroup.Window, "Task view"),
                        ["Keyboard_Home"] = new(FunctionGroup.Window, "Minimise everything but this window"),
                        ["Keyboard_ArrowLeft"] = new(FunctionGroup.Window, "Snap the window left"),
                        ["Keyboard_ArrowRight"] = new(FunctionGroup.Window, "Snap the window right"),
                        ["Keyboard_ArrowUp"] = new(FunctionGroup.Window, "Maximise the window"),
                        ["Keyboard_ArrowDown"] = new(FunctionGroup.Window, "Restore or minimise the window"),
                        ["Keyboard_PauseBreak"] = new(FunctionGroup.System, "Open system properties"),
                        ["Keyboard_PrintScreen"] = new(FunctionGroup.Tools, "Save a full-screen screenshot"),
                        ["Keyboard_NumPlus"] = new(FunctionGroup.View, "Magnifier: zoom in"),
                        ["Keyboard_NumMinus"] = new(FunctionGroup.View, "Magnifier: zoom out"),
                        ["Keyboard_Escape"] = new(FunctionGroup.View, "Close the magnifier")
                    },
                    FunctionGroup.File,
                    "Open the {0} taskbar app")),

            // ---------------------------------------------------------------- Windows + Shift
            [ModifierKeys.LeftWin | ModifierKeys.LeftShift] = Set(
                characters: new()
                {
                    ["s"] = new(FunctionGroup.Tools, "Snip part of the screen"),
                    ["r"] = new(FunctionGroup.Tools, "Record part of the screen"),
                    ["t"] = new(FunctionGroup.Tools, "Copy text out of an image"),
                    ["m"] = new(FunctionGroup.Window, "Restore minimised windows"),
                    ["a"] = new(FunctionGroup.System, "Focus the Windows tip"),
                    ["v"] = new(FunctionGroup.System, "Cycle through notifications")
                },
                keys: WithTaskbarPositions(
                    new()
                    {
                        ["Keyboard_Space"] = new(FunctionGroup.System, "Previous input language"),
                        ["Keyboard_Enter"] = new(FunctionGroup.Window, "Make a UWP app full screen"),
                        ["Keyboard_ArrowLeft"] = new(FunctionGroup.Window, "Move the window to the left monitor"),
                        ["Keyboard_ArrowRight"] = new(FunctionGroup.Window, "Move the window to the right monitor"),
                        ["Keyboard_ArrowUp"] = new(FunctionGroup.Window, "Stretch the window to full height"),
                        ["Keyboard_ArrowDown"] = new(FunctionGroup.Window, "Restore the window height")
                    },
                    FunctionGroup.File,
                    "Start a new instance of the {0} taskbar app")),

            // ---------------------------------------------------------------- Windows + Ctrl
            [ModifierKeys.LeftWin | ModifierKeys.LeftCtrl] = Set(
                characters: new()
                {
                    ["d"] = new(FunctionGroup.Window, "Add a virtual desktop"),
                    ["c"] = new(FunctionGroup.View, "Turn colour filters on or off"),
                    ["f"] = new(FunctionGroup.Search, "Search for computers on the network"),
                    ["q"] = new(FunctionGroup.Tools, "Open Quick Assist"),
                    ["o"] = new(FunctionGroup.System, "Show or hide the on-screen keyboard"),
                    ["v"] = new(FunctionGroup.System, "Open the sound output settings")
                },
                keys: WithTaskbarPositions(
                    new()
                    {
                        ["Keyboard_Space"] = new(FunctionGroup.System, "Switch back to the previous input method"),
                        ["Keyboard_F4"] = new(FunctionGroup.Window, "Close the virtual desktop"),
                        ["Keyboard_ArrowLeft"] = new(FunctionGroup.Navigation, "Previous virtual desktop"),
                        ["Keyboard_ArrowRight"] = new(FunctionGroup.Navigation, "Next virtual desktop"),
                        ["Keyboard_Enter"] = new(FunctionGroup.System, "Turn Narrator on or off")
                    },
                    FunctionGroup.Window,
                    "Switch to the last window of the {0} taskbar app")),

            // ---------------------------------------------------------------- Windows + Alt
            [ModifierKeys.LeftWin | ModifierKeys.LeftAlt] = Set(
                characters: new()
                {
                    ["b"] = new(FunctionGroup.View, "Turn HDR on or off"),
                    ["d"] = new(FunctionGroup.System, "Show or hide the date and time"),
                    ["h"] = new(FunctionGroup.Tools, "Move focus to the keyboard while dictating"),
                    ["k"] = new(FunctionGroup.Tools, "Mute or unmute the microphone"),

                    // Game Bar, which registers these system-wide rather than Windows itself.
                    ["r"] = new(FunctionGroup.Tools, "Start or stop recording"),
                    ["g"] = new(FunctionGroup.Tools, "Record the last thirty seconds")
                },
                keys: WithTaskbarPositions(
                    new()
                    {
                        ["Keyboard_Enter"] = new(FunctionGroup.System, "Open taskbar settings"),
                        ["Keyboard_ArrowUp"] = new(FunctionGroup.Window, "Snap the window to the top half"),
                        ["Keyboard_ArrowDown"] = new(FunctionGroup.Window, "Snap the window to the bottom half")
                    },
                    FunctionGroup.File,
                    "Open the jump list of the {0} taskbar app")),

            // ------------------------------------------------------- Windows + Ctrl + Shift
            [ModifierKeys.LeftWin | ModifierKeys.LeftCtrl | ModifierKeys.LeftShift] = Set(
                characters: new()
                {
                    ["b"] = new(FunctionGroup.System, "Restart the graphics driver")
                },
                keys: WithTaskbarPositions(
                    new(),
                    FunctionGroup.System,
                    "Open the {0} taskbar app as administrator")),

            // ------------------------------------------- Windows + Ctrl + Alt + Shift (Office)
            // Not Windows' own: Microsoft 365 registers these system-wide when it is installed,
            // which is why they are absent from a machine without it. They are listed anyway —
            // a layer that lights nothing is the correct picture on such a machine, and the
            // application profile mechanism cannot help here because these fire from anywhere.
            [ModifierKeys.LeftWin | ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt | ModifierKeys.LeftShift] = Set(
                characters: new()
                {
                    ["w"] = new(FunctionGroup.File, "Open Word"),
                    ["x"] = new(FunctionGroup.File, "Open Excel"),
                    ["p"] = new(FunctionGroup.File, "Open PowerPoint"),
                    ["n"] = new(FunctionGroup.File, "Open OneNote"),
                    ["o"] = new(FunctionGroup.File, "Open Outlook"),
                    ["t"] = new(FunctionGroup.File, "Open Teams"),
                    ["d"] = new(FunctionGroup.File, "Open OneDrive"),
                    ["l"] = new(FunctionGroup.Tools, "Open LinkedIn in the browser"),
                    ["y"] = new(FunctionGroup.Tools, "Open Viva Engage in the browser")
                },
                keys: new()),

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

    /// <summary>The top-row digits, in taskbar order: position ten is the <c>0</c> key.</summary>
    private static readonly string[] TaskbarKeys =
    [
        "Keyboard_1", "Keyboard_2", "Keyboard_3", "Keyboard_4", "Keyboard_5",
        "Keyboard_6", "Keyboard_7", "Keyboard_8", "Keyboard_9", "Keyboard_0"
    ];

    private static readonly string[] Ordinals =
    [
        "first", "second", "third", "fourth", "fifth",
        "sixth", "seventh", "eighth", "ninth", "tenth"
    ];

    /// <summary>
    /// Adds the ten taskbar-position commands to a layer, addressed by key position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the one deliberate exception to listing digits by the character they type, and
    /// the reason is Windows itself: the commands are bound to the virtual keys VK_1 to VK_0,
    /// not to the character produced. Two things follow, and listing by character got both
    /// wrong.
    /// </para>
    /// <para>
    /// On a French layout the top-row key types <c>&amp;</c> unmodified and still opens the first
    /// taskbar app, so a lookup for <c>"1"</c> would never find it. And the num pad's <c>1</c>
    /// types the same character as the top row while opening nothing at all, so with Num Lock on
    /// a lookup by character lit a key that has no command. Position is unambiguous on every
    /// layout and avoids both.
    /// </para>
    /// </remarks>
    private static Dictionary<string, Shortcut> WithTaskbarPositions(
        Dictionary<string, Shortcut> keys,
        FunctionGroup group,
        string labelFormat)
    {
        for (var position = 0; position < TaskbarKeys.Length; position++)
        {
            keys[TaskbarKeys[position]] = new(
                group,
                string.Format(labelFormat, Ordinals[position]));
        }

        return keys;
    }
}
