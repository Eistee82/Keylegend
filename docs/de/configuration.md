# Konfiguration

Die Einstellungen liegen unter `%APPDATA%\Keylegend\` und werden über die Oberfläche
bearbeitet. Beim ersten Start wird eine vollständige Standardkonfiguration angelegt.

## Farben

Je eine Farbe pro Kategorie:

| Kategorie | Gilt für |
|---|---|
| Ziffer | `1`, `7` sowie den Ziffernblock bei eingeschaltetem Num-Lock |
| Kleinbuchstabe | `a`, `ö` |
| Großbuchstabe | `A`, `Ö` |
| Sonderzeichen | `+`, `#`, `€`, `\|` sowie die Rechenzeichen des Ziffernblocks |
| Steuertaste | Esc, Tab, Eingabe, Rücktaste, Modifier, Pfeile, Navigationsblock und der Ziffernblock bei ausgeschaltetem Num-Lock |
| Funktionstaste | F1 bis F12 |
| Tote Taste | `^`, `´`, `` ` `` — Tasten, die erst mit einem zweiten Anschlag ein Zeichen erzeugen |
| Ohne Belegung | Tasten ohne Bedeutung im aktuellen Kontext; standardmäßig dunkel. Deutlichstes Beispiel ist die mittlere Taste des Ziffernblocks bei ausgeschaltetem Num-Lock |

Die Lock-Tasten haben je zwei Farben — eine für ein, eine für aus.

## Kürzelsätze

Ein Kürzelsatz ordnet Tasten **Funktionsgruppen** zu und wird über die Menge der gerade
gehaltenen Modifier nachgeschlagen. Mitgeliefert werden: `Windows`, `Windows+Umschalt`,
`Windows+Strg`, `Alt`, `Strg`, `Strg+Umschalt`, `Strg+Alt`.

Jede Gruppe hat eine eigene Farbe, sodass zusammengehörige Befehle als Block erkennbar werden —
etwa Bearbeiten (`X`/`C`/`V`/`Z`/`Y`/`A`) in der einen und Dateibefehle (`N`/`O`/`S`/`P`/`W`) in
einer anderen Farbe.

Windows-Kürzel sind systemweit fest vergeben und daher immer zutreffend. Strg-Kürzel
unterscheiden sich je nach Programm; der mitgelieferte Satz bildet die verbreiteten
Windows-Gepflogenheiten ab.

## Anwendungsprofile

Ein Profil beschreibt, was die Tastatur zeigen soll, solange ein bestimmtes Programm im
Vordergrund ist. Rund neunzig sind mitgeliefert — Programme wie Photoshop, Visual Studio Code
oder Excel und Spiele wie Elden Ring oder Counter-Strike 2. Sie greifen ohne Zutun: Sobald das
zugehörige Fenster den Fokus hat, gilt das Profil, und beim Wechsel des Fokus gelten wieder
die Standardsätze. Greift kein Profil, ändert sich nichts.

Erkannt wird am Namen der ausführbaren Datei. Trifft mehr als ein Profil zu, gewinnt dasjenige,
das das Programm namentlich nennt — ein Spiel mit eigenem Profil behält seines also auch dann,
wenn die Spielerkennung anschlägt. Bei Gleichstand entscheidet die Priorität.

Ein Profil ersetzt nur die Modifier-Ebenen, die es selbst nennt. Photoshop ersetzt die
Strg-Ebene, weil Strg dort andere Befehle bedeutet als sonst; `Windows+E` bleibt trotzdem
„Explorer öffnen“, weil Windows diese Kombination systemweit vergibt und sie unabhängig davon
zutrifft, was gerade im Vordergrund ist.

### Was in einem Profil steht

| Abschnitt | Inhalt |
|---|---|
| Zuordnung | Für welche Programme das Profil gilt: Namen der ausführbaren Dateien, ob es für erkannte Spiele allgemein gilt, und die Priorität |
| Hervorhebungen | Tasten mit fester Farbe, unabhängig vom erzeugten Zeichen — WASD im Spiel, die Werkzeugtasten einer Bildbearbeitung |
| Kürzel | Ersatz für einzelne Modifier-Ebenen: welche Taste unter `Strg` welchen Befehl trägt, nach Funktionsgruppe eingefärbt |

Hervorhebungen und Kürzel tragen zusätzlich eine Beschriftung, die sagt, was der Befehl tut —
„Ebene duplizieren“, „Springen“. Auf der Tastatur ist davon nichts zu sehen, die LEDs zeigen
nur Farbe. Die Beschriftung erscheint in der Vorschau der Anwendung, und sie ist bei neunzig
Profilen der einzige Weg, überhaupt zu prüfen, ob ein Eintrag stimmt.

### Ändern und Zurücksetzen

Die drei Abschnitte werden getrennt überschrieben. Änderst du die Hervorhebungen eines
mitgelieferten Profils, gehören ab da die Hervorhebungen dir: Sie sind eingefroren und folgen
der mitgelieferten Fassung nicht mehr. Zuordnung und Kürzel folgen weiter und übernehmen die
Verbesserungen, die eine neue Fassung von Keylegend mitbringt.

Gespeichert wird dabei nur der geänderte Abschnitt, abgelegt unter der Kennung des Profils —
nie eine Kopie des ganzen Profils. Genau deshalb gibt es ein Zurücksetzen, und deshalb kann ein
Update ein Profil verbessern, das du teilweise bearbeitet hast.

Zurückgesetzt wird entsprechend je Abschnitt: nur die Kürzel zurückgeben und die eigenen
Hervorhebungen behalten ist möglich. Das vollständige Zurücksetzen nimmt alle Abschnitte
zurück, dazu einen geänderten Namen und ein Ausblenden.

Mitgelieferte Profile lassen sich **ausblenden, aber nicht löschen**. Sie stecken in der
Programmdatei; ein Löschen hielte nur bis zum nächsten Start. Ein ausgeblendetes Profil wird
bei der Auswahl übergangen, bleibt aber in der Liste und lässt sich wieder einblenden.

### Eigene Profile

Ein selbst angelegtes Profil wird vollständig in `settings.json` gespeichert, denn es gibt
nichts, wogegen es sich vergleichen ließe. Es lässt sich daher nicht zurücksetzen, nur löschen.
Ansonsten verhält es sich wie ein mitgeliefertes: dieselben drei Abschnitte, dieselbe
Auswahlregel.

Soll ein Profil nicht nur bei dir gelten, sondern für alle mitgeliefert werden, gehört es als
Datei ins Projekt — siehe [Profil hinzufügen](adding-a-profile.md).

### Format der Einstellungsdatei

`settings.json` trägt `formatVersion` 2. Ältere Dateien werden beim Laden umgestellt: Version 1
kannte weder Kennungen noch die Herkunft eines Profils und kann daher nicht sagen, welche ihrer
Einträge einmal mitgeliefert waren. Alle werden deshalb zu eigenen Profilen. Es geht nichts
verloren, aber die mitgelieferten Profile stehen daneben, sodass es zunächst zwei Einträge für
dasselbe Programm geben kann; den überzähligen kannst du löschen oder ausblenden.

## Verhalten

| Einstellung | Bedeutung |
|---|---|
| Beleuchtung bei Ruhe zurückgeben | Ob überhaupt zurückgegeben wird. Ausgeschaltet behält Keylegend die Tastatur, bis Sie pausieren oder das Programm schließen — und übernimmt sie dann schon beim Start, ohne auf einen Tastendruck zu warten. |
| Ruhezeit | Sekunden ohne Tastaturaktivität, bis zurückgegeben wird. Vorgabe 60 — das Zurückholen kostet ein bis zwei Sekunden, eine kurze Ruhezeit macht daraus eine ständige Unterbrechung. Der Wert bleibt erhalten, während die Rückgabe ausgeschaltet ist. |
| Helligkeit | Globaler Faktor von 0 bis 100 %, der beim Erzeugen des Farbbilds auf alle Farben angewandt wird. |
| Anwendungsprofile verwenden | Ob Profile überhaupt herangezogen werden. Ausgeschaltet gelten überall die Standardsätze, unabhängig davon, was im Vordergrund ist. |
| Mit Windows starten | Trägt die Anwendung im `Run`-Schlüssel ein, mit dem Schalter `--minimized`. So gestartet kommt Keylegend im Infobereich hoch: kein Fenster, keine Sprechblase. Von Hand gestartet zeigt es sein Fenster wie gewohnt. Ein Eintrag aus einer früheren Fassung wird beim nächsten Start aufgefrischt. |

## Sprache

Die Oberfläche folgt der Anzeigesprache von Windows und ist in elf Sprachen verfügbar: Deutsch,
Englisch, Spanisch, Französisch, Italienisch, Niederländisch, Polnisch, Portugiesisch, Russisch,
Ukrainisch und vereinfachtes Chinesisch. Unter **Einstellungen → Sprache** lässt sich das
übersteuern; die Umschaltung wirkt sofort, ein Neustart ist nicht nötig.

Jede Sprache nennt sich in dieser Liste selbst, statt übersetzt zu werden. Sie zu übersetzen
hieße, dass jede der elf zehn Namen für die anderen mitführt — und wer eine Oberfläche in einer
Sprache vorfindet, die er nicht lesen kann, müsste seine eigene in einer Sprache suchen, die er
ebenfalls nicht lesen kann.

Gespeichert wird die Wahl in `settings.json` unter `language` als `Automatic`, `English`,
`German`, `Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`,
`Ukrainian` oder `ChineseSimplified`. Ein unbekannter Wert fällt auf `Automatic` zurück, statt
den Start zu verweigern — was eine von Hand bearbeitete Datei ohnehin am ehesten will.

Was übersetzt ist, sind die Menüs und Erklärungen. **Nicht** übersetzt sind zwei Dinge, und
beides mit Absicht:

- **Die Tastenbeschriftungen** auf der abgebildeten Tastatur. Sie stammen aus dem Geräteprofil
  und müssen zu der Tastatur passen, die vor Ihnen steht — nicht zur Sprache der Menüs. Eine
  deutsche ISO-Tastatur zeigt `strg` und `entf`, gleichgültig ob die Oberfläche englisch läuft.
- **Die Modifier-Namen** (Shift, Ctrl, Alt, AltGr, Num Lock …). Dieselben Namen erzeugt die
  Kürzelverwaltung für die Ebenenlisten, und die liegt außerhalb der Übersetzung; halb
  übersetzt läse sich schlechter als durchgehend englisch.

Fehlt eine Übersetzung, erscheint der englische Text. Eine unvollständige Sprachdatei kostet
also die betroffenen Zeilen, nicht die ganze Oberfläche.

## Kalibrierung

Die Kalibrierung ist ein Modus der Kommandozeile, keine Einstellungsseite:

```bash
keylegend-cli --profile devices/<ordner>/device.json --calibrate
```

Sie lässt eine Taste nach der anderen aufleuchten und benennt sie, damit ein Geräteprofil an
echter Hardware überprüft werden kann. Die Befunde werden laufend in `calibration-findings.txt`
geschrieben, und `tools/apply-calibration.ps1` trägt sie zurück ins Profil. Siehe
[Tastatur hinzufügen oder korrigieren](adding-a-keyboard.md).
