# Keylegend — working notes

Interactive keyboard lighting for Razer Chroma: keys are coloured by what they currently mean.

## Architecture rules that are load-bearing

These are not style preferences. Breaking them breaks the design.

1. **`Keylegend.Core` stays pure.** No Windows APIs, no network, no file system, no references
   to the other projects. The whole point is that colouring logic runs in tests without
   hardware. Platform code goes in `Keylegend.Windows`, Chroma access in `Keylegend.Chroma`.
2. **No global keyboard hooks, ever.** Only modifier and lock *states* are polled
   (`GetAsyncKeyState`, `GetKeyState`). Keystrokes are never intercepted, forwarded, logged or
   stored. This is a privacy commitment stated in the README — treat it as non-negotiable.
3. **Key meanings come from Windows, not from tables.** `ToUnicodeEx` against the active layout
   decides which character a key produces; the category follows from that character. Do not
   hardcode layout tables — that is what makes every keyboard layout work for free.
4. **Device support is asked for, not carried.** There is no file per keyboard and no list of
   models. Razer Synapse says which keyboard is attached and Razer's own drawing of that model
   supplies its geometry, casing and printed legends; the matrix cell of each key comes from the
   protocol's `RZKEY` table. If a change would require a file or a branch for a new keyboard,
   something has gone wrong. The one profile ever calibrated by hand is kept as test data and is
   the yardstick all of this is checked against.

## Modifier handling

Windows reports AltGr as Ctrl + right Alt, and Ctrl + left Alt produces the same characters.
They are distinguished by side:

- right Alt → AltGr layer (character assignment)
- Ctrl + left Alt → the `Ctrl+Alt` shortcut set

Always evaluate left/right variants separately (`VK_LMENU`/`VK_RMENU`, etc.).

Filtering modifiers (AltGr, Win, Alt, Ctrl) blank out unassigned keys. Shift, Caps Lock and
Num Lock do **not** filter — they only change which character is produced.

## Chroma facts worth remembering

- Colours are **BGR** integers: `(B << 16) | (G << 8) | R`
- Keyboard matrix is 6 rows × 22 columns; `CHROMA_CUSTOM` takes `[[22 ints] × 6]`
- Session needs a heartbeat more often than every 10 s
- Measured: session create 60–125 ms; first frame after taking over from Chroma Studio ~500 ms;
  every subsequent frame ~2 ms; `DELETE` returns control to Chroma Studio immediately
- Key cells follow Razer's `RZKEY_*` constants (`0xRRCC`). ISO extras: `EUR_1` = row 3 col 13
  (the `#` key), `EUR_2` = row 4 col 2 (the key left of Y/Z)

## Commands

```bash
dotnet build
dotnet test        # everything except the checks against the vendor's own files
```

## What the tests can and cannot reach

Everything the program decides is testable without hardware, and that includes which drawing it
believes: `SvgLayoutSource.Find` takes the directories to search, so a test writes a cache of files
shaped like the vendor's and checks the choice — that the layout the service names wins over one
that merely fits, that another model's drawing is ignored, that a file too small to hold a keyboard
is skipped unread. `DrawingChoiceTests` does that, and it runs anywhere.

What no test can reach is whether the vendor's real files still look the way this program assumes.
That needs the files, and they cannot be copied here — they are Razer's artwork and the licence
here is MIT. So the tests reading the local installation stay, and on a machine without Synapse they
pass without checking anything. They are the ones in `SvgLayoutSourceTests`, `FromDrawingTests` and
`LegendPlacementTests`; nineteen test cases, and a green run on a build machine says nothing about
them. Run them on a machine with the vendor's software before trusting a change to the parser.

## Documentation

The design specification and reasoning live in `docs/`, one folder per language with the same
file names in each. **English and German are the maintained originals**: when behaviour changes,
update `docs/en/` and `docs/de/` and add a `CHANGELOG.md` entry under `## [Unreleased]`. The
other nine languages (`es`, `fr`, `it`, `nl`, `pl`, `pt`, `ru`, `uk`, `zh-cn`) follow when
someone gets to them — a stale translation is a known cost, a wrong English page is not.

Interface texts live in `src/Keylegend.App/Localisation/Strings*.resx`, eleven files with the
same keys. A key missing from a translation falls back to English, so adding a string means
adding it to `Strings.resx` first; the rest can catch up.

## Licence hygiene

Everything here ships under MIT, so everything here has to be licensable under MIT. Two
consequences that are easy to get wrong:

- **No vendor artwork.** Not a product render, not a press photo, not a logo — see `NOTICE.md`.
  Nothing here needs a picture at all; the preview is drawn from the geometry.
- **No derived vendor data.** Key geometry comes from the published 19.05 mm grid, not from
  anyone's layout files. The Chroma matrix cells are interoperability facts, which is different
  and is fine.

The two donation buttons in `src/Keylegend.App/Assets/` are the sole exception and are excluded
in `NOTICE.md`. Do not add a third exception without a good reason.

## Trademark

Never put "Razer" or "Chroma" in the project name, package names, or namespaces. Referential
use in prose ("for Razer Chroma") is fine and intended; the trademark notice in the READMEs
must stay.
