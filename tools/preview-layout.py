#!/usr/bin/env python3
"""Draws a device profile as an SVG, so a profile can be checked before there is hardware.

The application draws the same geometry, but you need a Chroma keyboard to see it. This does
not: it turns `device.json` into a picture you can open in any browser, which is enough to
catch the mistakes that actually happen — a key in the wrong row, a gap where a key should be,
an Enter that did not come out L-shaped.

    python tools/preview-layout.py devices/generic-fullsize-iso-de/device.json
    python tools/preview-layout.py --all --output out/       # every profile at once
    python tools/preview-layout.py <profile> --cells         # print matrix cells on the keys

With `--cells`, each key shows the matrix cell it claims instead of its legend. That is the
view to use while calibrating: light a cell on the keyboard, find it in the picture, and check
that the key lighting up is the key drawn there.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from xml.sax.saxutils import escape

# The preview's palette, kept close to the one the application uses so the two look related.
BOARD = "#0a0a0d"
CAP_TOP = "#2a2a30"
CAP_BOTTOM = "#1a1a1f"
CAP_EDGE = "#3a3a42"
UNMAPPED = "#c04a4a"      # a key with no matrix cell: nothing will ever light it
LEGEND = "#c8c8d4"
CELL_TEXT = "#7ad0a0"


def render(profile: dict, show_cells: bool) -> str:
    width = profile["canvas"]["width"]
    height = profile["canvas"]["height"]

    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" '
        f'width="{width * 2:.0f}" height="{height * 2:.0f}">',
        '<defs><linearGradient id="cap" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{CAP_TOP}"/>'
        f'<stop offset="1" stop-color="{CAP_BOTTOM}"/></linearGradient></defs>',
        f'<rect width="{width}" height="{height}" rx="6" fill="{BOARD}"/>',
    ]

    for key in profile["keys"]:
        mapped = key.get("row") is not None
        stroke = CAP_EDGE if mapped else UNMAPPED

        areas = [key] + list(key.get("parts") or [])
        for area in areas:
            parts.append(
                f'<rect x="{area["x"] + 0.6:.2f}" y="{area["y"] + 0.6:.2f}" '
                f'width="{area["width"] - 1.2:.2f}" height="{area["height"] - 1.2:.2f}" '
                f'rx="2.2" fill="url(#cap)" stroke="{stroke}" stroke-width="0.7"/>')

        if show_cells:
            text = f'{key["row"]},{key["column"]}' if mapped else "--"
            colour, size = CELL_TEXT, 5.0
        else:
            text = key.get("label")
            if text is None:
                # A key with no legend types a character; the application asks Windows which
                # one. The id's last segment is the closest stand-in a static picture has.
                text = key["id"].removeprefix("Keyboard_")
                text = text if len(text) <= 2 else ""
            colour, size = LEGEND, 5.5

        if text:
            parts.append(
                f'<text x="{key["x"] + key["width"] / 2:.2f}" '
                f'y="{key["y"] + key["height"] / 2 + size * 0.36:.2f}" '
                f'font-family="Segoe UI, sans-serif" font-size="{size}" fill="{colour}" '
                f'text-anchor="middle">{escape(text)}</text>')

        second = key.get("labelSecondary")
        if second and not show_cells:
            parts.append(
                f'<text x="{key["x"] + key["width"] / 2:.2f}" '
                f'y="{key["y"] + key["height"] - 3:.2f}" '
                f'font-family="Segoe UI, sans-serif" font-size="4" fill="{LEGEND}" '
                f'opacity="0.65" text-anchor="middle">{escape(second)}</text>')

    parts.append("</svg>")
    return "\n".join(parts)


def main() -> None:
    repository = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("profile", nargs="?", type=Path, help="path to a device.json")
    parser.add_argument("--all", action="store_true", help="render every profile under devices/")
    parser.add_argument("--output", type=Path, default=None,
                        help="directory to write into (default: beside the profile)")
    parser.add_argument("--cells", action="store_true",
                        help="label keys with their matrix cell instead of their legend")
    arguments = parser.parse_args()

    if arguments.all:
        paths = sorted((repository / "devices").glob("*/device.json"))
    elif arguments.profile:
        paths = [arguments.profile]
    else:
        parser.error("give a profile path, or --all")

    for path in paths:
        profile = json.loads(path.read_text(encoding="utf-8-sig"))
        suffix = "-cells" if arguments.cells else ""
        name = f"{path.parent.name}{suffix}.svg"
        target = (arguments.output / name) if arguments.output else path.with_name(f"preview{suffix}.svg")
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(render(profile, arguments.cells), encoding="utf-8")

        unmapped = [k["id"] for k in profile["keys"] if k.get("row") is None]
        note = f"  ({len(unmapped)} key(s) without a cell: {', '.join(unmapped)})" if unmapped else ""
        print(f"{target}{note}")


if __name__ == "__main__":
    main()
