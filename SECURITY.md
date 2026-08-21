# Security

## What Keylegend does with your keystrokes

Nothing. That is the whole answer, and it is worth stating plainly because a program that colours
keys by what they type sounds like it would have to read what you type.

It does not install a global keyboard hook. Such a hook is functionally a keylogger, sits in the
input chain, and is regularly flagged by anti-cheat systems. Instead the program polls the
*states* of modifier and lock keys — `GetAsyncKeyState` for held modifiers, `GetKeyState` for Num,
Caps and Scroll Lock — and asks Windows which character a given key *would* produce
(`ToUnicodeEx`). No keystroke is intercepted, forwarded, logged or stored.

This is checked, not just promised: `tests/Keylegend.Core.Tests/ArchitectureTests.cs` fails the
build if `Keylegend.Core` grows a dependency it should not have, and there is no hook installation
anywhere in `Keylegend.Windows`. The relevant Win32 imports are collected in one file,
`NativeMethods.cs`, so the claim can be verified by reading a single page.

Raw Input is used in one place, `ConnectedKeyboards`, and only to ask Windows **which** keyboards
are attached — reading device names to find a matching device profile. Listing devices is not
listening to them.

## What it sends over the network

Nothing leaves the machine. The only connection is to `http://localhost:54235`, the Razer Chroma
SDK's local interface. There is no telemetry, no update check and no analytics.

## What it writes

- `%APPDATA%\Keylegend\settings.json` — your colours, profiles and settings
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` — only while autostart is switched on
- `calibration-findings.txt` next to the executable, and only while calibrating

## Reporting something

Open an issue at <https://github.com/Eistee82/Keylegend/issues>. If you would rather not discuss
it publicly, say so in an issue without the details and a private channel will be arranged.

This is a hobby project maintained in spare time. There is no guaranteed response window, and no
bounty.

## Unsigned releases

Release artefacts are not code-signed, so Windows SmartScreen reports an unknown publisher. A
certificate costs a few hundred euros a year, which this project does not have. Every release
carries `SHA256SUMS.txt` so a download can be checked against what the build produced:

```powershell
Get-FileHash .\Keylegend-1.0.0-setup.exe -Algorithm SHA256
```

Builds are produced by GitHub Actions from the tagged commit — the workflow is
`.github/workflows/release.yml`, and its log is public.
