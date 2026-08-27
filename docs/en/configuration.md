# Configuration

Settings live in `%APPDATA%\Keylegend\` and are edited through the interface. A complete
default configuration is written on first start.

## Colours

One colour per category:

| Category | Applies to |
|---|---|
| Digit | `1`, `7`, and the number pad while Num Lock is on |
| Lowercase | `a`, `ö` |
| Uppercase | `A`, `Ö` |
| Symbol | `+`, `#`, `€`, `\|`, and the number pad operators |
| Control key | Esc, Tab, Enter, Backspace, modifiers, arrows, navigation cluster, and the number pad while Num Lock is off |
| Function key | F1 to F12 |
| Dead key | `^`, `´`, `` ` `` — keys that need a second keystroke to produce a character |
| Unassigned | keys with no meaning in the current context; dark by default. The number pad's centre key with Num Lock off is the clearest example |

Lock keys have two colours each — one for on, one for off.

## Shortcut sets

A shortcut set maps keys to **function groups** and is looked up by the set of modifiers
currently held. Shipped sets: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`, `Ctrl+Shift`,
`Ctrl+Alt`.

Each group has its own colour, so related commands read as a block — for example editing
(`X`/`C`/`V`/`Z`/`Y`/`A`) in one colour and file operations (`N`/`O`/`S`/`P`/`W`) in another.

Windows shortcuts are fixed system-wide and therefore always accurate. Ctrl shortcuts vary
between programs; the shipped set covers the common Windows conventions.

## Application profiles

A profile describes what the keyboard should show while a particular program is in front.
Around ninety come with the application — programs such as Photoshop, Visual Studio Code or
Excel, and games such as Elden Ring or Counter-Strike 2. They take effect on their own: as soon
as the matching window has the focus the profile applies, and when the focus moves on the
default sets apply again. Where no profile matches, nothing changes.

Recognition is by executable name. Where more than one profile matches, the one naming the
program wins — a game with its own profile therefore keeps it even though the game detection
also fires. Priority only breaks the remaining ties.

A profile is laid over the general set, entry by entry. Photoshop says what `Ctrl+J` means
there; `Ctrl+C` still copies, because a profile naming the Ctrl layer is not claiming that Ctrl
means nothing else. And `Win+E` still opens Explorer, because Windows assigns that combination
system-wide and it holds regardless of what is in front.

### What a profile contains

| Section | Contents |
|---|---|
| Match | Which programs the profile applies to: executable names, whether it covers detected games in general, and the priority |
| Highlights | Keys pinned to a fixed colour regardless of the character they produce — WASD in a game, the tool keys of an image editor |
| Shortcuts | Replacements for individual modifier layers: which key carries which command under `Ctrl`, coloured by function group |

Highlights and shortcuts also carry a label saying what the command does — "Duplicate layer",
"Jump". None of that is visible on the keyboard; the LEDs only show colour. The label appears
in the preview inside the application, and at ninety profiles it is the only way to check
whether an entry is right at all.

### Editing and resetting

The three sections are overridden separately. Edit the highlights of a shipped profile and the
highlights are yours from then on: they are frozen and no longer follow the shipped version.
The match and the shortcuts keep following it and pick up the improvements a new release
brings.

Only the section you changed is saved, stored under the profile's id — never a copy of the
whole profile. That is precisely why resetting exists, and why an update can still improve a
profile you have partly edited.

Resetting works per section accordingly: giving the shortcuts back while keeping your own
highlights is possible. Resetting the whole profile takes back every section, plus a changed
name and a hidden state.

Shipped profiles can be **hidden but not deleted**. They live inside the program file; deleting
one would only last until the next start. A hidden profile is skipped when a profile is
selected, but stays in the list and can be shown again.

### Your own profiles

A profile you create yourself is stored whole in `settings.json`, because there is nothing to
compare it against. It therefore cannot be reset, only deleted. Otherwise it behaves like a
shipped one: the same three sections, the same selection rule.

If a profile should apply to everyone rather than only to you, it belongs in the project as a
file — see [Adding a profile](adding-a-profile.md).

### Settings file format

`settings.json` carries `formatVersion` 3. Older files are migrated on load.

A version 1 file knows neither ids nor where a profile came from, so it cannot say which of its
entries are shipped ones. All of them become user profiles. Nothing is lost, but the shipped
profiles appear alongside them, so there may at first be two entries for the same program; the
surplus one can be deleted or hidden.

A version 2 file lists every colour, the untouched ones included, which pins the palette: an
improved shipped colour reaches nobody who has run the program before. A colour equal to the
palette of that version is therefore read as a default and dropped on migration; anything else is
your choice and is kept.

## Behaviour

| Setting | Meaning |
|---|---|
| Hand the lighting back when idle | Whether it is handed back at all. Switched off, Keylegend keeps the keyboard until you pause or close it — and takes it at startup rather than waiting for a keypress. |
| Idle period | Seconds without keyboard activity before the hand-back. Default 60 — reclaiming it costs one to two seconds, so a short period turns that into a constant interruption. The value is kept while the hand-back is switched off. |
| Brightness | Global factor from 0 to 100 %, applied to every colour as the frame is composed. |
| Use application profiles | Whether profiles are consulted at all. Switched off, the default sets apply everywhere, whatever is in front. |
| Start with Windows | Registers the application in the `Run` key, with the `--minimized` switch. Started that way, Keylegend comes up in the notification area: no window, no balloon. Started by hand it always shows its window. An entry written by an earlier version is brought up to date at the next start. |

## Language

The interface follows the Windows display language and is available in eleven: English, German,
Spanish, French, Italian, Dutch, Polish, Portuguese, Russian, Ukrainian and simplified Chinese.
**Settings → Language** overrides that; switching takes effect immediately, with no restart.

Every language names itself in that list rather than being translated. Translating it would mean
each of the eleven carrying ten names for the others, and somebody whose interface came up in a
language they cannot read would have to find their own in a language they also cannot read.

The choice is stored in `settings.json` under `language` as `Automatic`, `English`, `German`,
`Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian` or
`ChineseSimplified`. An unrecognised value falls back to `Automatic` rather than refusing to
start, which is what a hand-edited file most likely wants anyway.

What is translated is the menus and explanations. Two things are **not**, both deliberately:

- **The key legends** on the pictured keyboard. They come from Razer's drawing and have to match the keyboard in front of you, not the language of the menus — a German ISO board shows
  `strg` and `entf` whether or not the interface is running in English.
- **The modifier names** (Shift, Ctrl, Alt, AltGr, Num Lock …). The same names are produced by
  the shortcut machinery for the layer lists, which sits outside the translation; half a
  translation would read worse than none.

Anything without a translation falls back to English, so an unfinished language file costs the
lines it is missing rather than the whole interface.

## If the lighting is not working

Talking to the Chroma service can fail: the service is stopped, Synapse was closed, another
program holds the session. Keylegend keeps retrying, with a growing pause between attempts, and
says what is wrong while it does:

- the status line at the bottom of the window carries the reason, in amber rather than the usual
  grey
- the notification area says so in its tooltip, so a closed window does not hide it
- one balloon announces it, once per fault rather than once per attempt

All three go away as soon as a frame gets through again. If nothing appears at all and the keyboard
still does not light, the program is not running — look for its icon in the notification area.

## If the wrong keys light up

The keyboard in the window is the keyboard on the desk: both are filled by the same code, so the
window shows what the hardware should look like. Holding the two side by side is the check.

Which cell of the lighting matrix a key belongs to is the one thing neither Synapse nor the drawing
states — it comes from the Chroma protocol's own table. So if a key lights up on the hardware while
a different one is lit in the window, that table is wrong for your model. An issue saying which
keyboard and which key is worth opening.
