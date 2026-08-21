# Tastatur hinzufügen oder korrigieren

Unterstützung für eine Tastatur ist **Daten, kein Code**. Du brauchst kein C# und keine
Build-Werkzeuge — ein Texteditor und deine eigene Tastatur genügen.

Die meisten, die hier landen, müssen gar nichts hinzufügen: für ihr Layout gibt es bereits ein
Profil. Was diesen Profilen fehlt, ist das Einzige, was sich nicht erzeugen lässt — jemand mit
der Hardware, der bestätigt, dass jede Taste dort leuchtet, wo das Profil es behauptet. **Das ist
die Aufgabe in [Teil 2](#2-ein-profil-korrigieren), und sie dauert etwa zehn Minuten.**

---

## Was ein Profil weiß, und wie sicher es das weiß

Ein Profil beantwortet zwei Fragen, und die beiden sind unterschiedlich verlässlich:

| Frage | Woher die Antwort kommt | Wie sicher |
|---|---|---|
| Wo sitzt jede Taste, und wie groß ist sie? | Das genormte 19,05-mm-Raster, dem jede Tastatur seit der IBM Model M folgt | **Sicher.** Die Geometrie folgt aus dem Layout. |
| Welche Zelle der LED-Matrix beleuchtet diese Taste? | Die veröffentlichte Matrix des Herstellers, unter der Annahme einer Standardtastatur | **Eine Vermutung.** Modelle verschieben Tasten, bestücken Zellen nicht und fügen eigene hinzu. |

Diese Trennung ist der ganze Grund für das Feld `verified`. Ein Profil mit `"verified": false`
hat mit ziemlicher Sicherheit das richtige Bild und möglicherweise die falsche Taste.

---

## 1. Ein fehlendes Layout ergänzen

Prüfe zuerst, ob es wirklich fehlt: In `devices/` liegen bereits Vollformat-Profile für ANSI-US,
ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP und
ABNT2-BR, dazu Tenkeyless-, 75-%-, 65-%- und 60-%-Varianten. Ist deins dabei, geh zu Teil 2.

### Der erzeugte Weg

`tools/make-layout.py` baut ein Profil aus den genormten Maßen. Eine Tastatur hinzuzufügen ist
ein Eintrag in der Liste `PROFILES` am Ende der Datei:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argument | Was es festlegt |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` oder `abnt2` — die Form der Eingabetaste und welche Zusatztasten es gibt |
| `legends` | Welcher Satz aufgedruckter Tastenbeschriftungen gilt: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` oder `fn` — was zwischen rechtem Alt und Kontextmenütaste sitzt |

Dann ausführen:

```bash
python tools/make-layout.py --only iso-tr
```

Ist die Beschriftung deiner Tastatur nicht unter den fünf Sätzen, ergänze einen: `LEGENDS_EN` in
derselben Datei kopieren, die Einträge übersetzen, in `LEGEND_SETS` eintragen. Nur Tasten, die
*nichts* schreiben, brauchen eine Beschriftung — die übrigen werden zur Laufzeit bei Windows
erfragt, und genau das lässt ein Profil jedes Software-Layout auf derselben Hardware bedienen.

### Der handgeschriebene Weg

Für eine Tastatur, die keine Abwandlung eines Standardlayouts ist — eine ortholineare, eine
geteilte, eine mit einer Makroreihe, die sonst niemand hat — schreibst du `device.json` direkt.
Die [Formatbeschreibung](device-profile-format.md) listet jedes Feld auf, und
`devices/device-profile.schema.json` gibt den meisten Editoren Vervollständigung und Fehler
direkt beim Tippen.

Der erste Durchgang muss nicht genau sein. Setz die Tasten ungefähr richtig, lass `row` und
`column` überall dort auf `null`, wo du unsicher bist, und überlass den Rest der Kalibrierung.

---

## 2. Ein Profil korrigieren

Das ist der Teil, der die Hardware braucht — und der Teil, auf den es ankommt.

### Erst hinsehen

Bevor du die Tastatur anfasst, sieh dir das Bild an:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-fr/device.json
```

Das schreibt `preview.svg` neben das Profil; öffne es in einem beliebigen Browser. Vergleiche es
mit der Tastatur vor dir und achte auf:

- fehlende Tasten, oder gezeichnete Tasten, die deine Tastatur nicht hat
- eine Eingabetaste in der falschen Form — hoch und L-förmig bei ISO, breit und flach bei ANSI
- eine untere Reihe mit der falschen Zahl an Modifikatoren, die stärker variiert als alles andere
- **rote Umrandungen**. Sie markieren Tasten ohne Matrixzelle: die werden nie leuchten.

Geometrie zu korrigieren ist Rechnen, kein Raten: Das Raster ist eine Einheit pro Taste, und eine
Einheit ist die `width`, die die gewöhnlichen Buchstabentasten haben.

### Dann kalibrieren

Die Kalibrierung leuchtet eine Taste nach der anderen an und benennt sie, damit du bestätigen
kannst, dass die weiß leuchtende Taste die ist, die das Profil behauptet. Nur so lässt sich
Gewissheit erreichen; alles andere ist Rückschluss aus einer Herstellertabelle.

```bash
keylegend-cli --profile devices/<dein-ordner>/device.json --calibrate
```

Sie geht die zugeordneten Tasten in Leserichtung durch:

| Taste | Wirkung |
|---|---|
| `Enter` oder `→` | stimmt, weiter zur nächsten |
| `F` | die falsche Taste hat geleuchtet — festhalten |
| `←` | eine Taste zurück |
| `A` | alle zugeordneten Tasten gleichzeitig anleuchten |
| `S` | direkt zur Zusammenfassung |
| `Q` oder `Esc` | abbrechen |

Weil die Tastenkennungen dem US-Layout folgen, zeigt die Anzeige zusätzlich, was die Taste auf
*deinem* Rechner tatsächlich schreibt — auf einer deutschen Tastatur heißt es also „die
ß-Taste" und nicht `Keyboard_MinusAndUnderscore`.

Die Befunde werden laufend in `calibration-findings.txt` geschrieben, nicht erst am Ende.
Kalibrieren ist geduldige Arbeit, und ein geschlossenes Fenster darf sie nicht kosten.

Beim Durchgehen hilft ein zweites Bild — es beschriftet jede Taste mit der Zelle, die sie
beansprucht, statt mit ihrer Aufschrift:

```bash
python tools/preview-layout.py devices/<dein-ordner>/device.json --cells
```

### Befunde übernehmen

`tools/apply-calibration.ps1` schreibt sie zurück und legt eine `.bak`-Kopie an:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<dein-ordner>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` ist für Tasten, bei denen gar nichts aufleuchtete: Die Matrix kann die Zelle ansprechen,
dieses Modell hat dort aber keine LED. Solche Tasten behalten ihre Geometrie — die Taste
existiert ja, und die Vorschau soll sie zeichnen — und verlieren `row`/`column`, damit nichts ins
Leere geschickt wird. `-Remap` ist für Tasten, die auf der falschen Zelle liegen.

### Womit zu rechnen ist

An diesen Stellen liegt ein erzeugtes Profil am häufigsten daneben:

| Wo | Was passiert |
|---|---|
| **Die ISO-Eingabetaste** | Sie erstreckt sich über zwei Zellen. Bei vielen Tastaturen ist nur die untere mit einer LED bestückt, die obere Hälfte leuchtet über die Nachbarzelle oder gar nicht. |
| **Die untere Reihe** | Zahl und Breite der Modifikatoren unterscheiden sich je Modell. Spieltastaturen setzen `Fn` dorthin, wo Bürotastaturen eine zweite Windows-Taste haben. |
| **Makro- und Medientasten** | Oft auf Spalte 0 oder auf den äußeren Spalten — und oft auf gar keiner Zelle. |
| **Kompakte Tastaturen** | Die Matrix behält ihre vollen 6 × 22; eine 60-%-Tastatur lässt schlicht den größten Teil leer. Die Zellen werden nicht neu nummeriert. |
| **Die hohen Tasten des Ziffernblocks** | Plus und Enter überdecken zwei Reihen, hören aber auf eine Zelle — meist die obere. |

Eine Taste ohne LED behält ihre Geometrie und verliert ihre Zelle:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Sie wird weiterhin gezeichnet, damit die Vorschau zur Hardware passt; sie leuchtet nur nie. Das
ist richtig so und kein Mangel.

### Deine Tastatur erkennbar machen

Keylegend fragt Windows, welche Tastaturen angeschlossen sind, und nimmt das Profil, dessen
`usb`-Kennungen passen. Das ist der Unterschied zwischen Finden und Raten — und bei über dreißig
mitgelieferten Profilen ist Raten wenig wert.

Deine Kennungen findest du so:

```powershell
Get-PnpDevice -Class Keyboard | Select-Object FriendlyName, InstanceId
```

Die Instanzkennung lautet dann etwa `HID\VID_1532&PID_0295&MI_01\...`. Die vier Hexziffern nach
`VID_` und `PID_` kommen ins Profil:

```jsonc
"usb": { "vendorId": "1532", "productId": "0295" }
```

Ein Hersteller verwendet dieselbe Produktkennung über alle Layouts hinweg, deshalb tragen das
ISO- und das ANSI-Profil eines Modells dasselbe Paar. Welches davon gilt, entscheidet dann das
Tastaturlayout, unter dem Windows läuft — ein Hinweis, keine Gewissheit, und nur zum Auflösen
genau dieses Gleichstands.

Das Feld ist optional. Ohne es funktioniert ein Profil weiterhin; es muss dann nur ausgewählt
statt gefunden werden.

### Als geprüft markieren

Wenn jede Zelle stimmt, gib demselben Skript `-MarkVerified` mit oder setz `"verified": true` von
Hand, und entferne den `note`-Hinweis, dass das Profil erzeugt wurde. Dieses Kennzeichen sagt der
nächsten Person mit deiner Tastatur, dass sie sich darauf verlassen kann.

---

## 3. Testen

```bash
dotnet test
```

Die Tests der mitgelieferten Profile prüfen jedes Profil unter `devices/`, auch deins. Sie fangen
doppelte Kennungen ab, zwei Tasten auf derselben LED, übereinander gezeichnete Tasten, Zellen
außerhalb der Matrix und Geometrie, die von der Zeichenfläche gerutscht ist.

## 4. Pull Request öffnen

Schreib dazu, welche Tastatur und welches physische Layout du geprüft hast und ob du die
Kalibrierung durchlaufen bist. Siehe [CONTRIBUTING.md](../../CONTRIBUTING.md).

Profile mit `"verified": false` sind ebenfalls willkommen — sie geben der nächsten Person mit
dieser Tastatur einen Vorsprung. Eine Korrektur an einem bestehenden Profil ist genauso viel wert
wie ein neues.

### Zu Bildern

Das Feld `image` ist optional und wird derzeit nicht verwendet: Die Vorschau wird aus der
Geometrie gezeichnet, bleibt dadurch in jeder Größe scharf und kann dem Profil nicht
widersprechen. Wenn du doch ein Bild beilegst, muss es eines sein, das **du** fotografiert oder
gezeichnet hast. Ein Produktrender des Herstellers lässt sich nicht unter der MIT-Lizenz dieses
Projekts veröffentlichen, und ein Pull Request mit einem solchen Bild wird gebeten, es zu
entfernen.

## Siehe auch

- [Geräteprofil-Format](device-profile-format.md) — jedes Feld im Detail
- [Architektur](architecture.md) — warum die Tastenbedeutung von Windows kommt und nicht aus einer Tabelle
