# Contributing to Keylegend

Thanks for considering a contribution. There are three kinds of help that are especially
valuable, and two of them need no programming at all.

## 1. Device profiles (no programming required)

Keylegend describes keyboards as **data**. Adding support for a new keyboard means writing
two files, not writing code:

- `devices/<vendor>-<model>-<layout>/device.json`
- `devices/<vendor>-<model>-<layout>/device.png`

The step-by-step guide is in [docs/en/adding-a-keyboard.md](docs/en/adding-a-keyboard.md).
The application includes a calibration mode that lights up matrix cells one at a time, so you
can work out the mapping by watching your own keyboard rather than by guessing.

Please state in the pull request **which keyboard and which physical layout** (ISO/ANSI/JIS
and language) you verified the profile against. A photo of the keyboard lit by the
calibration mode is welcome but not required.

## 2. Translations

The interface and documentation currently ship in English and German. To add a language:

- Interface strings: add a resource file under `src/Keylegend.App/Resources/`
- Documentation: add a folder under `docs/<language-code>/`
- Add your language to the table in both READMEs

Partial translations are accepted; missing strings fall back to English.

## 3. Code

Before starting anything substantial, please open an issue describing what you intend to do.
The design specification lives in `docs/` — please read the architecture document first, as
the separation between the pure decision logic and the platform adapters is deliberate and
load-bearing.

### Ground rules for code

- **Keep `Keylegend.Core` free of platform and network dependencies.** It must remain testable
  without a keyboard, without Synapse, and without Windows. Anything touching Windows APIs
  belongs in `Keylegend.Windows`, anything touching the Chroma service in `Keylegend.Chroma`.
- **No global keyboard hooks.** Keylegend reads modifier and lock *states* only. It must never
  intercept, forward, log or store keystrokes. Contributions that add keystroke capture will
  be declined regardless of purpose.
- **Cover decision logic with tests.** Colouring rules, categories, shortcut lookup and session
  transitions all run without hardware — there is no reason for them to be untested.
- Follow the existing `.editorconfig`.

### Building

```bash
dotnet build
dotnet test
```

CI runs the same two commands on Windows for every pull request.

## Commit messages and pull requests

Describe what changed and why. If a change alters observable behaviour, add an entry under
`## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md).

## Code of conduct

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licence of contributions

By contributing you agree that your work is licensed under the [MIT License](LICENSE) that
covers this project.

That means everything you contribute has to be yours to license. In practice the one thing this
rules out is **artwork you did not make**: a keyboard vendor's product render, a press photo, a
logo. Device profiles need no picture — the preview is drawn from the geometry — so the question
usually does not arise. See [NOTICE.md](NOTICE.md).
