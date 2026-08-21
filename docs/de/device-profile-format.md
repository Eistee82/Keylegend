# Geräteprofil-Format

Ein Geräteprofil beschreibt ein Tastaturmodell in einem physischen Layout. Es ist eine einzelne
Datei in einem Ordner unter `devices/`, benannt nach `<hersteller>-<modell>-<layout>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     Geometrie und LED-Zuordnung
```

`devices/device-profile.schema.json` beschreibt dasselbe maschinenlesbar. Wer es wie die
mitgelieferten Profile in einer `$schema`-Zeile nennt, bekommt in den meisten Editoren
Vervollständigung und Fehlerhinweise schon beim Tippen.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Feld | Bedeutung |
|---|---|
| `formatVersion` | Formatstand. Derzeit `1`. Ein Build weist ein Profil mit höherer Nummer ab. |
| `name` | Was die Oberfläche anzeigt. |
| `vendor`, `model` | Hersteller und Modell. `"Generic"` für ein Profil, das ein Layout statt eines Produkts beschreibt. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — die physische *Anordnung* der Tasten, nicht das Software-Layout. |
| `canvas` | Das Koordinatensystem, auf das sich alle Tastenpositionen beziehen. Nur Verhältnisse zählen; die mitgelieferten Profile rechnen in Millimetern. |
| `matrix` | Größe der Hersteller-LED-Matrix. Razer-Tastaturen sind 6 × 22, unabhängig von ihrer Größe. |
| `verified` | `true`, sobald jemand die Zuordnung an echter Hardware bestätigt hat. |
| `note` | Optionaler Freitext für die Person, die die Datei als Nächstes öffnet. |
| `image` | Optional und derzeit ungenutzt — siehe [Bilder](#bilder) unten. |
| `keys[]` | Ein Eintrag je Taste. |

### Physisches Layout, nicht Software-Layout

`physicalLayout` legt die *Form* der Tastatur fest: ob die Eingabetaste hoch und L-förmig ist, ob
es eine Zusatztaste links von `Y` gibt, ob die untere Reihe japanische Umwandlungstasten trägt.

Es sagt nichts darüber, welche Zeichen diese Tasten erzeugen. Das erfragt Keylegend zur Laufzeit
bei Windows, für das gerade aktive Layout. Ein ISO-DE-Profil bedient deshalb eine deutsche
Tastatur, ganz gleich ob Windows auf Deutsch, US, Dvorak oder Neo steht — und deshalb gibt es ein
Profil je *physischem* Layout und nicht eines je Sprache.

### Tasteneinträge

| Feld | Bedeutung |
|---|---|
| `id` | Eindeutige Kennung. Folge der bestehenden Benennung: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Position der linken oberen Ecke auf der Zeichenfläche. |
| `width`, `height` | Größe der Taste auf der Zeichenfläche. |
| `row`, `column` | Zelle in der Hersteller-LED-Matrix. Beide `null`, solange unbekannt — ein gültiger Zustand, und genau wofür die Kalibrierung da ist. |
| `scanCode` | Überschreibt den Standard-Scancode. Nur nötig, wo das physische Layout der US-basierten Benennung widerspricht. |
| `parts` | Weitere Rechtecke derselben Taste, für Tasten, die nicht rechteckig sind. |
| `label` | Was auf der Taste steht, für Tasten, die nichts schreiben. |
| `labelSecondary` | Eine zweite Zeile, unter der ersten. |

### Beschriftungen gehören zur Tastatur

`label` ist das, was *auf der Tastenkappe gedruckt* ist, keine Übersetzung dessen, was die Taste
tut. Eine deutsche Tastatur sagt `strg`, eine französische `ctrl`, eine italienische
`bloc maiusc` — und jede von ihnen sagt das unabhängig davon, auf welche Sprache Keylegends
eigene Menüs eingestellt sind. Die Oberflächensprache zu wechseln ändert die Beschriftungen nie.

Tasten, die ein Zeichen erzeugen, tragen überhaupt kein `label`. Ihre Beschriftung kommt aus dem
aktiven Windows-Layout und folgt damit von selbst Umschalt, Feststell und AltGr.

### Tasten mit mehr als einem Rechteck

Die ISO-Eingabetaste ist der Standardfall: eine Taste über zwei Reihen.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

Das Hauptrechteck trägt die Zelle, `parts` ergänzt den Rest der Form. Der ausdrückliche
`scanCode` steht dort, weil die obere Hälfte die Position einnimmt, die ANSI für den Backslash
verwendet: ohne ihn würde die Oberseite der Eingabetaste eingefärbt, als schriebe sie `\`.

### Scancodes für Tasten, die es nur auf einem Layout gibt

Die Standardtabelle in `Keylegend.Core` deckt ab, was eine US-Tastatur hat. Tasten, die es nur
anderswo gibt, nennen ihren Code im Profil — so muss für ein Layout kein C# geändert werden:

| Kennung | Taste | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, links von der Rücktaste auf JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, rechts der rechten Umschalttaste auf JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, links der Leertaste | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, rechts der Leertaste | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | die `/?`-Taste rechts der rechten Umschalttaste auf ABNT-2 | `0x73` |

## Regeln, die der Validierer durchsetzt

Diese Punkte prüft die CI, ein Profil mit solchen Fehlern kann also nicht übernommen werden:

- Tastenkennungen sind eindeutig
- Keine zwei Tasten beanspruchen dieselbe Matrixzelle
- Keine zwei Tasten überlappen sich auf der Zeichenfläche
- `row` und `column` sind entweder beide gesetzt oder beide `null`
- Zellen liegen innerhalb der angegebenen Matrix
- Tasten liegen innerhalb der Zeichenfläche
- Jede Taste hat eine positive Größe
- Ein unter `image` genanntes Bild ist auch vorhanden

## Benennung und der Unterschied ISO/ANSI

Die Tastenkennungen folgen dem US-Layout, weil die Matrix des Herstellers das ebenfalls tut. Auf
einer deutschen Tastatur sitzt das physische `Z` daher auf `Keyboard_Y` und umgekehrt. Das
betrifft nur die Benennung: weder Position noch Verhalten ändern sich, denn das tatsächliche
Zeichen wird zur Laufzeit bei Windows erfragt.

Zwei Kennungen gibt es nur auf ISO-Tastaturen:

| Kennung | Taste | Razer-Zelle |
|---|---|---|
| `Keyboard_NonUsBackslash` | die Zusatztaste links von `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, Zeile 4 Spalte 2 |
| `Keyboard_NonUsTilde` | die Taste neben der Eingabetaste in der Grundreihe (`#`, `'`) | `RZKEY_EUR_1`, Zeile 3 Spalte 13 |

Auf ISO-Tastaturen erstreckt sich die hohe Eingabetaste über zwei Matrixpositionen: die obere
Hälfte dort, wo ANSI den Backslash hat (Zeile 2, Spalte 14), die untere auf `Keyboard_Enter`
(Zeile 3, Spalte 14).

**Ob beide tatsächlich leuchten, hängt vom Modell ab.** Die Herstellertabelle beschreibt, was die
Matrix *ansprechen kann*, nicht, was eine bestimmte Tastatur *bestückt* hat. Bei der
DeathStalker V2 zeigte die Kalibrierung, dass die obere Zelle überhaupt keine LED treibt — die
gesamte Eingabetaste wird von der unteren beleuchtet. Deshalb modelliert das mitgelieferte Profil
die Eingabetaste als eine Taste mit zwei Rechtecken und nicht als zwei Tasten.

Genau das lässt sich aus keiner Dokumentation ableiten, und genau deshalb sollte ein Profil erst
dann `verified` heißen, wenn jemand es an der Hardware durchgegangen ist.

## Bilder

`image` ist optional und wird derzeit nicht verwendet: Die Vorschau auf dem Bildschirm wird aus
der Geometrie oben gezeichnet. Sie zu zeichnen hält die Vorschau in jeder Fenstergröße scharf und
macht es unmöglich, dass Bild und Profil auseinanderlaufen.

Wenn du doch eines beilegst, muss es ein Bild sein, das **du** aufgenommen oder erstellt hast.
Alles in diesem Repository erscheint unter der MIT-Lizenz, die jedem das Recht einräumt, den
Inhalt zu verändern und weiterzugeben — ein Recht, das niemand an der Produktfotografie eines
Tastaturherstellers einräumen kann. Siehe [NOTICE.md](../../NOTICE.md).

## Siehe auch

- [Tastatur hinzufügen oder korrigieren](adding-a-keyboard.md) — der praktische Ablauf
