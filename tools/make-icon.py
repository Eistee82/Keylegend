#!/usr/bin/env python3
"""Draws the application icon and writes src/Keylegend.App/keylegend.ico.

The icon is generated rather than checked in as an opaque binary that nobody can edit:
this file is the source, the .ico is its build product. Re-run it after changing anything
here.

    pip install pillow
    python tools/make-icon.py                 # writes the .ico
    python tools/make-icon.py --preview out   # plus one PNG per size, to look at

The motif is a single dark key cap with a coloured light spilling out from under it, and a
legend on its face: a large primary mark plus a small secondary one, the way a key carries
its base character and its AltGr character. That is the program in one picture — a key that
shows what it currently means. The three light colours are the ones the tray icon has always
been drawn in, so the program stays recognisable.

Small sizes are not downscaled from the large one: each size is drawn at its own geometry and
the 16 and 20 px frames drop the secondary legend mark, which at that scale would only turn
into a smudge.
"""

from __future__ import annotations

import argparse
import io
import struct
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

# What Windows asks for: Explorer's views, the title bar, Alt-Tab, the taskbar, and the
# notification area across the usual DPI scalings.
SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)

# The light under the cap. Blue, green and orange are the colours the tray icon was drawn
# in before there was a file; blurred into each other they read as keyboard backlight.
GLOW_STOPS = (
    (0.00, (0x3C, 0x7C, 0xFF)),
    (0.58, (0x00, 0xDC, 0x8C)),
    (1.00, (0xFF, 0x96, 0x00)),
)

CAP_SIDE = ((0x22, 0x22, 0x2C), (0x0C, 0x0C, 0x10))    # skirt, top to bottom
CAP_FACE = ((0x33, 0x33, 0x40), (0x1C, 0x1C, 0x24))    # top surface, lit from above
FACE_EDGE = (0x50, 0x50, 0x64)                          # highlight along the face's top edge
LEGEND_PRIMARY = (0xEE, 0xF0, 0xF8)
LEGEND_SECONDARY = (0xFF, 0xA2, 0x3A)                   # the AltGr mark


def _mix(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))  # type: ignore[return-value]


def _gradient(width: int, height: int, stops, vertical: bool) -> Image.Image:
    """A linear gradient through `stops`, given as (position 0..1, RGB) pairs."""
    steps = height if vertical else width
    strip = Image.new("RGB", (1, steps) if vertical else (steps, 1))
    pixels = strip.load()
    for i in range(steps):
        t = i / max(steps - 1, 1)
        lo = max((s for s in stops if s[0] <= t), key=lambda s: s[0], default=stops[0])
        hi = min((s for s in stops if s[0] >= t), key=lambda s: s[0], default=stops[-1])
        span = hi[0] - lo[0]
        colour = lo[1] if span <= 0 else _mix(lo[1], hi[1], (t - lo[0]) / span)
        if vertical:
            pixels[0, i] = colour
        else:
            pixels[i, 0] = colour
    return strip.resize((width, height), Image.Resampling.BICUBIC)


def _rounded_mask(canvas: int, box, radius: float) -> Image.Image:
    mask = Image.new("L", (canvas, canvas), 0)
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)
    return mask


def _vertical_ramp(canvas: int, top: float, bottom: float, start: int, end: int) -> Image.Image:
    """A top-to-bottom fade, used to keep the light where light would fall."""
    ramp = _gradient(canvas, canvas, ((0.0, (start,) * 3), (1.0, (end,) * 3)), vertical=True)
    ramp = ramp.convert("L")
    full = Image.new("L", (canvas, canvas), start)
    band = round(bottom - top)
    if band > 0:
        full.paste(ramp.resize((canvas, band), Image.Resampling.BILINEAR), (0, round(top)))
    full.paste(Image.new("L", (canvas, canvas - round(bottom)), end), (0, round(bottom)))
    return full


def render(size: int) -> Image.Image:
    """Draws one frame, supersampled and then reduced."""
    scale = 8 if size <= 64 else 4
    canvas = size * scale
    detail = size >= 32                     # below this, the secondary legend mark goes
    px = lambda f: f * canvas               # noqa: E731 - fraction of the canvas

    # Small frames carry less air around the cap, or the shape loses too many pixels.
    inset = 0.145 if detail else 0.105
    cap = (px(inset), px(inset + 0.015), px(1 - inset), px(1 - inset + 0.005))
    cap_w, cap_h = cap[2] - cap[0], cap[3] - cap[1]
    cap_r = cap_w * 0.21

    face = (
        cap[0] + cap_w * 0.135,
        cap[1] + cap_h * 0.105,
        cap[2] - cap_w * 0.135,
        cap[3] - cap_h * 0.255,
    )
    face_w, face_h = face[2] - face[0], face[3] - face[1]
    face_r = face_w * 0.17

    light = _gradient(canvas, canvas, GLOW_STOPS, vertical=False).convert("RGBA")
    image = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))

    # 1. The light itself: the cap's shape, spread and pushed downwards, so it looks like it
    #    escapes from under the cap rather than surrounding it evenly.
    spread = px(0.035)
    drop = px(0.045)
    glow = _rounded_mask(
        canvas,
        (cap[0] - spread, cap[1] - spread + drop, cap[2] + spread, cap[3] + spread + drop),
        cap_r + spread,
    )
    glow = glow.filter(ImageFilter.GaussianBlur(px(0.075)))
    glow = Image.composite(glow, Image.new("L", (canvas, canvas), 0),
                           _vertical_ramp(canvas, px(0.30), px(0.80), 70, 255))
    image.paste(light, mask=glow)

    # 2. The cap: a dark skirt with its top surface set into it.
    skirt = _gradient(canvas, canvas, ((0.0, CAP_SIDE[0]), (1.0, CAP_SIDE[1])), vertical=True)
    image.paste(skirt, mask=_rounded_mask(canvas, cap, cap_r))

    top = _gradient(canvas, canvas, ((0.0, CAP_FACE[0]), (1.0, CAP_FACE[1])), vertical=True)
    image.paste(top, mask=_rounded_mask(canvas, face, face_r))

    # 3. A hairline along the face's upper edge, which is what makes it read as raised.
    edge = Image.new("L", (canvas, canvas), 0)
    ImageDraw.Draw(edge).rounded_rectangle(face, radius=face_r, outline=255,
                                           width=max(1, round(px(0.014))))
    edge = edge.filter(ImageFilter.GaussianBlur(px(0.004)))
    edge = Image.composite(edge, Image.new("L", (canvas, canvas), 0),
                           _vertical_ramp(canvas, face[1], face[3], 255, 0))
    image.paste(Image.new("RGB", (canvas, canvas), FACE_EDGE), mask=edge)

    # 4. The bright rim where the light meets the cap's lower edge.
    rim = Image.new("L", (canvas, canvas), 0)
    ImageDraw.Draw(rim).rounded_rectangle(cap, radius=cap_r, outline=255,
                                          width=max(1, round(px(0.022))))
    rim = rim.filter(ImageFilter.GaussianBlur(px(0.006)))
    rim = Image.composite(rim, Image.new("L", (canvas, canvas), 0),
                          _vertical_ramp(canvas, px(0.42), px(0.86), 0, 255))
    image.paste(light, mask=rim)

    # 5. The legend. One mark below the other's left, as a key carries its base character
    #    and, smaller, the character its AltGr layer produces.
    def mark(x0, y0, x1, y1, colour):
        box = (face[0] + face_w * x0, face[1] + face_h * y0,
               face[0] + face_w * x1, face[1] + face_h * y1)
        radius = min(box[2] - box[0], box[3] - box[1]) * 0.3
        image.paste(Image.new("RGB", (canvas, canvas), colour),
                    mask=_rounded_mask(canvas, box, radius))

    if detail:
        mark(0.20, 0.34, 0.53, 0.80, LEGEND_PRIMARY)
        mark(0.62, 0.16, 0.83, 0.46, LEGEND_SECONDARY)
    else:
        # One big block instead of two small ones: at 16 px anything else is mud.
        mark(0.26, 0.24, 0.74, 0.78, LEGEND_PRIMARY)

    # Reduced through premultiplied alpha, otherwise the transparent edges pick up a dark fringe.
    return image.convert("RGBa").resize((size, size), Image.Resampling.LANCZOS).convert("RGBA")


def _dib(frame: Image.Image) -> bytes:
    """A 32-bit bottom-up DIB with the (unused, but expected) AND mask appended."""
    width, height = frame.size
    xor = frame.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes("raw", "BGRA")
    mask_stride = ((width + 31) // 32) * 4
    header = struct.pack(
        "<IiiHHIIiiII",
        40, width, height * 2, 1, 32, 0, len(xor), 0, 0, 0, 0,
    )
    return header + xor + b"\0" * (mask_stride * height)


def write_ico(path: Path, frames: list[Image.Image]) -> None:
    """Writes the frames into one .ico: DIBs below 256 px, PNG for 256, all 32-bit.

    PNG for the largest frame is what Windows itself does and keeps the file small; the shell
    and WPF read it. GDI+ (System.Drawing.Icon) does not, and hands out the 128 px frame
    instead when asked for 256 - which is why every size the program actually asks for is a
    plain DIB.
    """
    payloads = []
    for frame in frames:
        if frame.width >= 256:
            buffer = io.BytesIO()
            frame.save(buffer, "png")
            payloads.append(buffer.getvalue())
        else:
            payloads.append(_dib(frame))

    directory = struct.pack("<HHH", 0, 1, len(frames))
    offset = len(directory) + 16 * len(frames)
    entries = bytearray()
    for frame, payload in zip(frames, payloads):
        entries += struct.pack(
            "<BBBBHHII",
            frame.width if frame.width < 256 else 0,
            frame.height if frame.height < 256 else 0,
            0, 0, 1, 32, len(payload), offset,
        )
        offset += len(payload)

    path.write_bytes(bytes(directory) + bytes(entries) + b"".join(payloads))


def main() -> None:
    repository = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description="Generates the Keylegend application icon.")
    parser.add_argument("--output", type=Path,
                        default=repository / "src" / "Keylegend.App" / "keylegend.ico")
    parser.add_argument("--preview", type=Path, default=None,
                        help="directory to also write one PNG per size into")
    arguments = parser.parse_args()

    frames = [render(size) for size in SIZES]
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    write_ico(arguments.output, frames)
    print(f"{arguments.output} — {', '.join(str(s) for s in SIZES)} px")

    if arguments.preview:
        arguments.preview.mkdir(parents=True, exist_ok=True)
        for frame in frames:
            frame.save(arguments.preview / f"keylegend-{frame.width}.png")
        print(f"{arguments.preview} — previews")


if __name__ == "__main__":
    main()
