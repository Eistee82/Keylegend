# Shipped application profiles — design

*2026-08-12*

## The problem

Keylegend shipped exactly one profile: a generic game profile with WASD highlighted. Two things
were missing.

There was no library. A user who wanted Photoshop's shortcuts on the keyboard had to build the
profile themselves, key by key, and so did the next user.

And there was no provenance. `settings.json` stored every profile whole, with no record of which
came from the program and which the user built. The moment anything was saved, the shipped
profile became indistinguishable from a hand-made one. That has two consequences: a profile can
never be reset, because there is nothing to reset *to*; and a later release can never improve a
profile, because the saved copy has already replaced it.

## What was built

Around ninety profiles for common programs and games, active automatically when their program
has the foreground, editable, and resettable.

### Profiles are data

`profiles/apps/*.json` and `profiles/games/*.json`, one file per profile, documented in
`profiles/FORMAT.md`. This follows the rule already stated in `MAINTAINING.md` for devices: if adding
coverage would need code, the format is wrong.

They are compiled into `Keylegend.Core` as embedded resources rather than copied next to the
executable, which is how device profiles work. Three reasons:

- A single-file release carries them, with no folder to lose.
- Nothing on disk can be edited by accident — which is what makes "reset to shipped" mean
  anything at all.
- A file that fails to parse breaks the build rather than producing a program that silently has
  no profiles.

### Overriding is per section

A profile has three sections: **match** (which programs), **highlights** (keys pinned to a
colour), **shortcuts** (what a modifier layer means). The user's changes are stored as an
override keyed on the profile's stable `id`, and only for the sections actually touched.

The alternatives were both worse:

- **Per whole profile.** Change one colour and the profile is yours forever — a corrected
  shortcut in a later release never reaches you.
- **Per field.** Recolour `W`, and a release that adds `Q` gives you a mixture you never
  configured. For a display the user has to trust, predictability beats currency.

Sections are the granularity at which the result is still explainable: *you edited the
highlights, so the highlights are yours now.* Resetting works per section and for the whole
profile, which also restores the name.

Shipped profiles are hidden rather than deleted, because the file is inside the program and
deleting one would only last until the next start.

### Shortcuts carry labels

`ShortcutSet` used to store only `character → FunctionGroup`. That is all the LEDs can show —
they have colour and nothing else. But at ninety profiles and 1700 shortcuts, `"j" → Edit` is
unverifiable: nobody can tell whether it is right. A `label` saying what the command *does*
("Duplicate layer") makes the preview inside the application useful and makes the collection
checkable at all. Highlights carry labels for the same reason ("Forward", "Brush").

### A profile layers over the general shortcuts, it does not replace them

`FrameComposer` previously did `profile?.Shortcuts ?? shortcuts` — a profile replaced the entire
catalogue. With one game profile that was harmless. With a Photoshop profile that defines only
the Ctrl layer, it would have blanked out `Win+E`, which Windows assigns and which is true
whatever is in front. Profiles now replace only the layers they name.

### Window titles, reluctantly

The first draft matched on process name alone. Two real cases broke it: LibreOffice runs Writer
and Calc both as `soffice`, and every Java program is `javaw`. With nothing to tell them apart,
one profile wins arbitrarily and the keyboard shows Calc's shortcuts to somebody writing a
letter — a confident wrong answer, which is worse than no profile.

`match.titleContains` is a plain case-insensitive substring test, not a pattern language.
Titles are localised and change with the open document; a matching rule nobody can read back is
worse than no rule. A test rejects any two shipped profiles sharing an executable name unless
both narrow themselves by title.

## Migration

`settings.json` moves to `formatVersion` 2. A version 1 file has no ids and no provenance, so
there is no way to tell which of its entries were once shipped. All of them become user
profiles: keeping the user's work matters more than guessing. The shipped profiles then appear
alongside them, which may mean two entries for one program until the user removes one — visible
and fixable, unlike silently discarding an entry.

Saved shortcut sets are read in both the old shape (`"Edit"`) and the new one
(`{"group": "Edit", "label": "Undo"}`), so an existing file does not lose the user's edits.

## Checks

`dotnet test` validates every shipped profile: id unique and equal to the file name, key ids
present in a shipped device, colours parsable, groups and modifier combinations valid, every
shortcut labelled, no letter key addressed by position, no profile that would do nothing, and no
two profiles claiming one executable without a title condition.

One check was written and then removed: rejecting the same label twice under one modifier. It
looked like a way to catch copy-and-paste slips and caught real aliases instead — browsers close
a tab with both `Ctrl+W` and `Ctrl+F4`. A check that fires on correct data is worse than none.

## Accuracy

The rule given to everyone writing profiles: **a wrong shortcut is worse than a missing one.**
It does not fail loudly — it lights a key, and the user has no way to tell the keyboard is
lying. Anything uncertain is left out.

## Also done

The application had no icon: `TrayIcon` drew one at runtime, so Explorer, the taskbar, Alt-Tab
and the title bar all showed the generic default. There is now a real `keylegend.ico`,
reproducible from `tools/make-icon.py` rather than sitting in the repository as an opaque
binary.
