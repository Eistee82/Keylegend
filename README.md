# Keylegend

**Interactive keyboard lighting for Razer Chroma — your keys light up by what they actually do.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Version 1.2.0.** Lighting, interface, game detection and application profiles all work.
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
- **The lighting can answer the typing.** Eight effects to choose from, *none* by default:
  the struck key fades, flashes or glows on, a water drop or a dark wave runs across the board,
  the keys around it shake, sparks fly, or keys warm with use and cool down again. Laid over the
  colours rather than mixed into them, so every key still says what it means.
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

It deliberately does **not** install a global keyboard hook. It reads key *states* — whether a key
is down at this moment — and never intercepts, forwards or records keystrokes. With no typing effect
chosen it looks at the modifier and lock states alone; a typing effect additionally asks which of
this keyboard's own keys are down, and nothing further.
See [docs/en/architecture.md](docs/en/architecture.md).

## Requirements

- Windows 10 or 11
- Razer Synapse with the Chroma SDK service running
- A Razer Chroma keyboard, connected (see below)
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
| `Keylegend-1.2.0-setup.exe` | Installs for the current user — no administrator rights. Start menu entry, and a clean uninstall that also removes the autostart entry. |
| `Keylegend-1.2.0-portable.zip` | The same program, to unpack and run. Keep the language folders (`de`, `fr`, …) next to the executable, or the interface falls back to English. |

Both are unsigned, so Windows will call the publisher unknown — a certificate costs more per year
than this project has. Each release ships `SHA256SUMS.txt` if you would like to check the download
is intact, and the build log that produced it is public.

## Supported keyboards

**Every Razer Chroma keyboard.** There is no list, and no file per model, because Keylegend does
not need to recognise your keyboard — it asks. Razer Synapse describes the one that is plugged in:
the model by name, the physical layout as a number, and the keys the hardware actually has. Its own
drawing of that model supplies the rest — the real key sizes, the casing with its dial and media
keys, and the outlines of the characters printed on the caps, in the right language.

The one thing the drawing does not say is which cell of the lighting matrix each key belongs to.
That is a constant of the Chroma protocol, identical on every model, which is why Synapse itself
needs no per-model table either. Checked against the one keyboard this was calibrated on by hand,
all 105 keys agree.

The **physical layout** describes the *shape* of the keyboard, not the language you type in. Which
character a key produces is asked of Windows at run time, so a German keyboard is served correctly
even with Windows set to US or Dvorak.

**Requires Razer Synapse**, installed and running, with the keyboard connected. That is where the
keyboard is described and where its drawing is kept.

## Documentation

| Topic | |
|---|---|
| Architecture | how the colouring is decided, and why there is no keyboard hook |
| Adding a profile | per-application colouring |
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
cd Keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) is the whole program: window, notification-area icon,
settings. `--verify` is the one switch worth knowing — it checks that a copy carries the shipped
profiles and all eleven languages, writes what it found to the path given after it, and answers
through its exit code. That is what the release script runs against a packaged copy.

Settings live in `%APPDATA%\Keylegend\settings.json` and are written by the application.

## Contributing

Bug reports, application profiles and translations are all welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licence

[MIT](LICENSE). Two third-party donation buttons are excepted, and no vendor code, header,
library or artwork is contained here — see [NOTICE.md](NOTICE.md).

## Trademark notice

This project is **not affiliated with, endorsed by, or sponsored by Razer Inc.**

RAZER and RAZER CHROMA are trademarks or registered trademarks of Razer Inc. They are used
here solely to identify the hardware and the software interface this project works with, as
permitted by referential use. Keylegend is an independent, community-maintained project.

The same applies to every other name in this repository. The application and game profiles name
around ninety programs — Photoshop, Visual Studio Code, Excel, Elden Ring and the rest — and the
documentation names keyboard vendors and models. Those are trademarks of their respective owners
and appear only to say which program or which keyboard something is for. Keylegend is not
associated with any of them and contains none of their code or assets. See [NOTICE.md](NOTICE.md).