# Notices

Keylegend itself is released under the [MIT licence](LICENSE). This file lists the few things
inside the repository that the MIT licence does **not** cover, and the third-party names it
refers to.

## Files not covered by the MIT licence

| File | Owner | What you may do with it |
|---|---|---|
| `src/Keylegend.App/Assets/paypal-donate.gif` | PayPal | PayPal publishes this button so that people can link to a PayPal payment page. It is used here for exactly that. It is **not** licensed under MIT: if you fork Keylegend, replace it or obtain the button from PayPal yourself, and point it at your own payment page. |
| `src/Keylegend.App/Assets/kofi-button.png` | Ko-fi | Ko-fi publishes this button so that people can link to a Ko-fi page. Same terms as above. |

Everything else in this repository — all source code, all application profiles, all documentation,
the application icon and the scripts under `tools/` — is original work released under the MIT
licence.

## Dependencies

Keylegend has **no runtime dependencies**. It talks to the Razer Chroma SDK over its local REST
interface and needs no vendor library, header or redistributable.

Build and test time only, never shipped:

| Component | Licence | Used for |
|---|---|---|
| xunit | Apache-2.0 | test framework |
| xunit.runner.visualstudio | Apache-2.0 | test runner |
| Microsoft.NET.Test.Sdk | MIT | test host |
| coverlet.collector | MIT | coverage |
| Pillow | MIT-CMU (HPND) | `tools/make-icon.py` draws the application icon |
| actions/checkout, actions/setup-dotnet, actions/upload-artifact | MIT | continuous integration |

## Why there is no Chroma library here

Keylegend speaks to the Chroma SDK over its local REST interface, in about two hundred lines of
`ChromaClient.cs`, rather than binding to `RzChromaSDK64.dll` or taking a dependency on one of
the community wrappers. That is a deliberate choice and worth recording, because "why not just
use Colore?" is a reasonable question to ask.

**The vendor DLL.** Razer's End User Licence Agreement grants a "limited, revocable,
non-exclusive, non-transferable and non-sublicensable license to use the Software for your
personal non-commercial use", and forbids copying or reproducing it. No separate developer
licence for the SDK is published — not on the developer portal, not in the SDK documentation,
not as a file installed alongside it. Calling a local HTTP interface that this project
implements itself is a clean interoperability case, expressly protected in the EU (Article 6 of
Directive 2009/24/EC). Linking against a proprietary binary for which nobody has granted a
developer right is a step away from that safety — for no functional gain.

**The community wrappers.** [Colore](https://github.com/chroma-sdk/Colore) is MIT-licensed,
maintained, and does exactly what this project does: ships no Razer files, carries a trademark
notice, expects the SDK to arrive with Synapse. It is a fine library. It simply has nothing this
project needs. Most of its siblings are worse propositions: `chroma-core` and `chroma-python`
carry **no licence at all**, which reserves every right to their authors, and the majority of
that organisation has been untouched since 2017 or 2018.

**Device detection.** Neither route answers "which keyboard is attached?". The REST interface
has no query endpoint — a session returns an id and a URI, and a GET against it is refused with
"Not Supported". The native SDK offers `QueryDevice`, which answers "is *this* GUID present?"
one model at a time; the Colore request for a list of connected devices
([issue #145](https://github.com/chroma-sdk/Colore/issues/145)) has been open since January
2016. Synapse answers it outside the SDK altogether: it writes a description of every attached
device into `…\Razer Chroma SDK\Devices\<guid>.json`, which is what `SdkDeviceDescription` reads.

## Trademarks

Keylegend is an independent project. It is **not affiliated with, endorsed by, or sponsored by**
any of the companies named below.

**Razer.** RAZER and RAZER CHROMA are trademarks or registered trademarks of Razer Inc. They are
used here solely to identify the hardware and the software interface Keylegend works with. No Razer
code, header, library or artwork is contained in this repository. The Chroma key matrix the program
addresses is reached through publicly documented `0xRRCC` cell coordinates — factual information
required for interoperability, not copied material.

**Application and game names.** The profiles under `profiles/` name around ninety programs and
games — Photoshop, Visual Studio Code, Excel, Elden Ring and the rest. Those names are
trademarks of their respective owners and appear only to identify which program a profile is
for, and to describe what that program's own keyboard shortcuts do. Keylegend is not associated
with any of them, and contains none of their code or assets.

**Keyboard vendors.** Razer, Chroma, Synapse and the model names appear only to say which
keyboard is being described and which software Keylegend reads it from. Keylegend is not
associated with Razer and contains none of its code.

## The vendor's keyboard drawing

Keylegend draws the attached keyboard from the drawing Razer's own software keeps for that model:
the key rectangles, the shape of the casing, and the outlines of the characters printed on the
caps. That drawing is Razer's artwork, and **none of it is contained in this repository or in any
release**. It is read at run time from the local Razer Synapse installation — the same files that
installation already holds for its own use — and held in memory for as long as the program runs.

Only measurements and outlines are taken from it. Razer's colours and styling are ignored: the
greys, the glow and the type in this program are its own, so a keyboard on screen looks like the
rest of the application rather than like somebody else's software.

Which cell of the lighting matrix each key belongs to is not taken from the drawing at all. It
comes from the `RZKEY` enumeration in Razer's public Chroma SDK documentation, which is a
specification of the protocol rather than a creative work.
