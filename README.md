# Keylegend

**Interactive keyboard lighting for Razer Chroma — your keys light up by what they actually do.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Version 1.0.0.** Lighting, interface, game detection and application profiles all work.
> [Download the installer or the portable copy](https://github.com/Eistee82/Keylegend/releases/latest),
> or build from source. See [CHANGELOG.md](CHANGELOG.md).

![Keylegend colouring the keyboard by what each key currently means, and switching profile as the front application changes](docs/images/keylegend.png)

---

## What it does

Most RGB software treats your keyboard as decoration. Keylegend treats it as a **display**.

Every key is coloured by what it *currently* means — and that colouring changes the instant
its meaning changes:

- **Lock states at a glance.** Num, Caps and Scroll Lock show their state on the key itself.
- **Colour by character class.** Digits, lowercase letters, uppercase letters, symbols and
  control keys each get their own colour.
- **Hold a modifier, see the layer.** Press `AltGr` and only the keys that actually carry an
  AltGr character stay lit. Press `Win` and the Windows shortcuts light up, grouped by
  function. Same for `Alt`, `Ctrl` and their combinations.
- **Shift and Caps Lock just work.** Because the character each key produces is queried live
  from Windows, letters switch from the "lowercase" colour to the "uppercase" colour by
  themselves. The number pad recolours to navigation when Num Lock is off.
- **Games get their own treatment.** Detected automatically — including borderless-windowed
  ones — and WASD, the keys around it and the number row take fixed colours, because while
  playing it matters where your hands go and not which letter a key types.
- **Per-application profiles, around ninety of them included.** Photoshop, Visual Studio Code,
  Excel, Elden Ring and the rest take effect the moment the program has the focus, and a profile
  naming a program outranks the general game profile. Edit one and only the part you edited
  stops following the shipped version — the rest keeps improving with later releases.
- **Gives the lighting back.** After a configurable idle period (60 s by default), Keylegend
  releases the keyboard so your Chroma Studio effect takes over again.
- **Eleven languages.** English, German, Spanish, French, Italian, Dutch, Polish, Portuguese,
  Russian, Ukrainian and simplified Chinese. The interface follows the Windows display language
  and can be switched in the settings. Key legends are unaffected — they follow your keyboard,
  not the menus.

Because the key meanings come from the **active Windows keyboard layout** rather than a
hardcoded table, Keylegend works with any layout — German, US, French, Dvorak — without
changes.

## How it works

Keylegend asks Windows what character each key would produce in the current keyboard state
(`ToUnicodeEx`), derives a category from that character, and sends the resulting colour map
to the Razer Chroma SDK over its local REST interface.

It deliberately does **not** install a global keyboard hook. It only reads modifier and lock
*states*; it never intercepts, forwards or records keystrokes. See
[docs/en/architecture.md](docs/en/architecture.md).

## Requirements

- Windows 10 or 11
- Razer Synapse with the Chroma SDK service running
- A Chroma-capable keyboard with a device profile (see below)
- .NET 10 runtime

## Installing

```powershell
winget install Eistee82.Keylegend
```

That is the shortest route: winget fetches the .NET runtime as a declared dependency, so there is
no prerequisite to install by hand. Otherwise, take a file:

[**Download the latest release.**](https://github.com/Eistee82/Keylegend/releases/latest)

| File | What it is |
|---|---|
| `Keylegend-1.0.0-setup.exe` | Installs for the current user — no administrator rights. Start menu entry, and a clean uninstall that also removes the autostart entry. |
| `Keylegend-1.0.0-portable.zip` | The same program, to unpack and run. Keep the `devices` folder next to the executable. |

Both are unsigned, so Windows will call the publisher unknown — a certificate costs more per year
than this project has. Each release ships `SHA256SUMS.txt` if you would like to check the download
is intact, and the build log that produced it is public.

## Supported keyboards

Device support is **data, not code**. A keyboard is one file in `devices/`: `device.json`,
holding the key geometry and the mapping from keys to Chroma matrix cells.

Thirty-two profiles ship. One of them has been stepped through on real hardware; the rest are
generated from the standard key dimensions, which makes their geometry exact and their LED
mapping an educated guess.

| Keyboard | Layout | Status |
|---|---|---|
| Razer DeathStalker V2 | ISO-DE | **verified on hardware** |
| Razer DeathStalker V2, BlackWidow V4, Huntsman V3 Pro, Ornata V3 | ANSI-US, ISO-DE | generated |
| Full-size, 105/104 keys | ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP, ABNT2-BR | generated |
| Tenkeyless | ANSI-US, ISO-DE, ISO-UK, ISO-FR | generated |
| 75 %, 65 %, 60 % | ANSI-US, ISO-DE | generated |

`physicalLayout` describes the *shape* of the keyboard, not the language you type in. Which
character each key produces is asked from Windows at runtime, so one ISO-DE profile serves a
German keyboard whether Windows is set to German, US or Dvorak.

**Does your keyboard light up the wrong keys?** That is what "generated" means, and correcting it
needs no programming — about ten minutes with the calibration mode. See
[docs/en/adding-a-keyboard.md](docs/en/adding-a-keyboard.md). Corrections are as welcome as new
profiles, and turn a guess into a `verified` profile for everyone with that keyboard.

## Documentation

| Topic | |
|---|---|
| Architecture | how the colouring is decided, and why there is no keyboard hook |
| Adding or correcting a keyboard | device profiles, calibration, and what to do when keys light up wrong |
| Adding a profile | per-application colouring |
| Device profile format | every field, in detail |
| Configuration | settings, the settings file, autostart |

Available in eleven languages:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

The interface speaks the same eleven, following the Windows display language and switchable in
the settings. Key legends are unaffected — they follow your keyboard, not the menus.

English and German are the maintained originals; where a translation disagrees with them, the
English text is the one that is right. Corrections are welcome, see
[CONTRIBUTING.md](CONTRIBUTING.md).

## Building and running

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

Two programs are produced. **`Keylegend.exe`** (`src/Keylegend.App`) is the application: window,
notification-area icon, settings. It is what you want for normal use.

**`keylegend-cli.exe`** (`src/Keylegend.Host`) is a console driver with the diagnostics:

| Command | What it does |
|---|---|
| `keylegend-cli` | Runs the lighting. Takes over on the first keypress, hands back after 10 s idle. |
| `keylegend-cli --idle 30` | Same, with a 30-second idle timeout. |
| `keylegend-cli --once 10` | Paints the current state once and holds it for ten seconds. Good first check. |
| `keylegend-cli --calibrate` | Lights one key at a time so a device profile can be verified. |
| `keylegend-cli --dump-layout` | Prints what every key resolves to, plain / Shift / AltGr. |
| `keylegend-cli --watch-foreground` | Reports what the game detection sees as windows change. |
| `keylegend-cli --profile <path>` | Uses a specific `device.json`. |

Settings live in `%APPDATA%\Keylegend\settings.json` and are written by the application.

## Contributing

Bug reports, device profiles and translations are all welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licence

[MIT](LICENSE). Two third-party donation buttons are excepted, and no vendor code, header,
library or artwork is contained here — see [NOTICE.md](NOTICE.md).

## Trademark notice

This project is **not affiliated with, endorsed by, or sponsored by Razer Inc.**

RAZER and RAZER CHROMA are trademarks or registered trademarks of Razer Inc. They are used
here solely to identify the hardware and the software interface this project works with, as
permitted by referential use. Keylegend is an independent, community-maintained project.

The same applies to every other name in this repository. The application and game profiles
name around ninety programs — Photoshop, Visual Studio Code, Excel, Elden Ring and the rest —
and the device profiles name keyboard vendors and models. Those are trademarks of their
respective owners and appear only to say which program or which keyboard something is for.
Keylegend is not associated with any of them and contains none of their code or assets. See
[NOTICE.md](NOTICE.md).
