# Profil hinzufügen

Ein Anwendungsprofil ist **Daten, kein Code**. Du brauchst weder C# noch Entwicklungswerkzeuge —
ein Texteditor und echte Kenntnis des Programms genügen, und der zweite Teil ist der schwerere.

Soll das Profil nur bei dir gelten, lege es in der Oberfläche an: Es landet in `settings.json`
und braucht nichts von alldem. Eine Datei unter `profiles/` ist der Weg, auf dem ein Profil mit
der Anwendung für alle ausgeliefert wird.

## 1. Datei anlegen

```
profiles/apps/<kennung>.json      Programme
profiles/games/<kennung>.json     Spiele
```

Der Dateiname muss der `id` in der Datei entsprechen. Kleinbuchstaben, `a-z0-9-`. Der Build
sammelt jede Datei in diesen beiden Ordnern über ein Platzhaltermuster ein, es ist also keine
Projektdatei zu bearbeiten.

Eine Kennung ist endgültig. Überschreibungen und ausgeblendete Profile hängen an ihr, eine
Umbenennung in einer späteren Fassung macht also die Änderungen eines Anwenders heimatlos. Wähle
einen Namen, der auch nach einer Umbenennung des Programms noch stimmt — `adobe-photoshop`,
nicht `photoshop-2026`.

## 2. Inhalt eintragen

Die Felder, die drei Abschnitte, die Funktionsgruppen, die Modifier-Kombinationen und die
Farbkonventionen stehen in [profiles/FORMAT.md](../../profiles/FORMAT.md). Lies das zuerst; es
ist die Referenz, und diese Seite wiederholt sie nicht.

Was folgt, ist der Teil, der auch dann schiefgeht, wenn das Format gelesen wurde.

## 3. Position und Zeichen sind nicht dasselbe

Tastenkennungen stammen aus dem Geräteprofil und bezeichnen **US-Positionen**. `Keyboard_Y` ist
die physische Taste, die auf einer US-Tastatur `Y` tippt — auf einer deutschen tippt dieselbe
Taste `Z`. Deshalb kennt das Format zwei Arten, eine Taste zu benennen, und die falsche zu
wählen erzeugt ein Profil, das auf jedem Nicht-US-Layout sichtbar falsch ist, während es auf dem
Rechner, auf dem es entstand, tadellos aussieht.

Frag bei jedem Eintrag, worum es eigentlich geht:

- **Wo die Hand liegt → Position.** Eine Hervorhebung für WASD meint die Form, die die Finger
  bilden, nicht die Buchstaben. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` sind
  überall die richtigen Tasten.
- **Was der Befehl ist → Zeichen.** `Strg+Z` bedeutet „die Taste, die z tippt“. Als Position
  geschrieben, erschienen Rückgängig und Wiederherstellen auf einer deutschen Tastatur
  vertauscht.
- **Tasten, die nichts tippen → wieder Position.** Esc, Tab, Eingabe, Rücktaste, Pfeile und
  Funktionstasten erzeugen kein Zeichen, `shortcuts.keys` benennt sie daher ohne Mehrdeutigkeit
  über die Kennung.

### Bei Hervorhebungen entscheidet, wie das Programm liest

QWERTZ und QWERTY unterscheiden sich an genau zwei Stellen, `Keyboard_Y` und `Keyboard_Z` sind
also die einzigen Kennungen, bei denen das schiefgehen kann. Dafür geht es lautlos schief.

Eine Hervorhebungskennung ist immer eine **physische Position**. Die Frage ist, welche physische
Taste das Programm meint, und das hängt davon ab, wie es die Tastatur ausliest:

| Das Programm bindet an | Beispiele | `Z` in seiner Dokumentation heißt |
|---|---|---|
| das **Zeichen** (Windows-Tastencodes, die dem Layout folgen) | Photoshop, Blender, GIMP, Krita — Anwendungen im Allgemeinen | `Keyboard_Y` — die obere Reihe, die auf Deutsch `Z` tippt |
| die **Position** (Scancodes, wie in den meisten Spiel-Engines, damit WASD an seinem Platz bleibt) | Spiele im Allgemeinen | `Keyboard_Z` — die untere Reihe |

Lässt sich für ein bestimmtes Programm nicht klären, welcher Fall vorliegt, gehören die
Einträge für `Y` und `Z` nicht ins Profil. Alle übrigen Buchstaben sind davon unberührt.

## 4. Weglassen, was du nicht sicher weißt

Ein falsches Kürzel ist schlimmer als ein fehlendes. Ein fehlender Eintrag lässt eine Taste
dunkel und kostet nichts; ein falscher lässt die Tastatur etwas Unwahres behaupten, und der
Anwender hat keine Möglichkeit, das zu bemerken. Die Beschriftung macht die Behauptung
ausdrücklich — richtig macht sie sie nicht.

Also:

- Trage nur ein, wovon du überzeugt bist, dass es die **Standardbelegung** des Programms im
  Auslieferungszustand ist. Deine eigene Installation ist keine Quelle; du hast vermutlich etwas
  geändert und es vergessen.
- Prüfe gegen die Dokumentation des Programms oder gegen das Programm selbst mit unveränderten
  Einstellungen.
- Wo sich die Vorgaben zwischen Versionen unterscheiden, gilt die aktuelle.
- Erfinde nichts. Hat ein Programm für etwas kein allgemein bekanntes Kürzel, bekommt es keinen
  Eintrag.

Zwölf richtige Kürzel sind mehr wert als dreißig, von denen vier falsch sind. Für die
Beschriftungen der Hervorhebungen gilt dasselbe: Wenn du nicht sagen kannst, was eine Taste tut,
ist das ein Zeichen dafür, dass der Eintrag noch nicht ins Profil gehört.

## 5. Prüfen

```bash
dotnet test
```

Die Profiltests prüfen jede Datei unter `profiles/`: Kennung eindeutig und gleich dem
Dateinamen, `kind` passend zum Ordner, jede Tastenkennung im Geräteprofil vorhanden, Farben
lesbar, Gruppen und Modifier-Kombinationen gültig und kanonisch geschrieben, jedes Kürzel
beschriftet, kein Buchstabe unter `shortcuts.keys` (er gehört unter `characters`), kein Profil
ohne Inhalt, und keine zwei Profile, die dieselbe Programmdatei beanspruchen, ohne sich durch
`titleContains` zu unterscheiden.

Absichtlich **nicht** geprüft wird, ob eine Beschriftung zweimal unter derselben Kombination
vorkommt. Das sah nach einem Weg aus, Kopierfehler zu finden, und fand stattdessen echte
Zweitbelegungen: Browser schließen einen Tab mit `Strg+W` und mit `Strg+F4`. Eine Prüfung, die
bei richtigen Daten anschlägt, ist schlechter als keine.

Was kein Test prüfen kann, ist, ob ein Kürzel *stimmt*. Dafür gibt es die Durchsicht, und dafür
trägt jeder Eintrag eine Beschriftung.

## 6. Gegen das Programm ausprobieren

Starte Keylegend, hol das Programm in den Vordergrund und halte die Modifier, die dein Profil
belegt. Die Vorschau zeigt dasselbe wie die Tastatur, ein Rechner ohne Chroma-Hardware genügt
dafür also. Vergleiche mit den Menüs des Programms — ein Befehl, dessen Beschriftung du im
Programm nicht wiederfindest, ist das Erste, was wieder herausfliegt.

## 7. Pull Request eröffnen

Bitte gib an, gegen welches Programm und welche Version du geprüft hast und wie du die
Belegungen überprüft hast — Dokumentation des Programms, das Programm selbst oder beides. Siehe
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Ein kleines, sicheres Profil ist ein guter Beitrag. Ein großes, halb erinnertes nicht.
