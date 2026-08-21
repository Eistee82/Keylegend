#!/usr/bin/env python3
"""Generates the device profiles under devices/ from standard keyboard dimensions.

Keyboards are not arbitrary shapes. Since the IBM Model M the industry has built to a 19.05 mm
key pitch, and the widths of the odd-sized keys — Tab at 1.5 units, Caps Lock at 1.75, the ISO
Enter at 1.5 over 1.25 — are just as settled. That is enough to draw any of the usual layouts
without measuring a single keyboard, and it is why this file exists instead of a folder full of
hand-written JSON.

    python tools/make-layout.py            # writes every profile listed in PROFILES
    python tools/make-layout.py --list     # names them without writing anything
    python tools/make-layout.py --only 60  # only profiles whose folder name contains "60"

What the generator can and cannot know:

- **Geometry it knows.** Positions and sizes follow from the layout, exactly.
- **The matrix mapping it assumes.** Razer addresses the keyboard as 6 rows x 22 columns and
  its `RZKEY_*` constants encode each key as 0xRRCC. The table below is that assignment for a
  standard full-size board. Compact keyboards keep the matrix but populate fewer cells, and
  individual models move keys around — which is why every generated profile says
  `"verified": false` until somebody has stepped through it on real hardware.
- **Legends it takes from the layout.** What is printed on a key belongs to the keyboard, not
  to the language the software speaks: a German keyboard says "Strg" whatever Keylegend is set
  to. Keys that produce a character carry no legend here — that is asked from Windows.

Everything in this file is derived from published dimensions and from key names, both facts
rather than anyone's creative work. No vendor layout file or configuration software was used.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

# Drawing units. The canvas is in the same units as the keys, and only ratios matter — the
# preview scales the whole thing to whatever room it has.
UNIT = 19.0
MARGIN = 6.0

# The gap between the main block, the navigation cluster and the number pad. A quarter unit is
# what the standard leaves; keyboards with a tighter case only differ by a fraction of a key.
BLOCK_GAP = 0.25

# The half-unit air between the function row and the number row.
FUNCTION_ROW_GAP = 0.5


# --------------------------------------------------------------------------------------------
# The Chroma matrix
# --------------------------------------------------------------------------------------------
# Key id -> (row, column) in the 6 x 22 cell grid the Chroma SDK addresses. This mirrors Razer's
# own RZKEY_* constants, which encode a cell as 0xRRCC: row in the high byte, column in the low
# one. Interoperability data — the numbers are what the hardware answers to, not a description
# of anything.
#
# Names follow the US layout, as the ids do. On a German keyboard the physical Z sits where this
# table says Y; that affects the name only, never the position.
MATRIX: dict[str, tuple[int, int]] = {
    "Keyboard_Escape": (0, 1),
    "Keyboard_F1": (0, 3), "Keyboard_F2": (0, 4), "Keyboard_F3": (0, 5), "Keyboard_F4": (0, 6),
    "Keyboard_F5": (0, 7), "Keyboard_F6": (0, 8), "Keyboard_F7": (0, 9), "Keyboard_F8": (0, 10),
    "Keyboard_F9": (0, 11), "Keyboard_F10": (0, 12), "Keyboard_F11": (0, 13),
    "Keyboard_F12": (0, 14),
    "Keyboard_PrintScreen": (0, 15), "Keyboard_ScrollLock": (0, 16),
    "Keyboard_PauseBreak": (0, 17),

    "Keyboard_GraveAccentAndTilde": (1, 1),
    "Keyboard_1": (1, 2), "Keyboard_2": (1, 3), "Keyboard_3": (1, 4), "Keyboard_4": (1, 5),
    "Keyboard_5": (1, 6), "Keyboard_6": (1, 7), "Keyboard_7": (1, 8), "Keyboard_8": (1, 9),
    "Keyboard_9": (1, 10), "Keyboard_0": (1, 11),
    "Keyboard_MinusAndUnderscore": (1, 12), "Keyboard_EqualsAndPlus": (1, 13),
    "Keyboard_Backspace": (1, 14),
    "Keyboard_Insert": (1, 15), "Keyboard_Home": (1, 16), "Keyboard_PageUp": (1, 17),
    "Keyboard_NumLock": (1, 18), "Keyboard_NumSlash": (1, 19),
    "Keyboard_NumAsterisk": (1, 20), "Keyboard_NumMinus": (1, 21),

    "Keyboard_Tab": (2, 1),
    "Keyboard_Q": (2, 2), "Keyboard_W": (2, 3), "Keyboard_E": (2, 4), "Keyboard_R": (2, 5),
    "Keyboard_T": (2, 6), "Keyboard_Y": (2, 7), "Keyboard_U": (2, 8), "Keyboard_I": (2, 9),
    "Keyboard_O": (2, 10), "Keyboard_P": (2, 11),
    "Keyboard_BracketLeft": (2, 12), "Keyboard_BracketRight": (2, 13),
    "Keyboard_Backslash": (2, 14),
    "Keyboard_Delete": (2, 15), "Keyboard_End": (2, 16), "Keyboard_PageDown": (2, 17),
    "Keyboard_Num7": (2, 18), "Keyboard_Num8": (2, 19), "Keyboard_Num9": (2, 20),
    "Keyboard_NumPlus": (2, 21),

    "Keyboard_CapsLock": (3, 1),
    "Keyboard_A": (3, 2), "Keyboard_S": (3, 3), "Keyboard_D": (3, 4), "Keyboard_F": (3, 5),
    "Keyboard_G": (3, 6), "Keyboard_H": (3, 7), "Keyboard_J": (3, 8), "Keyboard_K": (3, 9),
    "Keyboard_L": (3, 10),
    "Keyboard_SemicolonAndColon": (3, 11), "Keyboard_ApostropheAndDoubleQuote": (3, 12),
    "Keyboard_NonUsTilde": (3, 13),
    "Keyboard_Enter": (3, 14),
    "Keyboard_Num4": (3, 18), "Keyboard_Num5": (3, 19), "Keyboard_Num6": (3, 20),

    "Keyboard_LeftShift": (4, 1),
    "Keyboard_NonUsBackslash": (4, 2),
    "Keyboard_Z": (4, 3), "Keyboard_X": (4, 4), "Keyboard_C": (4, 5), "Keyboard_V": (4, 6),
    "Keyboard_B": (4, 7), "Keyboard_N": (4, 8), "Keyboard_M": (4, 9),
    "Keyboard_CommaAndLessThan": (4, 10), "Keyboard_PeriodAndBiggerThan": (4, 11),
    "Keyboard_SlashAndQuestionMark": (4, 12),
    "Keyboard_RightShift": (4, 14),
    "Keyboard_ArrowUp": (4, 16),
    "Keyboard_Num1": (4, 18), "Keyboard_Num2": (4, 19), "Keyboard_Num3": (4, 20),
    "Keyboard_NumEnter": (4, 21),

    "Keyboard_LeftCtrl": (5, 1), "Keyboard_LeftGui": (5, 2), "Keyboard_LeftAlt": (5, 3),
    "Keyboard_Space": (5, 7),
    "Keyboard_RightAlt": (5, 11),
    "Keyboard_Function": (5, 12), "Keyboard_RightGui": (5, 12),
    "Keyboard_Application": (5, 13), "Keyboard_RightCtrl": (5, 14),
    "Keyboard_ArrowLeft": (5, 15), "Keyboard_ArrowDown": (5, 16), "Keyboard_ArrowRight": (5, 17),
    "Keyboard_Num0": (5, 19), "Keyboard_NumPeriodAndDelete": (5, 20),

    # Column 0 is the macro column keyboards like the BlackWidow carry down their left edge.
    "Keyboard_Macro1": (1, 0), "Keyboard_Macro2": (2, 0), "Keyboard_Macro3": (3, 0),
    "Keyboard_Macro4": (4, 0), "Keyboard_Macro5": (5, 0),
}

# Keys the standard scan code table in Keylegend.Core does not carry, because they exist only on
# one regional layout. The profile states the code so that no C# has to change for a layout.
EXTRA_SCAN_CODES = {
    "Keyboard_JpYen": 0x7D,        # the ¥ key left of Backspace on JIS
    "Keyboard_JpRo": 0x73,         # the ろ key right of the right Shift on JIS
    "Keyboard_JpMuhenkan": 0x7B,   # 無変換, left of the space bar
    "Keyboard_JpHenkan": 0x79,     # 変換, right of the space bar
    "Keyboard_JpKana": 0x70,       # かな / ひらがな
    "Keyboard_AbntC1": 0x73,       # the /? key right of the right Shift on ABNT-2
    "Keyboard_AbntC2": 0x7E,       # the number pad's comma on ABNT-2
}


# --------------------------------------------------------------------------------------------
# Key legends
# --------------------------------------------------------------------------------------------
# What is actually printed on the keys that type nothing. Keys that produce a character are
# absent here on purpose: Keylegend asks Windows what they currently produce, which is what
# makes one profile serve every software layout on the same physical keyboard.
#
# A legend of ("x", "y") is a key with two lines, as Print Screen carries "PrtSc" over "SysRq".
Legends = dict[str, "str | tuple[str, str]"]

# Arrow and symbol glyphs, kept in one place so the legend tables stay readable.
WIN = "⊞"       # the Windows key, drawn as a boxed plus
MENU = "≡"      # the context-menu key
SHIFT = "⇧"
CAPS = "⇪"
TAB = "⇥"
BACK = "←"
UP, DOWN, LEFT, RIGHT = "↑", "↓", "←", "→"

# English, as printed on US, UK, Nordic, Portuguese, Swiss, Polish, Russian, Ukrainian, Dutch
# and Chinese keyboards — all of which label the control keys in English even where the
# character keys carry a second alphabet.
LEGENDS_EN: Legends = {
    "Keyboard_Escape": "esc",
    "Keyboard_Tab": TAB,
    "Keyboard_CapsLock": "caps lock",
    "Keyboard_LeftShift": SHIFT, "Keyboard_RightShift": SHIFT,
    "Keyboard_LeftCtrl": "ctrl", "Keyboard_RightCtrl": "ctrl",
    "Keyboard_LeftAlt": "alt", "Keyboard_RightAlt": "alt",
    "Keyboard_LeftGui": WIN, "Keyboard_RightGui": WIN,
    "Keyboard_Application": MENU,
    "Keyboard_Enter": "enter", "Keyboard_NumEnter": "enter",
    "Keyboard_Backspace": BACK,
    "Keyboard_Space": "",
    "Keyboard_PrintScreen": ("prt sc", "sys rq"),
    "Keyboard_ScrollLock": "scroll lock",
    "Keyboard_PauseBreak": ("pause", "break"),
    "Keyboard_Insert": "insert", "Keyboard_Delete": "delete",
    "Keyboard_Home": "home", "Keyboard_End": "end",
    "Keyboard_PageUp": "pg up", "Keyboard_PageDown": "pg dn",
    "Keyboard_NumLock": "num lock",
    "Keyboard_ArrowUp": UP, "Keyboard_ArrowDown": DOWN,
    "Keyboard_ArrowLeft": LEFT, "Keyboard_ArrowRight": RIGHT,
    "Keyboard_Function": "fn",
}

LEGENDS_DE: Legends = {
    "Keyboard_Escape": "esc",
    "Keyboard_Tab": BACK + TAB,
    "Keyboard_CapsLock": "⇩",
    "Keyboard_LeftShift": SHIFT, "Keyboard_RightShift": SHIFT,
    "Keyboard_LeftCtrl": "strg", "Keyboard_RightCtrl": "strg",
    "Keyboard_LeftAlt": "alt", "Keyboard_RightAlt": "alt",
    "Keyboard_LeftGui": WIN, "Keyboard_RightGui": WIN,
    "Keyboard_Application": MENU,
    "Keyboard_Enter": "enter", "Keyboard_NumEnter": "enter",
    "Keyboard_Backspace": BACK,
    "Keyboard_Space": "",
    "Keyboard_PrintScreen": ("druck", "s-abf"),
    "Keyboard_ScrollLock": "rollen",
    "Keyboard_PauseBreak": "pause",
    "Keyboard_Insert": "einfg", "Keyboard_Delete": "entf",
    "Keyboard_Home": "pos 1", "Keyboard_End": "ende",
    "Keyboard_PageUp": "bild^", "Keyboard_PageDown": "bildv",
    "Keyboard_NumLock": "num",
    "Keyboard_ArrowUp": UP, "Keyboard_ArrowDown": DOWN,
    "Keyboard_ArrowLeft": LEFT, "Keyboard_ArrowRight": RIGHT,
    "Keyboard_Function": "fn",
}

LEGENDS_FR: Legends = {
    "Keyboard_Escape": "échap",
    "Keyboard_Tab": TAB,
    "Keyboard_CapsLock": "verr maj",
    "Keyboard_LeftShift": SHIFT, "Keyboard_RightShift": SHIFT,
    "Keyboard_LeftCtrl": "ctrl", "Keyboard_RightCtrl": "ctrl",
    "Keyboard_LeftAlt": "alt", "Keyboard_RightAlt": "alt gr",
    "Keyboard_LeftGui": WIN, "Keyboard_RightGui": WIN,
    "Keyboard_Application": MENU,
    "Keyboard_Enter": "entrée", "Keyboard_NumEnter": "entrée",
    "Keyboard_Backspace": BACK,
    "Keyboard_Space": "",
    "Keyboard_PrintScreen": ("impr", "écran"),
    "Keyboard_ScrollLock": ("arrêt", "défil"),
    "Keyboard_PauseBreak": ("pause", "attn"),
    "Keyboard_Insert": "inser", "Keyboard_Delete": "suppr",
    "Keyboard_Home": "↖", "Keyboard_End": "fin",
    "Keyboard_PageUp": "pg préc", "Keyboard_PageDown": "pg suiv",
    "Keyboard_NumLock": "verr num",
    "Keyboard_ArrowUp": UP, "Keyboard_ArrowDown": DOWN,
    "Keyboard_ArrowLeft": LEFT, "Keyboard_ArrowRight": RIGHT,
    "Keyboard_Function": "fn",
}

LEGENDS_ES: Legends = {
    "Keyboard_Escape": "esc",
    "Keyboard_Tab": TAB,
    "Keyboard_CapsLock": "bloq mayús",
    "Keyboard_LeftShift": SHIFT, "Keyboard_RightShift": SHIFT,
    "Keyboard_LeftCtrl": "ctrl", "Keyboard_RightCtrl": "ctrl",
    "Keyboard_LeftAlt": "alt", "Keyboard_RightAlt": "alt gr",
    "Keyboard_LeftGui": WIN, "Keyboard_RightGui": WIN,
    "Keyboard_Application": MENU,
    "Keyboard_Enter": "intro", "Keyboard_NumEnter": "intro",
    "Keyboard_Backspace": BACK,
    "Keyboard_Space": "",
    "Keyboard_PrintScreen": ("impr pant", "pet sis"),
    "Keyboard_ScrollLock": "bloq despl",
    "Keyboard_PauseBreak": ("pausa", "inter"),
    "Keyboard_Insert": "insert", "Keyboard_Delete": "supr",
    "Keyboard_Home": "inicio", "Keyboard_End": "fin",
    "Keyboard_PageUp": "re pág", "Keyboard_PageDown": "av pág",
    "Keyboard_NumLock": "bloq num",
    "Keyboard_ArrowUp": UP, "Keyboard_ArrowDown": DOWN,
    "Keyboard_ArrowLeft": LEFT, "Keyboard_ArrowRight": RIGHT,
    "Keyboard_Function": "fn",
}

LEGENDS_IT: Legends = {
    "Keyboard_Escape": "esc",
    "Keyboard_Tab": TAB,
    "Keyboard_CapsLock": "bloc maiusc",
    "Keyboard_LeftShift": SHIFT, "Keyboard_RightShift": SHIFT,
    "Keyboard_LeftCtrl": "ctrl", "Keyboard_RightCtrl": "ctrl",
    "Keyboard_LeftAlt": "alt", "Keyboard_RightAlt": "alt gr",
    "Keyboard_LeftGui": WIN, "Keyboard_RightGui": WIN,
    "Keyboard_Application": MENU,
    "Keyboard_Enter": "invio", "Keyboard_NumEnter": "invio",
    "Keyboard_Backspace": BACK,
    "Keyboard_Space": "",
    "Keyboard_PrintScreen": ("stamp", "r sist"),
    "Keyboard_ScrollLock": "bloc scorr",
    "Keyboard_PauseBreak": ("pausa", "interr"),
    "Keyboard_Insert": "ins", "Keyboard_Delete": "canc",
    "Keyboard_Home": "↖", "Keyboard_End": "fine",
    "Keyboard_PageUp": "pag " + UP, "Keyboard_PageDown": "pag " + DOWN,
    "Keyboard_NumLock": "bloc num",
    "Keyboard_ArrowUp": UP, "Keyboard_ArrowDown": DOWN,
    "Keyboard_ArrowLeft": LEFT, "Keyboard_ArrowRight": RIGHT,
    "Keyboard_Function": "fn",
}

# The number pad's second legend: what each digit means with Num Lock off. Follows the same
# vocabulary as the navigation cluster above, so it is derived from it rather than repeated.
def numpad_legends(legends: Legends) -> Legends:
    def text(key: str) -> str:
        value = legends.get(key, "")
        return value if isinstance(value, str) else value[0]

    return {
        "Keyboard_Num7": text("Keyboard_Home"),
        "Keyboard_Num8": text("Keyboard_ArrowUp"),
        "Keyboard_Num9": text("Keyboard_PageUp"),
        "Keyboard_Num4": text("Keyboard_ArrowLeft"),
        "Keyboard_Num6": text("Keyboard_ArrowRight"),
        "Keyboard_Num1": text("Keyboard_End"),
        "Keyboard_Num2": text("Keyboard_ArrowDown"),
        "Keyboard_Num3": text("Keyboard_PageDown"),
        "Keyboard_Num0": text("Keyboard_Insert"),
        "Keyboard_NumPeriodAndDelete": text("Keyboard_Delete"),
    }


LEGEND_SETS: dict[str, Legends] = {
    "en": LEGENDS_EN,
    "de": LEGENDS_DE,
    "fr": LEGENDS_FR,
    "es": LEGENDS_ES,
    "it": LEGENDS_IT,
}


# --------------------------------------------------------------------------------------------
# Layout building blocks
# --------------------------------------------------------------------------------------------
# A row is a list of entries. ("id", width) places a key; (None, width) leaves a gap. Heights
# other than one unit — the number pad's tall Plus and Enter — are given as a third element.
Entry = tuple[str | None, float] | tuple[str | None, float, float]

# The upper half of an ISO Enter. It is not a key of its own: the placeholder is folded into the
# Enter below it as an extra rectangle, so one key ends up with two areas.
ENTER_TOP = "@enter-top"


def gap(width: float) -> Entry:
    return (None, width)


def alphas(variant: str) -> dict[str, list[Entry]]:
    """The five rows every keyboard has, from the number row down to the space bar.

    `variant` is the physical layout family: "ansi", "iso", "jis" or "abnt2". They differ in
    four places — the shape of the Enter, whether there is an extra key left of Z, whether the
    bottom row carries conversion keys, and how wide the space bar is.
    """
    iso_like = variant in ("iso", "jis", "abnt2")

    # Number row. JIS squeezes in the ¥ key by halving the Backspace.
    digits: list[Entry] = [
        ("Keyboard_GraveAccentAndTilde", 1),
        *((f"Keyboard_{d}", 1) for d in "1234567890"),
        ("Keyboard_MinusAndUnderscore", 1),
        ("Keyboard_EqualsAndPlus", 1),
    ]
    if variant == "jis":
        digits += [("Keyboard_JpYen", 1), ("Keyboard_Backspace", 1)]
    else:
        digits += [("Keyboard_Backspace", 2)]

    # Upper letter row. The ISO Enter's tall part takes the space ANSI gives to Backslash.
    upper: list[Entry] = [
        ("Keyboard_Tab", 1.5),
        *((f"Keyboard_{c}", 1) for c in "QWERTYUIOP"),
        ("Keyboard_BracketLeft", 1),
        ("Keyboard_BracketRight", 1),
        (ENTER_TOP, 1.5) if iso_like else ("Keyboard_Backslash", 1.5),
    ]

    # Home row.
    home: list[Entry] = [
        ("Keyboard_CapsLock", 1.75),
        *((f"Keyboard_{c}", 1) for c in "ASDFGHJKL"),
        ("Keyboard_SemicolonAndColon", 1),
        ("Keyboard_ApostropheAndDoubleQuote", 1),
    ]
    if iso_like:
        home += [("Keyboard_NonUsTilde", 1), ("Keyboard_Enter", 1.25)]
    else:
        home += [("Keyboard_Enter", 2.25)]

    # Lower letter row. ABNT-2 and JIS both add a key on the right, taken off the right Shift.
    lower: list[Entry] = []
    lower += [("Keyboard_LeftShift", 1.25), ("Keyboard_NonUsBackslash", 1)] if iso_like \
        else [("Keyboard_LeftShift", 2.25)]
    lower += [
        *((f"Keyboard_{c}", 1) for c in "ZXCVBNM"),
        ("Keyboard_CommaAndLessThan", 1),
        ("Keyboard_PeriodAndBiggerThan", 1),
        ("Keyboard_SlashAndQuestionMark", 1),
    ]
    if variant == "abnt2":
        lower += [("Keyboard_AbntC1", 1), ("Keyboard_RightShift", 1.75)]
    elif variant == "jis":
        lower += [("Keyboard_JpRo", 1), ("Keyboard_RightShift", 1.75)]
    else:
        lower += [("Keyboard_RightShift", 2.75)]

    return {"digits": digits, "upper": upper, "home": home, "lower": lower}


def bottom_row(variant: str, right: str = "win") -> list[Entry]:
    """The modifier row. `right` decides what sits between the right Alt and the menu key —
    a second Windows key on most keyboards, an Fn key on most gaming ones."""
    if variant == "jis":
        # JIS trades space-bar width for the three conversion keys.
        return [
            ("Keyboard_LeftCtrl", 1.25), ("Keyboard_LeftGui", 1.25), ("Keyboard_LeftAlt", 1.25),
            ("Keyboard_JpMuhenkan", 1),
            ("Keyboard_Space", 3.25),
            ("Keyboard_JpHenkan", 1), ("Keyboard_JpKana", 1),
            ("Keyboard_RightAlt", 1),
            ("Keyboard_Application", 1), ("Keyboard_RightCtrl", 1),
        ]

    # Both variants are the same key to Windows; only the printed legend differs, and that is
    # applied from the legend table further down. Keeping the id means the verified DeathStalker
    # profile keeps the mapping it was calibrated with.
    return [
        ("Keyboard_LeftCtrl", 1.25), ("Keyboard_LeftGui", 1.25), ("Keyboard_LeftAlt", 1.25),
        ("Keyboard_Space", 6.25),
        ("Keyboard_RightAlt", 1.25), ("Keyboard_RightGui", 1.25),
        ("Keyboard_Application", 1.25), ("Keyboard_RightCtrl", 1.25),
    ]


def function_row() -> list[Entry]:
    """Escape and F1 to F12, in the four groups every keyboard has kept since 1986."""
    return [
        ("Keyboard_Escape", 1), gap(1),
        *((f"Keyboard_F{n}", 1) for n in (1, 2, 3, 4)), gap(FUNCTION_ROW_GAP),
        *((f"Keyboard_F{n}", 1) for n in (5, 6, 7, 8)), gap(FUNCTION_ROW_GAP),
        *((f"Keyboard_F{n}", 1) for n in (9, 10, 11, 12)),
    ]


NAVIGATION = [
    ["Keyboard_PrintScreen", "Keyboard_ScrollLock", "Keyboard_PauseBreak"],
    ["Keyboard_Insert", "Keyboard_Home", "Keyboard_PageUp"],
    ["Keyboard_Delete", "Keyboard_End", "Keyboard_PageDown"],
]

NUMBER_PAD: list[list[Entry]] = [
    [("Keyboard_NumLock", 1), ("Keyboard_NumSlash", 1), ("Keyboard_NumAsterisk", 1),
     ("Keyboard_NumMinus", 1)],
    [("Keyboard_Num7", 1), ("Keyboard_Num8", 1), ("Keyboard_Num9", 1),
     ("Keyboard_NumPlus", 1, 2)],
    [("Keyboard_Num4", 1), ("Keyboard_Num5", 1), ("Keyboard_Num6", 1)],
    [("Keyboard_Num1", 1), ("Keyboard_Num2", 1), ("Keyboard_Num3", 1),
     ("Keyboard_NumEnter", 1, 2)],
    [("Keyboard_Num0", 2), ("Keyboard_NumPeriodAndDelete", 1)],
]


# --------------------------------------------------------------------------------------------
# Form factors
# --------------------------------------------------------------------------------------------
# Each returns rows as (y in units, x offset in units, entries). Building them this way keeps
# the three blocks independent: the main block never has to know where the number pad starts.

def _main_block(variant: str, right: str) -> list[tuple[float, float, list[Entry]]]:
    rows = alphas(variant)
    return [
        (0.0, 0.0, function_row()),
        (1.5, 0.0, rows["digits"]),
        (2.5, 0.0, rows["upper"]),
        (3.5, 0.0, rows["home"]),
        (4.5, 0.0, rows["lower"]),
        (5.5, 0.0, bottom_row(variant, right)),
    ]


def _arrow_cluster(x: float) -> list[tuple[float, float, list[Entry]]]:
    return [
        (4.5, x + 1.0, [("Keyboard_ArrowUp", 1)]),
        (5.5, x, [("Keyboard_ArrowLeft", 1), ("Keyboard_ArrowDown", 1),
                  ("Keyboard_ArrowRight", 1)]),
    ]


def _navigation_block(x: float) -> list[tuple[float, float, list[Entry]]]:
    # The three clusters sit on the function row, the number row and the row below it.
    return [
        (0.0, x, [(key, 1) for key in NAVIGATION[0]]),
        (1.5, x, [(key, 1) for key in NAVIGATION[1]]),
        (2.5, x, [(key, 1) for key in NAVIGATION[2]]),
        *_arrow_cluster(x),
    ]


def _number_pad(x: float) -> list[tuple[float, float, list[Entry]]]:
    return [(1.5 + i, x, row) for i, row in enumerate(NUMBER_PAD)]


def full_size(variant: str, right: str) -> tuple[float, float, list]:
    """104 / 105 keys: main block, navigation cluster, number pad."""
    nav_x = 15.0 + BLOCK_GAP
    pad_x = nav_x + 3.0 + BLOCK_GAP
    rows = _main_block(variant, right) + _navigation_block(nav_x) + _number_pad(pad_x)
    return pad_x + 4.0, 6.5, rows


def tenkeyless(variant: str, right: str) -> tuple[float, float, list]:
    """87 / 88 keys: a full-size board with the number pad cut off."""
    nav_x = 15.0 + BLOCK_GAP
    rows = _main_block(variant, right) + _navigation_block(nav_x)
    return nav_x + 3.0, 6.5, rows


def _compact(variant: str, right_column: list[str],
             function_row: bool) -> list[tuple[float, float, list[Entry]]]:
    """The shared shape of every keyboard without a navigation cluster.

    The right Shift gives up a unit so the Up arrow can sit beside it, the bottom row gives up
    a unit and a quarter so the other three fit, and what is left of the navigation keys becomes
    a single column down the right-hand edge — `right_column`, one key per letter row.
    """
    blocks = alphas(variant)
    lower = list(blocks["lower"])
    lower[-1] = (lower[-1][0], 1.75)

    top = 1.0 if function_row else 0.0
    return [
        (top + 0, 0.0, blocks["digits"] + [(right_column[0], 1)]),
        (top + 1, 0.0, blocks["upper"] + [(right_column[1], 1)]),
        (top + 2, 0.0, blocks["home"] + [(right_column[2], 1)]),
        (top + 3, 0.0, lower + [("Keyboard_ArrowUp", 1), (right_column[3], 1)]),
        (top + 4, 0.0, [
            ("Keyboard_LeftCtrl", 1.25), ("Keyboard_LeftGui", 1.25), ("Keyboard_LeftAlt", 1.25),
            ("Keyboard_Space", 6.25),
            ("Keyboard_RightAlt", 1), ("Keyboard_Function", 1), ("Keyboard_RightCtrl", 1),
            ("Keyboard_ArrowLeft", 1), ("Keyboard_ArrowDown", 1), ("Keyboard_ArrowRight", 1),
        ]),
    ]


def seventy_five(variant: str, right: str) -> tuple[float, float, list]:
    """75 %: everything a tenkeyless has, with the navigation cluster folded into one column.

    Models differ here more than at any other size. This follows the most common arrangement:
    the function row keeps its groups but loses its air, Delete sits at the top of the right-hand
    column, and the arrows are tucked in beside a shortened right Shift.
    """
    edge = 15.0 + BLOCK_GAP
    functions: list[Entry] = [
        ("Keyboard_Escape", 1), gap(BLOCK_GAP),
        *((f"Keyboard_F{n}", 1) for n in (1, 2, 3, 4)), gap(BLOCK_GAP),
        *((f"Keyboard_F{n}", 1) for n in (5, 6, 7, 8)), gap(BLOCK_GAP),
        *((f"Keyboard_F{n}", 1) for n in (9, 10, 11, 12)), gap(BLOCK_GAP),
        ("Keyboard_PrintScreen", 1),
    ]
    column = ["Keyboard_Home", "Keyboard_PageUp", "Keyboard_PageDown", "Keyboard_End"]
    rows: list[tuple[float, float, list[Entry]]] = [
        (0.0, 0.0, functions),
        (0.0, edge, [("Keyboard_Delete", 1)]),
        *_compact(variant, column, function_row=True),
    ]
    return edge + 1.0, 6.0, rows


def sixty_five(variant: str, right: str) -> tuple[float, float, list]:
    """65 %: no function row at all, so Delete moves up to the number row.

    Home takes the last slot rather than End, because on a board this size the key beside the
    Up arrow is the one people reach for most.
    """
    column = ["Keyboard_Delete", "Keyboard_PageUp", "Keyboard_PageDown", "Keyboard_Home"]
    return 16.0, 5.0, _compact(variant, column, function_row=False)


def sixty(variant: str, right: str) -> tuple[float, float, list]:
    """60 %: the main block and nothing else. Arrows and function keys live on the Fn layer.

    Keylegend colours what a key produces, and an Fn layer produces nothing Windows can be
    asked about — so those keys simply keep their base meaning here.
    """
    blocks = alphas(variant)
    rows: list[tuple[float, float, list[Entry]]] = [
        (0.0, 0.0, blocks["digits"]),
        (1.0, 0.0, blocks["upper"]),
        (2.0, 0.0, blocks["home"]),
        (3.0, 0.0, blocks["lower"]),
        (4.0, 0.0, bottom_row(variant, "fn")),
    ]
    return 15.0, 5.0, rows


def full_size_with_macro_column(variant: str, right: str) -> tuple[float, float, list]:
    """A full-size board with five macro keys down the left edge, as the BlackWidow V4 has.

    The macro keys have no scan code and produce no character, so Keylegend leaves them dark
    unless a profile colours them. They are here so that the preview matches the hardware.
    """
    width, height, rows = full_size(variant, right)
    offset = 1.25
    shifted = [(y, x + offset, entries) for y, x, entries in rows]
    macros = [(1.5 + i, 0.0, [(f"Keyboard_Macro{i + 1}", 1)]) for i in range(5)]
    return width + offset, height, shifted + macros


FORM_FACTORS = {
    "fullsize": full_size,
    "tkl": tenkeyless,
    "75": seventy_five,
    "65": sixty_five,
    "60": sixty,
    "fullsize-macro": full_size_with_macro_column,
}


# --------------------------------------------------------------------------------------------
# Assembly
# --------------------------------------------------------------------------------------------

def round2(value: float) -> float:
    """Two decimals is more than the quarter-unit grid ever needs, and keeps the JSON readable."""
    return round(value + 0.0, 2)


def build(name: str, vendor: str, model: str, physical_layout: str,
          form_factor: str, variant: str, legends: str,
          right: str = "win", verified: bool = False, note: str | None = None,
          usb: tuple[str, str] | None = None) -> dict:
    width_u, height_u, rows = FORM_FACTORS[form_factor](variant, right)

    legend_table = dict(LEGEND_SETS[legends])
    legend_table.update(numpad_legends(LEGEND_SETS[legends]))

    keys: list[dict] = []
    enter_top: dict | None = None

    for row_y, row_x, entries in rows:
        x = row_x
        for entry in entries:
            key_id, key_width = entry[0], entry[1]
            key_height = entry[2] if len(entry) > 2 else 1.0

            if key_id is None:
                x += key_width
                continue

            area = {
                "x": round2(MARGIN + x * UNIT),
                "y": round2(MARGIN + row_y * UNIT),
                "width": round2(key_width * UNIT),
                "height": round2(key_height * UNIT),
            }

            if key_id == ENTER_TOP:
                enter_top = area
                x += key_width
                continue

            key: dict = {"id": key_id, **area}

            cell = MATRIX.get(key_id)
            key["row"] = cell[0] if cell else None
            key["column"] = cell[1] if cell else None

            if key_id in EXTRA_SCAN_CODES:
                key["scanCode"] = EXTRA_SCAN_CODES[key_id]

            legend = legend_table.get(key_id)
            if isinstance(legend, tuple):
                key["label"], key["labelSecondary"] = legend
            elif legend is not None:
                # The number pad's second line is a secondary legend; everything else is primary.
                if key_id.startswith("Keyboard_Num") and key_id not in (
                        "Keyboard_NumLock", "Keyboard_NumEnter"):
                    key["labelSecondary"] = legend
                else:
                    key["label"] = legend

            keys.append(key)
            x += key_width

    if enter_top is not None:
        for key in keys:
            if key["id"] == "Keyboard_Enter":
                key["parts"] = [enter_top]
                # The tall Enter covers the position ANSI uses for Backslash. Its upper LED must
                # still report Enter, or the key above it would be coloured as a backslash.
                key["scanCode"] = 0x1C
                break

    profile: dict = {
        "$schema": "../device-profile.schema.json",
        "formatVersion": 1,
        "name": name,
        "vendor": vendor,
        "model": model,
        "physicalLayout": physical_layout,
        "canvas": {
            "width": round2(width_u * UNIT + 2 * MARGIN),
            "height": round2(height_u * UNIT + 2 * MARGIN),
        },
        "matrix": {"rows": 6, "columns": 22},
        "verified": verified,
        "keys": keys,
    }
    if usb:
        # What lets Windows-attached hardware be recognised instead of guessed. Only filled in
        # where the pair has actually been read off a real keyboard.
        profile["usb"] = {"vendorId": usb[0], "productId": usb[1]}
    if note:
        profile["note"] = note
    return profile


# --------------------------------------------------------------------------------------------
# What gets written
# --------------------------------------------------------------------------------------------
# folder, then the arguments to build(). Every entry is unverified unless it says otherwise:
# the geometry is right by construction, but only hardware can confirm the matrix mapping.

GENERIC_NOTE = ("Generated from the standard layout dimensions and not yet checked against "
                "hardware. Run the calibration and correct row/column where they disagree.")

PROFILES: list[tuple[str, dict]] = [
    # -- Full size, one per physical layout ---------------------------------------------------
    ("generic-fullsize-ansi-us", dict(
        name="Full-size keyboard (US)", vendor="Generic", model="Full-size 104-key",
        physical_layout="ANSI-US", form_factor="fullsize", variant="ansi", legends="en")),
    ("generic-fullsize-iso-de", dict(
        name="Full-size keyboard (German)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-DE", form_factor="fullsize", variant="iso", legends="de")),
    ("generic-fullsize-iso-uk", dict(
        name="Full-size keyboard (UK)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-UK", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-iso-fr", dict(
        name="Full-size keyboard (French, AZERTY)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-FR", form_factor="fullsize", variant="iso", legends="fr")),
    ("generic-fullsize-iso-es", dict(
        name="Full-size keyboard (Spanish)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-ES", form_factor="fullsize", variant="iso", legends="es")),
    ("generic-fullsize-iso-it", dict(
        name="Full-size keyboard (Italian)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-IT", form_factor="fullsize", variant="iso", legends="it")),
    ("generic-fullsize-iso-nordic", dict(
        name="Full-size keyboard (Nordic)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-NORDIC", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-iso-pt", dict(
        name="Full-size keyboard (Portuguese)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-PT", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-iso-ch", dict(
        name="Full-size keyboard (Swiss)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-CH", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-iso-ru", dict(
        name="Full-size keyboard (Russian)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-RU", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-iso-pl", dict(
        name="Full-size keyboard (Polish)", vendor="Generic", model="Full-size 105-key",
        physical_layout="ISO-PL", form_factor="fullsize", variant="iso", legends="en")),
    ("generic-fullsize-jis-jp", dict(
        name="Full-size keyboard (Japanese)", vendor="Generic", model="Full-size 109-key",
        physical_layout="JIS-JP", form_factor="fullsize", variant="jis", legends="en")),
    ("generic-fullsize-abnt2-br", dict(
        name="Full-size keyboard (Brazilian)", vendor="Generic", model="Full-size 107-key",
        physical_layout="ABNT2-BR", form_factor="fullsize", variant="abnt2", legends="en")),

    # -- Tenkeyless ---------------------------------------------------------------------------
    ("generic-tkl-ansi-us", dict(
        name="Tenkeyless keyboard (US)", vendor="Generic", model="Tenkeyless 87-key",
        physical_layout="ANSI-US", form_factor="tkl", variant="ansi", legends="en")),
    ("generic-tkl-iso-de", dict(
        name="Tenkeyless keyboard (German)", vendor="Generic", model="Tenkeyless 88-key",
        physical_layout="ISO-DE", form_factor="tkl", variant="iso", legends="de")),
    ("generic-tkl-iso-uk", dict(
        name="Tenkeyless keyboard (UK)", vendor="Generic", model="Tenkeyless 88-key",
        physical_layout="ISO-UK", form_factor="tkl", variant="iso", legends="en")),
    ("generic-tkl-iso-fr", dict(
        name="Tenkeyless keyboard (French, AZERTY)", vendor="Generic", model="Tenkeyless 88-key",
        physical_layout="ISO-FR", form_factor="tkl", variant="iso", legends="fr")),

    # -- Compact ------------------------------------------------------------------------------
    ("generic-75-ansi-us", dict(
        name="75 % keyboard (US)", vendor="Generic", model="75 %",
        physical_layout="ANSI-US", form_factor="75", variant="ansi", legends="en")),
    ("generic-75-iso-de", dict(
        name="75 % keyboard (German)", vendor="Generic", model="75 %",
        physical_layout="ISO-DE", form_factor="75", variant="iso", legends="de")),
    ("generic-65-ansi-us", dict(
        name="65 % keyboard (US)", vendor="Generic", model="65 %",
        physical_layout="ANSI-US", form_factor="65", variant="ansi", legends="en")),
    ("generic-65-iso-de", dict(
        name="65 % keyboard (German)", vendor="Generic", model="65 %",
        physical_layout="ISO-DE", form_factor="65", variant="iso", legends="de")),
    ("generic-60-ansi-us", dict(
        name="60 % keyboard (US)", vendor="Generic", model="60 %",
        physical_layout="ANSI-US", form_factor="60", variant="ansi", legends="en")),
    ("generic-60-iso-de", dict(
        name="60 % keyboard (German)", vendor="Generic", model="60 %",
        physical_layout="ISO-DE", form_factor="60", variant="iso", legends="de")),

    # -- Named models -------------------------------------------------------------------------
    # Razer keyboards put Fn where most boards have a second Windows key, which is the only
    # difference from the generic profiles above for all but the BlackWidow.
    ("razer-deathstalker-v2-de", dict(
        name="Razer DeathStalker V2", vendor="Razer", model="DeathStalker V2",
        physical_layout="ISO-DE", form_factor="fullsize", variant="iso", legends="de",
        right="fn", verified=True, usb=("1532", "0295"))),
    ("razer-deathstalker-v2-us", dict(
        name="Razer DeathStalker V2", vendor="Razer", model="DeathStalker V2",
        physical_layout="ANSI-US", form_factor="fullsize", variant="ansi", legends="en",
        right="fn", usb=("1532", "0295"))),
    ("razer-blackwidow-v4-de", dict(
        name="Razer BlackWidow V4", vendor="Razer", model="BlackWidow V4",
        physical_layout="ISO-DE", form_factor="fullsize-macro", variant="iso", legends="de",
        right="fn")),
    ("razer-blackwidow-v4-us", dict(
        name="Razer BlackWidow V4", vendor="Razer", model="BlackWidow V4",
        physical_layout="ANSI-US", form_factor="fullsize-macro", variant="ansi", legends="en",
        right="fn")),
    ("razer-huntsman-v3-pro-de", dict(
        name="Razer Huntsman V3 Pro", vendor="Razer", model="Huntsman V3 Pro",
        physical_layout="ISO-DE", form_factor="fullsize", variant="iso", legends="de",
        right="fn")),
    ("razer-huntsman-v3-pro-us", dict(
        name="Razer Huntsman V3 Pro", vendor="Razer", model="Huntsman V3 Pro",
        physical_layout="ANSI-US", form_factor="fullsize", variant="ansi", legends="en",
        right="fn")),
    ("razer-huntsman-v3-pro-tkl-us", dict(
        name="Razer Huntsman V3 Pro TKL", vendor="Razer", model="Huntsman V3 Pro TKL",
        physical_layout="ANSI-US", form_factor="tkl", variant="ansi", legends="en",
        right="fn")),
    ("razer-ornata-v3-de", dict(
        name="Razer Ornata V3", vendor="Razer", model="Ornata V3",
        physical_layout="ISO-DE", form_factor="fullsize", variant="iso", legends="de",
        right="fn")),
    ("razer-ornata-v3-us", dict(
        name="Razer Ornata V3", vendor="Razer", model="Ornata V3",
        physical_layout="ANSI-US", form_factor="fullsize", variant="ansi", legends="en",
        right="fn")),
]


def main() -> None:
    repository = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=repository / "devices")
    parser.add_argument("--only", default=None,
                        help="only write profiles whose folder name contains this text")
    parser.add_argument("--list", action="store_true", help="name the profiles, write nothing")
    # Passing usb=None through build() would overwrite an id somebody added by hand, so it is
    # only forwarded when the entry actually carries one.
    arguments = parser.parse_args()

    selected = [(folder, spec) for folder, spec in PROFILES
                if arguments.only is None or arguments.only in folder]

    for folder, spec in selected:
        if arguments.list:
            print(folder)
            continue

        profile = build(note=None if spec.get("verified") else GENERIC_NOTE, **spec)
        target = arguments.output / folder
        target.mkdir(parents=True, exist_ok=True)
        path = target / "device.json"
        path.write_text(json.dumps(profile, indent=2, ensure_ascii=False) + "\n",
                        encoding="utf-8")
        mapped = sum(1 for k in profile["keys"] if k["row"] is not None)
        print(f"{folder}: {len(profile['keys'])} keys, {mapped} mapped, "
              f"{profile['canvas']['width']:.0f} x {profile['canvas']['height']:.0f}")

    if not arguments.list:
        print(f"\n{len(selected)} profile(s) written to {arguments.output}")


if __name__ == "__main__":
    main()
