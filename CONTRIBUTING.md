# Contributing to Keylegend

Thanks for considering a contribution. There are three kinds of help that are especially
valuable, and two of them need no programming at all.

## 1. Confirming a keyboard (no programming required)

There is nothing to write for a new keyboard. Keylegend asks Razer Synapse which one is
plugged in and reads Razer's own drawing of that model, so support for a keyboard is not something
this repository can carry or lack.

What it cannot do by itself is confirm that the lighting lands on the right keys. Which cell of the
matrix a key belongs to comes from the Chroma protocol's own table, and that table is checked by hand
against exactly one keyboard — a DeathStalker V2 in ISO-DE. Every other model is correct by
inference.

Checking it needs no special mode, because the window already shows what the hardware should look
like: the keyboard on screen and the keyboard on the desk are filled by the same code. Hold the two
side by side.

If a key lights up on the hardware while a different one is lit in the window, that is worth an
issue — please say **which keyboard and which physical layout** (ISO/ANSI/JIS and language), and
which key is wrong. A photo is welcome but not required. A report that everything matched is just as
useful: it turns an inference into a confirmed model.

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

Nineteen tests report as **skipped** unless Razer Synapse is installed: they read the vendor's own
keyboard drawings, which cannot be copied into this repository. `506 passed` and `487 passed, 19
skipped` are both green runs — the second is what a machine without Synapse looks like, and it is
not a fault.

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
logo. Nothing here needs a picture — the preview is drawn from Razer's own drawing on your machine
— so the question usually does not arise. See [NOTICE.md](NOTICE.md).