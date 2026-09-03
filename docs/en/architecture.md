# Architecture

## The central idea

The entire decision logic is a **pure calculation** with no access to Windows, the network or
the file system:

```
(keyboard state, attached keyboard, application profile, colour settings) → colour per key
```

Two things follow from this, and both are the reason the design is shaped this way:

1. The on-screen preview and the real keyboard are filled by **the same code**. What you see
   in the window is what lights up.
2. The logic is fully testable without a keyboard attached and without Synapse installed.

Everything that talks to the outside world sits in thin adapters around that core.

## Projects

| Project | Contains | May depend on |
|---|---|---|
| `Keylegend.Core` | the attached keyboard, categories, shortcut sets, the frame composer, session state machine | nothing platform-specific |
| `Keylegend.Windows` | keyboard state, character resolution, foreground window | Windows APIs |
| `Keylegend.Chroma` | REST client for the Chroma SDK, heartbeat | network |
| `Keylegend.Engine` | the loop that reads the keyboard, composes a frame and sends it | Core, Chroma, Windows |
| `Keylegend.App` | WPF interface, tray icon, configuration storage | all of the above |

`Keylegend.Core` must never reference the others. If a change seems to require it, the
abstraction is in the wrong place.

## Reading the keyboard state

Keylegend does **not** install a global keyboard hook. Such a hook is functionally a
keylogger, sits in the input chain, and is regularly flagged by anti-cheat systems.

Instead the states of the keys of interest are polled (`GetAsyncKeyState` for held modifiers,
`GetKeyState` for lock states) about sixty times per second, and a new frame is composed only
when something changed. No keystroke is ever intercepted, forwarded, logged or stored.

With a typing effect chosen, the same poll is carried through to the keys the attached board reports
rather than stopping at the modifiers. It is the same question asked of more keys — is this one down
at this moment — and it is asked only while an effect is chosen; with none, the individual keys are
never looked at. What is kept is small and does not last: `KeyActivity` holds when each key went
down and came up, and forgets a key nothing has touched for a few seconds. The one exception is the
heat effect, which keeps a decaying number per key for as long as it takes to cool — a trace of the
typing in memory, written nowhere and gone with the process.

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

Razer Synapse is asked, because it already knows. It writes a description of every attached device
into `…\Razer Chroma SDK\Devices\<guid>.json`: the model by name, the physical layout as a number,
the matrix size, and the scan code of every key the hardware actually has. `SdkDeviceDescription`
reads that. Nothing about the keyboard is inferred — not the model, not the layout, not
which keys exist.

That description is written when Razer's software comes up and does not exist before then, which at
logon is a race Keylegend can lose: on the machine this was developed on, the file appeared
ninety-five seconds after the system started, and Keylegend's own startup entry fired eight seconds
later. So looking for it is not one attempt whose failure ends the program. `AttachedKeyboardSearch`
keeps looking — briskly while no device is named, backing off while only the drawing is missing —
the notification-area icon is created before the first look, and the engine is built the moment a
keyboard turns up.

The Chroma SDK's own interfaces cannot answer this. Its REST interface has no query endpoint —
creating a session returns an id and a URI, and a `GET` against that URI is answered with "Not
Supported". The native DLL offers `QueryDevice`, but that answers "is *this* GUID present?" one
model at a time; the request for a list of connected devices in the most active community wrapper
has been open since 2016.

What the keyboard *looks like* comes from the same installation. Synapse's interface is a web
application, and the drawings it loads for a device stay in its cache: key rectangles with names,
the shape of the casing including the volume dial and the media strip, and the outlines of the
characters printed on the caps. `SvgLayoutSource` finds the one for the attached model and physical
layout — exactly, not by shape, because each drawing is delivered beside a configuration object
naming both, and the layout id there is the same number the service reports.

Only measurements and outlines are taken. Razer's colours and styling are ignored, and none of the
artwork is copied into this repository — it is read at run time from the installation that already
holds it.

The one thing neither the description nor the drawing states is which cell of the lighting matrix a
key belongs to. That is `StandardKeyMatrix`, the protocol's own `RZKEY` table, identical on every
model — which is why Synapse needs no per-model table for it either.

**So nothing describing a keyboard is shipped at all.** There is no folder of such files, nothing
to write for a new keyboard, and no list of supported models. The one keyboard measured by hand is
kept as test data, and `FromDrawingTests` checks the whole assembly against it: same keys, and
every key on the cell that was measured at the hardware.

## Application profiles

A profile binds lighting rules to a program. Around ninety are shipped, and the decisions behind
them are worth stating, because none of them is the obvious answer.

### Profiles are data, not code

The same rule as device support: adding a profile is adding a JSON file under `profiles/`, and
the build picks it up by wildcard. Nobody has to touch C# to teach Keylegend a program, which
means a profile can be contributed, reviewed and corrected by somebody who only knows the
program. If supporting a new application ever needed code, the format would be wrong.

### Embedded in the assembly rather than loose on disk

Application profiles are compiled into the assembly rather than left as files beside the
executable. Three reasons, and each would be enough on its own. A single-file release carries them
with no folder to lose. Nothing on disk can be edited by accident, which is what makes "reset to
shipped" mean anything at all — the shipped version has to be out of reach to be a version worth
resetting to. And a profile that fails to build becomes a build error rather than a program that
quietly has no profiles.

### Overrides are per section

A user's edit is never stored as a copy of the profile. It is stored as an override keyed on
the profile's id, holding only the sections that were touched. Two things follow: resetting is
possible at all, and an updated build can still improve a profile somebody has partly edited.
The id is load-bearing for this and must never change once shipped — renaming it orphans
somebody's edits.

The granularity holds against both obvious alternatives:

- **Per field** looks tidier and produces states nobody configured. Recolour `W`, then take an
  update that adds `Q`, and the result is a mixture the user never built and cannot explain.
- **Per profile** is the opposite failure. Rename one thing and the profile is frozen forever;
  it never sees another correction.

A section is the granularity at which the change still has a sentence: you edited the
highlights, so the highlights are yours now.

### A profile is laid over the general set, entry by entry

Shortcuts are keyed by modifier combination, and a profile's entries go over the general ones
rather than in place of them — per entry, not per layer. Photoshop knows what `Ctrl+J` means
inside Photoshop; it knows nothing about `Win+E`, which Windows assigns system-wide, and nothing
about `Ctrl+C`, which holds anywhere there is a caret.

Per layer would mean that a profile naming `Ctrl` for its own commands takes the whole layer with
it, and the clipboard is what that costs: copy, paste, cut, undo and select-all go dark in a
browser, in a chat client, in a terminal — programs one does little in but type and paste. Per
entry, naming a key wins for that key and nothing else moves. There is deliberately no way to
blank a layer wholesale.

A profile that names no layer returns the general catalogue unchanged, so the common case
allocates nothing.

### Shortcuts and highlights carry a label

The label says what the command does — "Duplicate layer", not "Ctrl+J". The hardware never
shows it: the LEDs carry colour and nothing else, so the label costs nothing at runtime. It
pays for itself three times elsewhere. The preview inside the application can show it, a test
can find contradictions between entries, and at ninety profiles it is the only way anyone can
review whether an entry is correct. `"j": "Edit"` cannot be checked against anything;
`"j": "Duplicate layer"` can.

### Migrating a format 1 settings file

A format 1 file stores profiles whole, without an id and without any record of where a profile
came from. An override needs an id to attach to, and resetting needs to know that a shipped
version exists to reset to, so such a file cannot say which of its entries are shipped ones.

All of them therefore become user profiles. That keeps every edit somebody made, at the price of
the shipped profile appearing next to the migrated copy until one of the two is removed — the
right trade, because the other reading silently deletes work.

### Migrating a format 2 settings file

A format 2 file lists every colour, the untouched ones included, so it cannot say which of its
entries are decisions and which are defaults echoed back. Honouring all of them pins the palette:
an improved shipped colour then reaches nobody who has ever run the program.

Format 3 writes only what differs from the shipped palette, so an entry in the file means somebody
chose it. Migrating an older file has to guess at that distinction, and the guess is: an entry equal
to the palette of that format's day is a default, anything else is a choice. `PaletteBeforeFormat3`
holds that palette as a frozen copy rather than reading the current one — that comparison is
meaningless the moment the palette changes again, which is exactly when it is needed.

The price is that somebody who deliberately picked one of those colours loses it. That is the right
way round: one person re-picks a colour, against every user keeping a palette nobody chose.

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
keyboard does not show — a silent failure, indistinguishable from the lighting simply not
changing.

So `result` in the body decides: zero is success, anything else is a rejection. Where the
service supplies an `error` in plain language it is kept as-is, because it names the actual
defect better than any wording invented here. The codes a user can act on are translated:

| Code | Meaning |
|---|---|
| 4309 | Chroma is switched off for this device in Synapse |
| 1152 | another application is holding the session |
| 1167 | no Chroma device is connected |
| 5 | access was denied |
| 87 | the request was malformed |
| 50 | the request is not supported |

A successful session init carries no `result` at all — it returns the session details instead —
so its absence counts as success.

### How often frames are sent

This looks like a detail and is not: both obvious answers are wrong.

**Sending only on change** starves the hand-over. An ordinary keypress does not change the
keyboard state — only modifiers and locks do — so a take-over is one single frame. Chroma discards
frames while it is still taking control and reports success for them, so that frame can vanish and
leave the keyboard on the previous effect until the user happens to press a modifier.

**Sending as fast as possible** ruins responsiveness. Frames queue inside the interface, and a
state change then waits behind everything already sent — pressing Shift takes a visible second or
two to show.

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

Only one copy of Keylegend drives the keyboard. Two would open two sessions for the same device;
the service gives it to one of them, and the other lights nothing while still reporting success —
which looks exactly like a program that has quietly stopped working. What a second start does
depends on what is already running. The same program from the same place means somebody
double-clicked the icon while it sat in the notification area: its window comes up and the second
start bows out, so nothing is killed and the lighting does not blink. Anything else — an older
version, or the same one from another folder — is superseded: it is asked to quit, hands its
session back, and is ended outright only if it does not answer within two seconds.
