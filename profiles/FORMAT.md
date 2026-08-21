# Application profile format

A profile says what the keyboard should show while a particular program is in front. One JSON
file per profile; the files in this directory are compiled into the application as embedded
resources, so a released build carries them without loose files beside the executable.

Profiles here are **read-only at runtime**. A user's edits live in `settings.json` as an
override and can always be reset back to the file — that only works if the file itself is never
written to.

## Layout

```
profiles/
  apps/<id>.json      programs
  games/<id>.json     games
```

The file name must equal the `id`.

## Shape

```json
{
  "$schema": "../schema.json",
  "formatVersion": 1,
  "id": "adobe-photoshop",
  "name": "Photoshop",
  "kind": "app",

  "match": {
    "processes": ["photoshop"],
    "appliesToGames": false,
    "priority": 10
  },

  "highlights": {
    "Keyboard_B": { "colour": "#22CC66", "label": "Brush" },
    "Keyboard_E": { "colour": "#22CC66", "label": "Eraser" }
  },

  "shortcuts": {
    "Ctrl": {
      "characters": {
        "t": { "group": "Edit", "label": "Free transform" },
        "j": { "group": "Edit", "label": "Duplicate layer" }
      },
      "keys": {
        "Keyboard_Tab": { "group": "View", "label": "Hide panels" }
      }
    },
    "Ctrl+Shift": {
      "characters": {
        "s": { "group": "File", "label": "Save as" }
      }
    }
  }
}
```

`match`, `highlights` and `shortcuts` are the three **sections**. The user overrides them
separately: editing the highlights freezes the highlights and leaves the shortcuts following
this file, so improvements here still reach them. Keep that in mind when deciding which section
a piece of information belongs in.

## Fields

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Stable identifier, lower case, `a-z0-9-`. **Never change it** — user overrides and hidden-profile entries are keyed on it. Renaming it orphans someone's edits. |
| `name` | yes | Shown in the interface. May be changed freely; the user can rename it anyway. |
| `kind` | yes | `app` or `game`. Groups the list in the interface; carries no behaviour. |
| `match.processes` | yes | Executable names **without `.exe`**, lower case. Matching is case-insensitive. List every name the program runs under. |
| `match.appliesToGames` | no | `true` only for the generic game profile. A named title must not set this. |
| `match.priority` | no | Default `0`. Higher wins among profiles that match. A profile naming the process already outranks the generic game profile, so this is only for ties. |
| `match.titleContains` | rarely | The profile applies only if the window title contains one of these, case-insensitively. See below — most profiles must not use it. |

## `titleContains` — only for shared executable names

Some unrelated programs run under one executable name. LibreOffice starts Writer and Calc both
as `soffice`; every Java program is `javaw`. Matching on the process name alone, one profile
wins arbitrarily and the keyboard shows Calc's shortcuts to somebody writing a letter — a
confident wrong answer, which is worse than no profile at all.

```json
"match": {
  "processes": ["soffice", "soffice.bin"],
  "titleContains": ["Calc"]
}
```

Do not reach for this otherwise. Titles are localised, they change with the open document, and
a matching rule nobody can read back is worse than no rule. If a profile needs more than "the
title mentions Calc", it is solving the wrong problem.
| `highlights` | no | Key id → fixed colour, optionally with a label. |
| `shortcuts` | no | Modifier combination → the keys carrying a command under it. |

## Key ids — the position trap

Key ids come from the device profile and follow **US positions**. `Keyboard_Y` is the physical
key that types `Y` on a US keyboard — on a German one, that same key types `Z`.

This is why the format has two different ways to name a key, and using the wrong one produces a
profile that is visibly wrong on any non-US layout:

- **`highlights` use key ids — position.** WASD is about where the hand rests. `Keyboard_W`,
  `Keyboard_A`, `Keyboard_S`, `Keyboard_D` are the right keys on every layout.
- **`shortcuts.characters` use the character — meaning.** `Ctrl+Z` means "the key that types
  z". Writing it as a position would show undo and redo swapped on a German keyboard.
- **`shortcuts.keys` use key ids**, but only for keys that type nothing: Escape, Tab, Enter,
  Backspace, the arrows, the function keys. Those have no character, so there is no ambiguity.

### Y and Z need a decision, every other letter does not

QWERTZ and QWERTY differ in exactly two places, so `Keyboard_Y` and `Keyboard_Z` are the only
ids where this can go wrong — and it goes wrong silently.

A highlight id is always a **physical position**. The question is which physical key the program
actually means, and that depends on how the program reads the keyboard:

| The program binds to | Example | `Z` in its documentation means |
|---|---|---|
| the **character** (Windows virtual-key codes, which follow the layout) | Photoshop, Blender, GIMP, Krita, Inkscape — applications generally | `Keyboard_Y` — the top-row key, which types `Z` on a German board |
| the **position** (scancodes, as most game engines use, so WASD stays put) | games generally | `Keyboard_Z` — the bottom-row key |

So for Photoshop, whose zoom tool is `Z` and whose history brush is `Y`:

```json
"Keyboard_Y": { "colour": "#00C8FF", "label": "Zoom" },
"Keyboard_Z": { "colour": "#22CC66", "label": "History brush" }
```

which reads backwards until you remember that the ids are US positions and the user's keyboard
is not.

If you cannot establish which way a particular program reads the keyboard, leave the `Y` and `Z`
entries out. Every other letter is unaffected.

The full list of valid key ids is in `devices/razer-deathstalker-v2-de/device.json`. A test
rejects any id that is not in there.

## Modifier combinations

The keys of the `shortcuts` object. Exactly these eight, spelled exactly like this:

```
Win   Win+Shift   Win+Ctrl   Ctrl   Ctrl+Shift   Ctrl+Alt   Alt   AltGr
```

The order matters — `Ctrl+Shift` is valid, `Shift+Ctrl` is not. Shift on its own is not a
combination: it changes which character a key types rather than selecting a shortcut layer.

## Function groups

Every shortcut carries one, and related commands sharing a group is the whole point — the eye
reads a block of one colour, not scattered keys.

| Group | For |
|---|---|
| `File` | open, save, print, new, close, import, export |
| `Edit` | undo, copy, paste, duplicate, delete, transform, select |
| `Search` | find, replace, go to, filter |
| `View` | zoom, layout, panels, full screen, show/hide |
| `Window` | tabs, splits, arranging, switching document |
| `System` | settings, lock, power, session |
| `Tools` | tool selection, mode switches, program-specific instruments |
| `Navigation` | moving the caret or the viewport: home, end, word-wise, page-wise |

## Labels

Every shortcut needs one, and it must say what the command *does* — "Duplicate layer", not
"Ctrl+J". The LEDs cannot show text, so a label costs nothing on the hardware. It earns its
keep three times over: the preview inside the application can show it, a test can find
contradictions, and at eighty profiles it is the only way anyone can check whether an entry is
right. `"j": "Edit"` on its own is unverifiable.

Labels on `highlights` are optional but wanted wherever the key has a specific meaning —
`"W" → "Forward"` in a game, `"B" → "Brush"` in Photoshop.

Write labels in English. The interface is English; translation is a separate concern.

## Colours

`#RRGGBB`. Highlight colours should form a small, readable set within one profile — a handful of
groups, not thirty individual shades. Colours already used by the shipped game profile:

| Colour | Meaning |
|---|---|
| `#FF0000` | movement (WASD) |
| `#FF8C00` | keys the same hand reaches without moving |
| `#00C8FF` | selection — weapons, items, the number row |
| `#B4B4B4` | menu, escape |

## Accuracy

A wrong shortcut is worse than a missing one: the keyboard then shows something untrue, and the
user has no way to tell. So:

- Only write down what you are confident is the program's **default** binding, out of the box.
- Leave out anything you are unsure of. A profile with twelve correct shortcuts beats one with
  thirty of which four are wrong.
- Do not invent. If a program has no well-known shortcut for something, it gets no entry.
- Where a program's defaults differ between versions, follow the current one.

## Checks

`dotnet test` validates every file here: id unique and matching the file name, key ids present
in the device profile, colours parsable, groups and modifier combinations valid, no character
assigned twice within one combination, every shortcut labelled.
