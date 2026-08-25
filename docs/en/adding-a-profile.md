# Adding a profile

An application profile is **data, not code**. You need no C# and no build tools — a text editor
and actual knowledge of the program are enough, and the second part is the harder one.

If you only want a profile for yourself, make it in the interface: it is stored in
`settings.json` and needs none of this. A file under `profiles/` is how a profile ships with the
application for everybody.

## 1. Create the file

```
profiles/apps/<id>.json      programs
profiles/games/<id>.json     games
```

The file name must equal the `id` inside. Lower case, `a-z0-9-`. The build embeds every file in
these two folders by wildcard, so there is no project file to edit.

An id is permanent. User overrides and hidden-profile entries are keyed on it, so renaming one
in a later release orphans somebody's edits. Pick a name that will still be right after the
program is rebranded — `adobe-photoshop`, not `photoshop-2026`.

## 2. Fill it in

The fields, the three sections, the function groups, the modifier combinations and the colour
conventions are described in [profiles/FORMAT.md](../../profiles/FORMAT.md). Read that first;
it is the reference and this page does not repeat it.

What follows is the part that goes wrong even when the format has been read.

## 3. Positions and characters are not the same thing

Key ids come from the lighting protocol's own table and name **US positions**. `Keyboard_Y` is the
physical key that types `Y` on a US keyboard — on a German one, that key types `Z`. The format
therefore has two ways of naming a key, and picking the wrong one produces a profile that is
visibly wrong on every non-US layout while looking perfectly fine on the machine it was written on.

The question to ask for each entry is what it is really about:

- **Where the hand is → position.** A highlight for WASD is about the shape your fingers make,
  not about the letters. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` are the right
  keys everywhere.
- **What the command is → character.** `Ctrl+Z` means "the key that types z". Written as a
  position, undo and redo appear swapped on a German keyboard.
- **Keys that type nothing → position again.** Escape, Tab, Enter, Backspace, the arrows and the
  function keys have no character, so `shortcuts.keys` names them by id with no ambiguity.

### For highlights, it depends on how the program reads the keyboard

QWERTZ and QWERTY differ in exactly two places, so `Keyboard_Y` and `Keyboard_Z` are the only
ids where this can go wrong. They go wrong silently.

A highlight id is always a **physical position**. The question is which physical key the program
means, and that follows from how it reads the keyboard:

| The program binds to | Examples | `Z` in its documentation means |
|---|---|---|
| the **character** (Windows virtual-key codes, which follow the layout) | Photoshop, Blender, GIMP, Krita — applications generally | `Keyboard_Y` — the top-row key, which types `Z` on a German board |
| the **position** (scancodes, as most game engines use, so WASD stays put) | games generally | `Keyboard_Z` — the bottom-row key |

If you cannot establish which way a particular program reads the keyboard, leave the `Y` and `Z`
entries out. Every other letter is unaffected.

## 4. Leave out what you are not sure of

A wrong shortcut is worse than a missing one. A missing entry leaves a key dark and costs
nothing; a wrong one makes the keyboard state something untrue, and the user has no way to tell
that it is untrue. The label makes the claim explicit — it does not make it correct.

So:

- Write down only what you are confident is the program's **default** binding, out of the box.
  Your own installation is not a source; you have probably changed things and forgotten.
- Check against the program's own documentation, or against the program itself with settings
  untouched.
- Where defaults differ between versions, follow the current one.
- Do not invent. If a program has no well-known shortcut for something, it gets no entry.

Twelve correct shortcuts are worth more than thirty of which four are wrong. The same applies to
highlight labels: if you cannot say what a key does, that is a sign the entry does not belong in
the profile yet.

## 5. Test it

```bash
dotnet test
```

The profile tests check every file under `profiles/`: the id is unique and matches the file name,
`kind` matches the folder, every key id exists in the matrix table, colours parse, groups and
modifier combinations are valid and canonically spelled, every shortcut carries a label, no letter
key sits under `shortcuts.keys` (it belongs under `characters`), no profile is empty, and no two
profiles claim one executable without telling themselves apart through `titleContains`.

One thing is deliberately **not** checked: the same label appearing twice under one modifier. It
looked like a way to catch copy-and-paste slips and caught real aliases instead — browsers close
a tab with both `Ctrl+W` and `Ctrl+F4`. A check that fires on correct data is worse than none.

What no test can check is whether a shortcut is *true*. That is what review is for, and the
reason every entry has a label to review.

## 6. Try it against the program

Start Keylegend, bring the program to the front and hold the modifiers your profile defines. The
preview shows the same thing the keyboard does, so a laptop without Chroma hardware is enough
for this. Compare it with the program's own menus — a command whose label you cannot find in the
program is the first thing to remove.

## 7. Open a pull request

Please state which program and which version you checked against, and how you verified the
bindings — the program's documentation, the program itself, or both. See
[CONTRIBUTING.md](../../CONTRIBUTING.md).

A small, certain profile is a good contribution. A large, half-remembered one is not.
