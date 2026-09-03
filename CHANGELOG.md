# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Nothing yet.

## [1.2.0] — 2026-09-03

### Added
- **The lighting can answer the typing.** A new setting, *Effect while typing*, with eight to
  choose from and *none* as the default: **Fade** (the struck key goes dark and comes back over a
  second), **Flash**, **Afterglow**, **Impact** (the stroke shakes the keys around it), **Water
  drop**, **Dark wave**, **Sparks** and **Heat** (keys warm as they are used and cool down again).
  One at a time. Each is a curve over the time since a press or a release, laid over the finished
  frame rather than mixed into the decision about what a key means — so the colours still say what
  they said, and the keyboard in the window shows the same thing as the one on the desk. An effect
  that brightens a key does it by mixing white in, up to white at full brightness: the shipped
  palette already runs a channel at 255 on every colour, so there is no brighter blue to go to.
  The waves are given the distance from one corner of the board to the other to travel, so a drop
  sweeps the whole keyboard whatever keyboard it is
- Choosing an effect is also what makes Keylegend look at the individual keys at all. With *none*
  chosen — the default — it asks only whether anybody is typing, exactly as before. Still no
  keyboard hook: it reads whether a key is down at this moment, and never intercepts, forwards or
  records a keystroke. The heat effect is the one that keeps anything, and keeps it in memory
  only: a decaying number per key for as long as it takes to cool

### Fixed
- **Two profile colours read as a tinted white and have been replaced.** The colour for abilities
  in fourteen games and the one for building in eight sat at 0.61 saturation, and both were used
  beside the grey that marks menu keys — on a lit keycap that pair reads as pink next to white
  rather than as two meanings. They are now a saturated violet and a saturated green. The same
  pass took the last two dim colours out of the application profiles, so every colour a profile
  can show is fully saturated and drives a channel to 255, with the one grey left to mean grey.
  Two tests hold the rule, which the existing check could not: it compared a highlight with the
  colour the key carries anyway, never with the other highlights beside it
- **The keyboard in the window keeps up with the one on the desk.** Turning a layer on changed the
  hardware at once and the picture about a third of a second later, which read as the window
  lagging behind the keyboard — and it was. The printed legends arrive from Razer as one path for
  the whole board, and every key was handed that whole path under a clip so it could paint its own
  characters in its own colour. A clip saves the renderer nothing: the path is rasterised in full
  and the result thrown away outside it, so one frame meant a hundred and five passes over tens of
  thousands of segments, and twice that for lit keys, whose glow is a second pass with a wide pen.
  None of it showed in the program's own timings, because that cost falls on the rendering thread
  after `OnRender` has recorded the instructions and gone home. The path is now cut into one shape
  per key once, when the drawing is read. Measured: 300 ms to 33 ms, with the first picture after a
  start arriving sooner as well, and the same pixels on screen
- **Keylegend now starts before Razer Synapse does.** At logon the two come up together, and
  Synapse writes its description of the attached keyboard only once it is ready — measured here,
  ninety-five seconds after the system started, with Keylegend's own startup entry firing eight
  seconds later. Keylegend used to look once, and a look that came up empty ended it: an error
  message that nobody could see behind everything else on a fresh desktop, or a process sitting
  there with nothing in the notification area. It waits now. The icon appears before the first
  look and stays through the whole wait, the search repeats — every two seconds while no keyboard
  is named, backing off to half a minute while only the drawing is missing — and the lighting
  starts by itself the moment there is a keyboard to light. A start from the startup list opens no
  window for this; a start by hand shows a small one saying what is missing and when it last
  tried, and closing it changes nothing

## [1.1.0] — 2026-08-26

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
- **Keylegend no longer runs twice.** Two copies open two Chroma sessions for the same keyboard,
  the service gives it to one of them, and the other lights nothing while still reporting success
  — which looks exactly like a program that has quietly stopped working. What a second start does
  now depends on what is already running:
  - **The same program from the same place** — you double-clicked the icon while it sat in the
    notification area. Its window comes up and the second start bows out. Nothing is killed, the
    session does not change hands, and the lighting does not blink
  - **Anything else** — an older version, or the same one from another folder — is superseded by
    the newer start. It is asked to quit first, so it hands its session back and the keyboard
    returns to its own effect rather than freezing on the last frame; one that does not answer
    within two seconds is ended outright
- **A failed start now says which of three things is missing**, instead of always blaming the
  keyboard. No device description means Synapse is not running or nothing is plugged in; a device
  Synapse knows but has no drawing for means opening Synapse once, with the keyboard connected, so
  it fetches one; a drawing that cannot be read means the format moved and is worth reporting. The
  second case used to be told to connect a keyboard that was already connected
- **Closing the window no longer pops up a balloon** saying the program is still running. The
  icon in the notification area says that already, and the balloon said it again on every single
  close. The one balloon left is the one that reports a fault
- **Synapse now has to know your keyboard, not merely be running.** It was already required in
  1.0.0, for the Chroma service that does the lighting; what changed is what else it is asked for.
  The keyboard is now described by Synapse instead of by a file shipped here, so it has to be
  connected and Synapse has to hold the drawing of that model — which it downloads the first time
  one is attached. Where 1.0.0 fell back on a generated layout, this says what is missing and
  stops, because a guessed layout lights the wrong keys without ever admitting it
- Colours in the palette are saturated now. A pale one is a tinted white on a lit keycap, and it was
  indistinguishable from the keys next to it — measured on the hardware, not in the preview
- The lit legends keep their hue and glow at any window size
- **The nineteen tests that need Razer's own files now report as skipped**, with the reason, instead
  of passing without having checked anything. A green run on a machine without Synapse used to
  report every test as passed while having looked at no drawing at all; it now says `19 skipped`
  and names them. This is what the test project is on xUnit v3 for — `Assert.Skip` decides at run
  time, which xUnit 2 cannot do
- Internal naming: what describes the plugged-in keyboard is called `AttachedKeyboard` rather than
  `DeviceProfile`. It is not a file and never was one, and "profile" already means the application
  profiles elsewhere in the program

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
