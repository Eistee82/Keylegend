# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Keylegend no longer carries its own description of your keyboard. Razer Synapse already holds one —
the model, the physical layout, which keys the hardware actually has, and a drawing of the whole
board down to the characters printed on the caps — so that is what is used now, read from the local
installation while the program runs. The keyboard on screen is therefore the keyboard on the desk:
its real key sizes, its casing with the volume dial and the media strip, and its legends in the
language they are actually printed in. Nothing has to be recognised, chosen or measured, and there
is no list of supported models to be absent from.

### Added
- Windows shortcuts that were missing: the five taskbar layers on the digit row, the Microsoft 365
  layer on Win+Ctrl+Alt+Shift, the on-screen keyboard, the taskbar settings and the Game Bar
- More complete shortcuts for Chrome, Edge and Firefox, with the same highlights on the keys all
  three give a meaning to unmodified — F1, F3, F5, F7, F11, F12 and Esc
- A profile can now define what the keyboard shows under Shift, and with no modifier held at all.
  For programs that use the keyboard for functions rather than for writing: a game binds Shift to
  sprint, where which character a key types is beside the point. Nothing shipped uses either, so a
  keyboard behaves as before until a profile says otherwise
- **Keylegend says when the lighting is not working.** If talking to Chroma fails — the service
  stopped, another program holding the session — the status line carries the reason in amber, the
  notification area says so in its tooltip, and one balloon announces it. All three are withdrawn as
  soon as a frame gets through again. This is the case the window alone cannot cover: it is usually
  closed, and a keyboard that stops lighting otherwise looks like the program having quietly given
  up

### Changed
- **Razer Synapse is now required**, installed and running, with the keyboard connected. It is where
  the keyboard is described and where the drawing lives. Without it the program says so and stops
- Colours in the palette are saturated now. A pale one is a tinted white on a lit keycap, and it was
  indistinguishable from the keys next to it — measured on the hardware, not in the preview
- The lit legends keep their hue and glow at any window size

### Removed
- **The shipped device profiles, and with them the whole idea of them.** There is no `devices/`
  folder, no profile format, no generator, and nothing to write when a keyboard is not listed. One
  profile is kept as test data: the keyboard this was developed against, which every generated one
  is checked against
- **The calibration mode.** It existed to confirm a hand-measured profile against the hardware, and
  there are no hand-measured profiles left. The check it performed needs no mode of its own either:
  the keyboard in the window and the keyboard on the desk are filled by the same code, so holding
  the two side by side is the comparison. If they disagree, the window already names the key that
  should have lit
- **Everything left over from a keyboard being described in a file.** What the program builds for
  the attached keyboard now holds only what it reads: a name, the physical layout, the drawing
  surface, the matrix size and the keys. Gone are `formatVersion`, `verified`, `usb`, `note`,
  `image`, `vendor` and `model` — seven fields that were written and never read — along with
  `UsbId` and the reader for such files. The one hand-measured keyboard is kept as what it always
  was, a table of measurements, and no longer pretends to be a profile
- **`keylegend-cli.exe`.** A release is one executable now. The console driver existed for
  diagnostics from a time when the window could not show them: it ran the lighting without a
  window, printed what each key resolves to, and stepped through the matrix. The window does all of
  that, and it says when the lighting is not working. What it could do that the window could not —
  answer a script about whether a packaged copy is sound — is now `Keylegend.exe --verify`, which
  opens no window, needs no keyboard, writes its findings to a path given after it and answers
  through its exit code

### Fixed
- **An application profile no longer blanks the shortcuts it does not mention.** A profile naming the
  Ctrl layer for its own commands dropped everything it did not repeat, and what it dropped most
  often was the clipboard — Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+Z and Ctrl+A went dark in browsers, chat
  clients and terminals, which are programs one does little but type and paste in
- A settings file no longer pins the colour palette. Every colour was saved, the untouched ones
  included, so improvements to the palette reached nobody who had ever run the program
- The ISO Enter is drawn and lit as the L-shape it is, so its legend sits where the keyboard has it
  and its light stays off the key beside it

## [1.0.0] — 2026-08-22

First release. Everything below is what it contains; there is no earlier version to compare it
against.

### Added
- **Installer and portable release.** `Keylegend-1.0.0-setup.exe` installs for the current user
  without administrator rights, adds a start menu entry, and removes both the program and its
  autostart entry on uninstall. `Keylegend-1.0.0-portable.zip` is the same program to unpack.
  Neither is code-signed, so Windows reports an unknown publisher; every release carries
  `SHA256SUMS.txt` and the build log that produced it is public
- A release workflow that builds both artefacts from a `v*` tag, and `tools/build-release.ps1`,
  which does the same work locally so a release can be reproduced off CI
- An animated demonstration in the READMEs, recorded from the running application by
  `tools/record-demo.ps1` and assembled by `tools/build-demo.py`
- **Windows Package Manager manifests** under `packaging/winget`, so Keylegend can be installed
  with `winget install Eistee82.Keylegend`. The manifest declares the .NET Desktop Runtime as a
  dependency, which means winget fetches it first — the one real drawback of shipping
  framework-dependent, handled by the package manager rather than by the user
- Continuous integration now packages the release on every change, not only at a tag: the two
  defects below were both invisible to a build and a test, and only appeared once something was
  actually assembled
- **Thirty-two device profiles**, generated by `tools/make-layout.py` from the standard 19.05 mm
  key grid: full-size in thirteen physical layouts (ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES,
  ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP, ABNT2-BR), tenkeyless in four,
  75 %, 65 % and 60 % in two each, and named profiles for the Razer DeathStalker V2,
  BlackWidow V4, Huntsman V3 Pro and Ornata V3. All are `"verified": false` until someone
  steps through calibration on the hardware — the geometry is exact, the LED mapping is inferred
- **Interface in eleven languages**, selectable in the settings and following the Windows
  display language by default
- `tools/make-layout.py` — builds a device profile from a form factor, a layout variant and a
  set of key legends, so adding a keyboard is one entry in a list rather than a file of JSON
- `tools/preview-layout.py` — renders a device profile as an SVG, so a profile can be checked
  without owning the keyboard. `--cells` labels each key with the matrix cell it claims, which
  is the view to work from while calibrating
- `devices/device-profile.schema.json` — the profile format as JSON Schema, giving editors
  completion and inline errors
- `NOTICE.md` — what is not covered by the MIT licence, and which third-party names appear here
- A `note` field on device profiles, for telling whoever opens the file next what is unchecked
- The profile validator now rejects keys drawn on top of each other

- **The attached keyboard is recognised**, rather than a profile being guessed. Windows is asked
  which keyboards are plugged in (Raw Input, device names only — nothing is read *from* them),
  and a profile carrying a matching `usb` pair wins outright. Where several profiles describe the
  recognised model, the active Windows keyboard layout separates the ISO and ANSI variants. The
  Chroma SDK cannot answer this: its REST interface is organised by device class and a session
  returns an id and a URI, nothing about the hardware
- A `usb` field on device profiles, holding the vendor and product id as four hex digits each

### Fixed
- **The default profile is chosen, not stumbled upon.** With one profile shipped, "the first file
  found" was the right one; with thirty-two it was whichever name sorted first — a 60 % layout,
  which left two thirds of a full-size keyboard dark, because a profile that does not mention a
  key cannot light it. The choice is now ranked: recognised hardware, then the layout, then
  `verified`, then the most keys, and only then by name
- **A frame the Chroma service rejected was reported as sent.** The service answers everything
  with HTTP 200 and puts the outcome in the body, so checking the status code claimed success
  for frames the keyboard never showed. The `result` field now decides, the service's own
  `error` wording is passed through, and the codes a user can act on are named — 4309 is Chroma
  switched off in Synapse, 1152 another application holding the session, 1167 no device
  connected
- `tools/screenshot.ps1` picked the first process named Keylegend, which is not always the one
  owning the window; it failed with an unhelpful "Parameter is not valid" from deep inside the
  bitmap constructor
- The console host had its own copy of the profile search, with the same defect. It now uses the
  shared locator and reports whether the profile was recognised or merely chosen

### Changed
- Documentation restructured: one folder per language, the same file names in each
- Device profile pictures are optional. The on-screen preview has always been drawn from the
  geometry rather than from a bitmap, so the field did nothing; requiring it only invited
  contributions of vendor product renders, which cannot be released under the MIT licence

### Removed
- `devices/razer-deathstalker-v2-de/device.png` — a vendor product render that the application
  never displayed and this project cannot license
- Project scaffolding: solution, project layout, build configuration
- Design specification covering architecture, colouring rules, shortcut sets and session handling
- Device profile format — keyboards are described by data files, not code
- Device profile for the Razer DeathStalker V2 (German ISO layout)
- Documentation in eleven languages: English, German, Spanish, French, Italian, Dutch,
  Polish, Portuguese, Russian, Ukrainian and simplified Chinese
- Continuous integration running build and tests on Windows
- **Lighting engine**: colour by character category, lock-state display, modifier layers for
  AltGr, Win, Alt, Ctrl and their combinations
- **Character resolution from the active Windows layout**, so any keyboard layout works
  without a layout table
- **Chroma client** over the local REST interface, with session handling that hands the
  lighting back to Chroma Studio after an idle period
- **Console host** with a calibration mode for verifying a device profile against hardware,
  and a layout dump for checking character resolution

- **Graphical interface**: keyboard drawn from the device profile and mirrored live, clickable
  modifier layers for inspecting a layer without holding keys, colour pickers for every category
  and shortcut group, profile editor, settings, and a notification-area icon
- **Game detection** using Windows' own full-screen signal plus a monitor-coverage check, so
  borderless-windowed games are caught too
- **Application profiles**: lighting rules bound to programs, including a generic game profile
  that highlights WASD and the number row in any detected game
- **Settings persistence** under `%APPDATA%\Keylegend\settings.json`, written atomically
- **Autostart** via the per-user `Run` key, registered with the `--minimized` switch: a start
  by Windows comes up in the notification area without a window or a balloon, while a start by
  hand shows the window as before. Entries written before the switch existed are brought up to
  date at the next start
- `--watch-foreground` for checking the game detection against real applications
- **The hand-back can be switched off entirely**, so Keylegend keeps the keyboard until it is
  paused or closed. With it off, the lighting is taken at startup instead of waiting for the
  first keypress — otherwise the keyboard would sit dark after every start, which is the
  opposite of what switching it off is for. The idle period is remembered meanwhile, so
  switching the hand-back back on restores it rather than the default
- **German interface**, chosen automatically from the Windows display language, with English as
  the fallback for anything untranslated. Switchable in the settings — the window redraws
  immediately rather than asking for a restart. Key legends are unaffected: they come from the
  device profile, so they keep matching the keyboard in front of you rather than the menus
- **Donation buttons** in the footer, PayPal and Ko-fi, each using the artwork the service
  publishes for the purpose. They sit in the footer rather than in a tab so they need no
  hunting, and stay out of the way: the program is free and works the same whether or not
  anyone ever clicks them
- **Application icon** for the window, Explorer, the taskbar and the notification area, drawn by
  `tools/make-icon.py` with its own frame for every size from 16 to 256 px
- **Around ninety shipped application profiles** for programs and games, written as JSON under
  `profiles/` and embedded in the build. They apply on their own as soon as the matching program
  has the focus; no setup is needed
- **Per-section overrides**: a profile has a stable id, a provenance, and three sections —
  match, highlights and shortcuts — that are overridden separately. Editing one freezes only
  that section; the others keep following the shipped file and pick up its improvements.
  Resetting works per section and for the whole profile
- **Labels on shortcuts and highlights** saying what a command does. The LEDs still show colour
  only; the label feeds the preview and is what makes a shipped profile reviewable
- A profile replaces only the modifier layers it names, so system-wide Windows shortcuts stay
  accurate while a program profile is active
- **Profile format description** in `profiles/FORMAT.md`, and a guide for contributing a
  profile ([en](docs/en/adding-a-profile.md), [de](docs/de/adding-a-profile.md))

### Changed
- `settings.json` is now `formatVersion` 2. Version 1 files are migrated on load: their
  profiles become user profiles, because a version 1 file cannot say which entries were once
  shipped. Nothing is lost, but a program may briefly have two entries until the surplus one is
  removed
- Shipped profiles can be hidden but no longer deleted — they are embedded in the program file,
  so a deletion would only last until the next start

### Fixed
- The AltGr layer lit every key instead of only those carrying an AltGr character
- The number pad never showed digits, because Windows' scan-code mapping ignores Num Lock
- Shift now correctly suspends Num Lock in one direction only, so Shift plus the pad's arrows
  still selects text
- The lighting froze on the vendor effect when taking over, because frames sent during a
  hand-over are discarded while still being reported as successful
- Category colours were three shades of blue and indistinguishable in use

### Notes
- The Razer DeathStalker V2 profile is verified on hardware. One finding: the upper half of the
  ISO Enter key drives no LED on this model, so that key carries no matrix cell.
- Shortcut-set editing is not in the interface yet; the shipped sets cover the common Windows
  conventions and can be replaced per profile in the settings file.

[Unreleased]: https://github.com/Eistee82/Keylegend/commits/main
