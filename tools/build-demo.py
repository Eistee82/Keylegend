#!/usr/bin/env python3
"""Assembles the frames from tools/record-demo.ps1 into the README animation.

    pip install pillow
    python tools/build-demo.py --frames out/frames

Written as an animated PNG. That choice was measured rather than assumed — the same ten frames
encoded four ways:

    GIF             352 KB   average error 1.96 / 255, worst pixel 73
    WebP q95        312 KB   average error 1.60,       worst pixel 55
    WebP lossless   648 KB   exact
    APNG           1051 KB   exact

APNG costs the most and is still the right answer, for two reasons that outweigh the megabyte:

- **It reproduces the interface, rather than approximating it.** GIF has 256 colours to spend on
  a window full of anti-aliased text over dark gradients, and spends them visibly at the edges.
  An illustration of a program that colours things ought to get the colours right.

- **It is a PNG.** GitHub's documented list of supported image types is PNG, GIF, JPEG and SVG —
  WebP is absent, so the smaller lossless option is not actually available here. And a browser
  too old for APNG does not fail: it renders the first frame as an ordinary still image. WebP
  offers no such fallback; it either animates or shows nothing at all.

Frames are deduplicated on the way in. Every encoder collapses identical consecutive frames
anyway, so holding a state for longer is a matter of duration, not of recording it twice.
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path

from PIL import Image

# Region of the 1400x900 capture worth keeping: below the title bar, above the donation row.
#
# The footer is cropped deliberately. It carries the PayPal and Ko-fi buttons, which are
# third-party marks excluded from this project's licence in NOTICE.md — a README image is
# exactly the sort of place they should not travel to.
CROP = (20, 50, 1385, 815)

# Wide enough for the key legends to stay readable on GitHub, narrow enough not to take over
# the page.
WIDTH = 900

# How long each state is held, in milliseconds. A layer has to be read; a return to the starting
# point only has to register.
DURATIONS = {
    "base": 1600,
    "shift": 1500,
    "altgr": 1700,
    "win": 1700,
    "ctrl": 1700,
    "locks": 1600,
    "app-notepad": 1900,
    "app-terminal": 1900,
    "app-explorer": 1900,
    "back": 1300,
}

DEFAULT_DURATION = 1600


def label_of(path: Path) -> str:
    """The state a frame shows, from its name: frame-09-app-notepad.png -> app-notepad."""
    match = re.match(r"frame-\d+-(.+)", path.stem)

    return match.group(1) if match else path.stem


def load(path: Path, width: int) -> Image.Image:
    frame = Image.open(path).convert("RGB").crop(CROP)
    height = round(frame.height * width / frame.width)

    return frame.resize((width, height), Image.Resampling.LANCZOS)


def main() -> None:
    repository = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", type=Path, default=repository / "out" / "frames")
    parser.add_argument("--output", type=Path, default=None,
                        help="defaults to docs/images/keylegend.<format>")
    parser.add_argument("--width", type=int, default=WIDTH)
    parser.add_argument("--format", choices=("apng", "gif"), default="apng",
                        help="gif is a fallback for surfaces that will not render APNG")
    arguments = parser.parse_args()

    paths = sorted(arguments.frames.glob("frame-*.png"))
    if not paths:
        raise SystemExit(f"No frames in {arguments.frames}. Run tools/record-demo.ps1 first.")

    # One entry per state, in the order they were recorded.
    unique: list[Path] = []
    seen: set[str] = set()
    for path in paths:
        label = label_of(path)
        if label not in seen:
            seen.add(label)
            unique.append(path)

    frames = [load(path, arguments.width) for path in unique]
    durations = [DURATIONS.get(label_of(path), DEFAULT_DURATION) for path in unique]

    suffix = "png" if arguments.format == "apng" else "gif"
    output = arguments.output or repository / "docs" / "images" / f"keylegend.{suffix}"
    output.parent.mkdir(parents=True, exist_ok=True)

    if arguments.format == "apng":
        frames[0].save(output, save_all=True, append_images=frames[1:],
                       duration=durations, loop=0, optimize=True)
    else:
        # One palette for the whole animation, not one per frame: a palette that shifts between
        # frames makes the unchanged background crawl, which reads as compression noise on what
        # is meant to look like a still interface.
        palette = frames[0].quantize(colors=256, method=Image.Quantize.MEDIANCUT)
        quantised = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
        quantised[0].save(output, save_all=True, append_images=quantised[1:],
                          duration=durations, loop=0, optimize=True, disposal=1)

    size = output.stat().st_size
    print(f"{output} — {len(frames)} states, {frames[0].width}x{frames[0].height}, "
          f"{size / 1024 / 1024:.2f} MB")

    if len(paths) != len(unique):
        print(f"  {len(paths) - len(unique)} duplicate frames folded into their durations")


if __name__ == "__main__":
    main()
