# Device profile format

A device profile describes one keyboard model in one physical layout. It is a single file in a
folder under `devices/`, named `<vendor>-<model>-<layout>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometry and LED mapping
```

`devices/device-profile.schema.json` describes the same thing in machine-readable form. Naming
it in a `$schema` line, as the shipped profiles do, gives most editors completion and inline
errors while you type.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Field | Meaning |
|---|---|
| `formatVersion` | Format revision. Currently `1`. A build refuses a profile numbered higher than it understands. |
| `name` | What the interface shows. |
| `vendor`, `model` | Who makes it and which model. `"Generic"` for a profile describing a layout rather than a product. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — the physical key *arrangement*, not the software layout. |
| `canvas` | The coordinate system all key positions refer to. Only ratios matter; the shipped profiles use millimetres. |
| `matrix` | Size of the vendor LED matrix. Razer keyboards are 6 × 22 whatever their size. |
| `verified` | `true` once someone has confirmed the mapping on real hardware. |
| `note` | Optional free text for whoever opens the file next. |
| `image` | Optional, and currently unused — see [Pictures](#pictures) below. |
| `keys[]` | One entry per key. |

### Physical layout, not software layout

`physicalLayout` decides the *shape* of the keyboard: whether the Enter key is tall and
L-shaped, whether there is an extra key left of `Z`, whether the bottom row carries Japanese
conversion keys.

It says nothing about which characters those keys produce. Keylegend asks Windows that at
runtime, for the layout currently active. One ISO-DE profile therefore serves a German keyboard
whether Windows is set to German, US, Dvorak or Neo — which is why there is one profile per
*physical* layout and not one per language.

### Key entries

| Field | Meaning |
|---|---|
| `id` | Unique identifier. Follow the existing naming: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Position of the top-left corner on the canvas. |
| `width`, `height` | Size of the key on the canvas. |
| `row`, `column` | Cell in the vendor LED matrix. Both `null` while unknown — a valid state, and what calibration is for. |
| `scanCode` | Overrides the standard scan code. Only needed where the physical layout disagrees with the US-based naming. |
| `parts` | Further rectangles belonging to the same key, for keys that are not rectangular. |
| `label` | What is printed on the key, for keys that type nothing. |
| `labelSecondary` | A second printed line, below the first. |

### Legends belong to the keyboard

`label` is what is *printed on the keycap*, not a translation of what the key does. A German
keyboard says `strg`, a French one says `ctrl`, an Italian one says `bloc maiusc` — and each of
them says so no matter which language Keylegend's own menus are set to. Changing the interface
language never changes the legends.

Keys that produce a character carry no `label` at all. Their legend comes from the active
Windows layout, so it follows Shift, Caps Lock and AltGr by itself.

### Keys with more than one rectangle

The ISO Enter is the standard case: one key covering two rows.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

The main rectangle carries the cell; `parts` adds the rest of the shape. The explicit
`scanCode` is there because the upper half occupies the position ANSI uses for backslash: without
it the top of the Enter key would be coloured as though it typed `\`.

### Scan codes for keys that exist on one layout only

The standard table in `Keylegend.Core` covers what a US keyboard has. Keys that exist only
elsewhere state their code in the profile, so no C# has to change for a layout:

| Id | Key | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, left of Backspace on JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, right of the right Shift on JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, left of the space bar | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, right of the space bar | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | the `/?` key right of the right Shift on ABNT-2 | `0x73` |

## Rules the validator enforces

These are checked in CI, so a profile that breaks them cannot be merged:

- Key ids are unique
- No two keys claim the same matrix cell
- No two keys overlap on the canvas
- `row` and `column` are either both set or both `null`
- Cells lie inside the declared matrix
- Keys lie inside the canvas
- Every key has a positive size
- A picture named by `image` actually exists

## Naming and the ISO/ANSI difference

Key ids follow the US layout by convention, because that is what the vendor's own matrix does.
On a German keyboard the physical `Z` therefore sits at `Keyboard_Y` and vice versa. This is
naming only: it affects neither position nor behaviour, because the actual character is queried
from Windows at runtime.

Two ids exist only on ISO keyboards:

| Id | Key | Razer cell |
|---|---|---|
| `Keyboard_NonUsBackslash` | the extra key left of `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, row 4 column 2 |
| `Keyboard_NonUsTilde` | the key next to Enter in the home row (`#`, `'`) | `RZKEY_EUR_1`, row 3 column 13 |

On ISO keyboards the tall Enter spans two matrix positions: the upper half sits where ANSI has
backslash (row 2, column 14), the lower half at `Keyboard_Enter` (row 3, column 14).

**Whether both are actually lit depends on the model.** The vendor table describes what the
matrix can *address*, not what a given keyboard has *fitted*. On the DeathStalker V2, calibration
showed the upper cell drives no LED at all — the whole Enter key is lit by the lower one, which
is why the shipped profile models the Enter as one key with two rectangles rather than as two
keys.

This is exactly the kind of thing that cannot be derived from documentation, and the reason a
profile should not be marked `verified` until someone has stepped through it on hardware.

## Pictures

`image` is optional and currently unused: the on-screen preview is drawn from the geometry above.
Drawing it keeps the preview sharp at any window size and makes it impossible for the picture and
the profile to disagree.

If you do attach one, it must be a picture **you** took or made. Everything in this repository
ships under the MIT licence, which grants everyone the right to modify and redistribute what is
in it — a right nobody can grant over a keyboard vendor's product photography. See
[NOTICE.md](../../NOTICE.md).

## See also

- [Adding or correcting a keyboard](adding-a-keyboard.md) — the practical walkthrough
