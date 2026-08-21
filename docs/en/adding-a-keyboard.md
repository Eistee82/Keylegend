# Adding or correcting a keyboard

Support for a keyboard is **data, not code**. You need no C# and no build tools — a text editor
and your own keyboard are enough.

Most people arriving here do not need to add anything, because a profile for their layout
already exists. What those profiles need is the one thing that cannot be generated: somebody
with the hardware confirming that each key lights where the profile claims. **That is the job
described in [part 2](#2-correcting-a-profile), and it takes about ten minutes.**

---

## What a profile knows, and how sure it is

A profile answers two separate questions, and they are not equally reliable:

| Question | Where the answer comes from | How sure |
|---|---|---|
| Where does each key sit, and how big is it? | The standard 19.05 mm key grid, which every keyboard since the IBM Model M has followed | **Certain.** Geometry follows from the layout. |
| Which cell of the LED matrix lights that key? | The vendor's published matrix, assuming a standard board | **A guess.** Models move keys, leave cells unfitted, and add their own. |

That split is the whole reason the `verified` flag exists. A profile marked `"verified": false`
is almost certainly right about the picture and quite possibly wrong about which key lights up.

---

## 1. Adding a layout that is missing

First check whether it really is missing: `devices/` already holds full-size profiles for
ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL,
JIS-JP and ABNT2-BR, plus tenkeyless, 75 %, 65 % and 60 % variants. If yours is among them, skip
to part 2.

### The generated way

`tools/make-layout.py` builds a profile from the standard dimensions. Adding a keyboard to it
is one entry in the `PROFILES` list at the bottom of the file:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argument | What it decides |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` or `abnt2` — the shape of the Enter and which extra keys exist |
| `legends` | Which set of printed key legends to use: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` or `fn` — what sits between the right Alt and the menu key |

Then run it:

```bash
python tools/make-layout.py --only iso-tr
```

If your keyboard's legends are not among the five sets, add one: copy `LEGENDS_EN` in the same
file, translate the entries, and register it in `LEGEND_SETS`. Only keys that type *nothing*
need a legend — the rest are asked from Windows at runtime, which is what makes one profile
serve every software layout on the same hardware.

### The hand-written way

For a keyboard that is not a variation on a standard layout — an ortholinear board, a split one,
something with a row of macro keys nobody else has — write `device.json` directly. The
[format description](device-profile-format.md) lists every field, and
`devices/device-profile.schema.json` gives most editors completion and inline errors.

You do not need to be exact on the first pass. Get the keys roughly right, leave `row` and
`column` as `null` for anything you are unsure about, and let calibration fill in the rest.

---

## 2. Correcting a profile

This is the part that needs the hardware, and the part that actually matters.

### Look at it first

Before touching the keyboard, check the picture:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-fr/device.json
```

That writes `preview.svg` beside the profile; open it in any browser. Compare it with the
keyboard in front of you and look for:

- keys that are missing, or keys drawn that your keyboard does not have
- an Enter of the wrong shape — tall and L-shaped on ISO, wide and flat on ANSI
- a bottom row with the wrong number of modifiers, which varies more than anything else
- **red outlines**, which mark keys with no matrix cell. Those will never light.

Fixing geometry is arithmetic, not guesswork: the grid is one unit per key, and a unit is
whatever `width` the ordinary letter keys have.

### Then calibrate

Calibration lights one key at a time and names it, so you can confirm that the key glowing white
is the key the profile claims. It is the only way to be certain: everything else is inference
from a vendor table.

```bash
keylegend-cli --profile devices/<your-folder>/device.json --calibrate
```

It walks the mapped keys in reading order:

| Key | What it does |
|---|---|
| `Enter` or `→` | this one is correct, move on |
| `F` | the wrong key lit up — record it |
| `←` | back one key |
| `A` | light every mapped key at once |
| `S` | skip to the summary |
| `Q` or `Esc` | stop |

Because key ids follow the US layout, the prompt also shows what each key actually types on
*your* machine — so on a German keyboard you are told "the ß key", not
`Keyboard_MinusAndUnderscore`.

Findings are written to `calibration-findings.txt` as you go, not at the end. Calibration is
patient work and closing the window must not cost you it.

A second picture helps while you work — this one labels every key with the cell it claims instead
of its legend:

```bash
python tools/preview-layout.py devices/<your-folder>/device.json --cells
```

### Apply what you found

`tools/apply-calibration.ps1` writes the findings back, keeping a `.bak` copy:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<your-folder>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` is for keys that lit up nothing at all: the matrix can address the cell, but this model
has no LED there. Those keys keep their geometry — the key exists, and the preview should draw it
— and lose their `row`/`column`, so nothing is sent into the void. `-Remap` is for keys mapped to
the wrong cell.

### What to expect

These are the places where a generated profile is most often wrong:

| Where | What happens |
|---|---|
| **The ISO Enter** | It spans two cells. On many keyboards only the lower one is fitted with an LED, and the upper half is lit by its neighbour or not at all. |
| **The bottom row** | The number and width of modifiers differs between models. Gaming keyboards put `Fn` where office keyboards have a second Windows key. |
| **Macro and media keys** | Often on column 0 or on the outer columns, and often on no cell at all. |
| **Compact keyboards** | The matrix keeps its full 6 × 22 size; a 60 % board simply leaves most of it empty. Cells do not renumber. |
| **The number pad's tall keys** | Plus and Enter cover two rows but answer to one cell — usually the upper one. |

A key that turns out to have no LED keeps its geometry and loses its cell:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

It is still drawn, so the preview matches the hardware; it simply never lights. That is correct,
not a defect.

### Make your keyboard recognisable

Keylegend asks Windows which keyboards are plugged in and picks the profile whose `usb` ids
match. That is the difference between it finding your keyboard and it guessing — and with more
than thirty profiles shipped, a guess is worth very little.

Find your ids:

```powershell
Get-PnpDevice -Class Keyboard | Select-Object FriendlyName, InstanceId
```

The instance id reads something like `HID\VID_1532&PID_0295&MI_01\...`. Those four hex digits
after `VID_` and `PID_` go into the profile:

```jsonc
"usb": { "vendorId": "1532", "productId": "0295" }
```

A vendor uses one product id across layouts, so both the ISO and the ANSI profile for a model
carry the same pair. Which of them applies is then decided by the keyboard layout Windows is
running — a hint, not a certainty, and only ever used to break that tie.

The field is optional. Without it a profile still works; it just has to be chosen rather than
found.

### Mark it verified

When every cell matches, pass `-MarkVerified` to the same script, or set `"verified": true` by
hand, and remove the `note` saying the profile was generated. That flag is what tells the next
person with your keyboard that they can trust it.

---

## 3. Test it

```bash
dotnet test
```

The shipped-profile tests validate every profile under `devices/`, including yours. They catch
duplicate ids, two keys claiming the same LED, keys drawn on top of each other, cells outside
the matrix, and geometry that has drifted off the canvas.

## 4. Open a pull request

Say which keyboard and which physical layout you checked, and whether you stepped through
calibration. See [CONTRIBUTING.md](../../CONTRIBUTING.md).

Profiles with `"verified": false` are welcome too — they give the next person with that keyboard
a head start. A correction to an existing profile is worth just as much as a new one.

### About pictures

The `image` field is optional and currently unused: the preview is drawn from the geometry, which
stays sharp at any size and cannot disagree with the profile. If you do attach a picture, it must
be one **you** photographed or drew. A vendor's product render cannot be released under this
project's MIT licence, and a pull request carrying one will be asked to remove it.

## See also

- [Device profile format](device-profile-format.md) — every field, in detail
- [Architecture](architecture.md) — why key meanings come from Windows rather than from a table
