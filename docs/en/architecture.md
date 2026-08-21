# Architecture

## The central idea

The entire decision logic is a **pure calculation** with no access to Windows, the network or
the file system:

```
(keyboard state, device profile, application profile, colour settings) → colour per key
```

Two things follow from this, and both are the reason the design is shaped this way:

1. The on-screen preview and the real keyboard are filled by **the same code**. What you see
   in the window is what lights up.
2. The logic is fully testable without a keyboard attached and without Synapse installed.

Everything that talks to the outside world sits in thin adapters around that core.

## Projects

| Project | Contains | May depend on |
|---|---|---|
| `Keylegend.Core` | device profiles, categories, shortcut sets, the frame composer, session state machine | nothing platform-specific |
| `Keylegend.Windows` | keyboard state, character resolution, foreground window | Windows APIs |
| `Keylegend.Chroma` | REST client for the Chroma SDK, heartbeat | network |
| `Keylegend.App` | WPF interface, tray icon, configuration storage | all of the above |

`Keylegend.Core` must never reference the others. If a change seems to require it, the
abstraction is in the wrong place.

## Reading the keyboard state

Keylegend does **not** install a global keyboard hook. Such a hook is functionally a
keylogger, sits in the input chain, and is regularly flagged by anti-cheat systems.

Instead the states of the keys of interest are polled (`GetAsyncKeyState` for held modifiers,
`GetKeyState` for lock states) about sixty times per second, and a new frame is composed only
when something changed. No keystroke is ever intercepted, forwarded, logged or stored.

### Left and right modifiers

Windows reports **AltGr as Ctrl plus right Alt**, and on German layouts Ctrl + left Alt
produces the same characters as AltGr. The two are told apart by side:

- **right Alt** → AltGr layer, showing the character assignment
- **Ctrl + left Alt** → the `Ctrl+Alt` shortcut set

Left and right variants must therefore be evaluated separately (`VK_LMENU`/`VK_RMENU` and so
on).

## Determining what a key means

Rather than shipping a table of layouts, Keylegend asks Windows what character a key would
produce in the current keyboard state (`ToUnicodeEx`), and derives the category from the
resulting character.

This is why Shift, Caps Lock and Num Lock need no special handling: the same key simply
returns `A` instead of `a` and lands in the "uppercase" category by itself. It is also why any
keyboard layout works without changes.

### Which keyboard is attached

The Chroma SDK cannot say. Its REST interface has no query endpoint — creating a session returns
an id and a URI, and a `GET` against that URI is answered with "Not Supported". The native DLL
offers `QueryDevice`, but that answers "is *this* GUID present?" one model at a time; the request
for a list of connected devices in the most active community wrapper has been open since 2016.

Windows answers it in one call. `ConnectedKeyboards` asks Raw Input for the attached devices and
keeps the USB vendor and product ids out of their names — `1532:0295` for a DeathStalker V2. A
device profile carrying a matching `usb` pair is then chosen outright.

Two things are worth being precise about. Raw Input is used here **only to enumerate devices**,
never to receive input from them: listing keyboards is not listening to them, and the promise
above holds unchanged. And a vendor uses one product id across layouts, so recognition narrows
the choice to a *model*; which ISO or ANSI variant of it applies is then taken from the active
Windows keyboard layout, as a hint and only to break that tie.

This matters more than it looks. While one profile shipped, picking "the first file found" was
the same thing as picking the right one. With thirty-two it was a 60 % layout, which left two
thirds of a full-size keyboard dark — a profile that does not mention a key cannot light it.

## Application profiles

A profile binds lighting rules to a program. Around ninety are shipped, and the decisions
behind them are worth stating, because each of them was the second answer rather than the
first.

### Profiles are data, not code

The same rule as device support: adding a profile is adding a JSON file under `profiles/`, and
the build picks it up by wildcard. Nobody has to touch C# to teach Keylegend a program, which
means a profile can be contributed, reviewed and corrected by somebody who only knows the
program. If supporting a new application ever needed code, the format would be wrong.

### Embedded in the assembly rather than loose on disk

Device profiles sit beside the executable; application profiles do not. Three reasons, and each
would be enough on its own. A single-file release carries them with no folder to lose. Nothing
on disk can be edited by accident, which is what makes "reset to shipped" mean anything at all
— the shipped version has to be out of reach to be a version worth resetting to. And a profile
that fails to build becomes a build error rather than a program that quietly has no profiles.

### Overrides are per section

A user's edit is never stored as a copy of the profile. It is stored as an override keyed on
the profile's id, holding only the sections that were touched. Two things follow: resetting is
possible at all, and an updated build can still improve a profile somebody has partly edited.
The id is load-bearing for this and must never change once shipped — renaming it orphans
somebody's edits.

The granularity was chosen against both obvious alternatives:

- **Per field** looks tidier and produces states nobody configured. Recolour `W`, then take an
  update that adds `Q`, and the result is a mixture the user never built and cannot explain.
- **Per profile** is the opposite failure. Rename one thing and the profile is frozen forever;
  it never sees another correction.

A section is the granularity at which the change still has a sentence: you edited the
highlights, so the highlights are yours now.

### A profile replaces only the layers it names

Shortcuts are keyed by modifier combination and laid over the general catalogue, not
substituted for it. Photoshop knows what `Ctrl` means inside Photoshop; it knows nothing about
`Win+E`, which Windows assigns system-wide and which is true no matter what is in front.
Replacing the whole catalogue would make a profile responsible for facts it has no opinion
about. A profile that names no layer returns the general catalogue unchanged, so the common
case allocates nothing.

### Shortcuts and highlights carry a label

The label says what the command does — "Duplicate layer", not "Ctrl+J". The hardware never
shows it: the LEDs carry colour and nothing else, so the label costs nothing at runtime. It
pays for itself three times elsewhere. The preview inside the application can show it, a test
can find contradictions between entries, and at ninety profiles it is the only way anyone can
review whether an entry is correct. `"j": "Edit"` cannot be checked against anything;
`"j": "Duplicate layer"` can.

### Migrating a format 1 settings file

Format 1 stored profiles whole, without an id and without any record of where a profile came
from. That is exactly what the new format fixes: an override needs an id to attach to, and
resetting needs to know that there is a shipped version to reset to.

The consequence for migration is that an old file cannot say which of its entries were once
shipped. So all of them become user profiles. That keeps every edit somebody made, at the price
of the shipped profile appearing next to the migrated copy until one of the two is removed —
which is the right trade, because the other reading would silently delete work.

## Talking to the keyboard

The Chroma SDK is addressed over its local REST interface. Colours are BGR-encoded integers;
the whole keyboard is written as a 6 × 22 matrix. A session must be kept alive with a
heartbeat.

Measured on the development machine: creating a session takes 60–125 ms, the first frame after
taking over from a running Chroma Studio effect about 500 ms, and every frame after that
around 2 ms.

### Every reply says 200, so the body decides

The service answers **everything** with HTTP 200, including requests it threw away. A frame with
the wrong matrix size comes back as:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

with a 200 status. Checking the status code alone therefore reports success for frames the
keyboard never showed — a silent failure, indistinguishable from the lighting simply not
changing.

So `result` in the body decides: zero is success, anything else is a rejection. Where the
service supplies an `error` in plain language it is kept as-is, because it names the actual
defect better than any wording invented here. The codes a user can act on are translated:

| Code | Meaning |
|---|---|
| 4309 | Chroma is switched off for this device in Synapse |
| 1152 | another application is holding the session |
| 1167 | no Chroma device is connected |
| 87 | the request was malformed |
| 50 | the request is not supported |

A successful session init carries no `result` at all — it returns the session details instead —
so its absence counts as success.

### How often frames are sent

This looks like a detail and is not; both obvious answers are wrong, and each was tried.

**Sending only on change** starves the hand-over. An ordinary keypress does not change the
keyboard state — only modifiers and locks do — so a take-over produced exactly one frame.
Chroma discards frames while it is still taking control, and reports success for them, so that
single frame could vanish and leave the keyboard frozen on the previous effect until the user
happened to press a modifier.

**Sending as fast as possible** ruins responsiveness. Frames queue inside the interface, and a
state change then waits behind everything already sent — pressing Shift took a visible second
or two to show.

What works is sending for three distinct reasons at three different rates:

| Reason | Rate |
|---|---|
| The keyboard state changed | immediately — measured at 1 ms end to end |
| Within three seconds of a take-over | every 120 ms, until the hand-over settles |
| Otherwise | every 750 ms, purely as insurance against a lost frame |

## Session handling

| State | Behaviour |
|---|---|
| **Idle** | No session. Chroma Studio drives the lighting. Only the cheap activity poll runs. |
| **Active** | Session open, heartbeat running, a new frame on every state change. |
| **Paused** | Lighting released until resumed. |

Keylegend takes over on the first keypress and releases the keyboard after a configurable idle
period, so your own Chroma Studio effect returns. The ~500 ms wake-up cost is therefore paid
only after a real pause, never while typing.
