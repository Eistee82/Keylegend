# Apparaatprofielformaat

Een apparaatprofiel beschrijft één toetsenbordmodel in één fysieke indeling. Het is één bestand in
een map onder `devices/`, genoemd `<fabrikant>-<model>-<indeling>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometrie en ledkoppeling
```

`devices/device-profile.schema.json` beschrijft hetzelfde in machineleesbare vorm. Het noemen in
een `$schema`-regel, zoals de meegeleverde profielen doen, geeft de meeste editors aanvulling en
inline fouten terwijl je typt.

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

| Veld | Betekenis |
|---|---|
| `formatVersion` | Formaatversie. Momenteel `1`. Een build weigert een profiel met een hoger nummer dan hij begrijpt. |
| `name` | Wat de interface toont. |
| `vendor`, `model` | Wie het maakt en welk model. `"Generic"` voor een profiel dat een indeling beschrijft in plaats van een product. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — de fysieke *opstelling* van de toetsen, niet de software-indeling. |
| `canvas` | Het coördinatenstelsel waarnaar alle toetsposities verwijzen. Alleen verhoudingen tellen; de meegeleverde profielen rekenen in millimeters. |
| `matrix` | Grootte van de ledmatrix van de fabrikant. Razer-toetsenborden zijn 6 × 22, ongeacht hun formaat. |
| `verified` | `true` zodra iemand de koppeling op echte hardware heeft bevestigd. |
| `note` | Optionele vrije tekst voor wie het bestand hierna opent. |
| `image` | Optioneel, en op dit moment ongebruikt — zie [Afbeeldingen](#afbeeldingen) hieronder. |
| `keys[]` | Eén item per toets. |

### Fysieke indeling, niet software-indeling

`physicalLayout` bepaalt de *vorm* van het toetsenbord: of de Enter hoog en L-vormig is, of er een
extra toets links van de `Z` zit, of de onderste rij Japanse conversietoetsen draagt.

Het zegt niets over welke tekens die toetsen opleveren. Dat vraagt Keylegend tijdens de uitvoering
aan Windows, voor de op dat moment actieve indeling. Eén ISO-indelingsprofiel bedient dus een
toetsenbord of Windows nu op Nederlands, Amerikaans, Dvorak of iets anders staat — vandaar één
profiel per *fysieke* indeling en niet één per taal.

### Toetsitems

| Veld | Betekenis |
|---|---|
| `id` | Unieke aanduiding. Volg de bestaande naamgeving: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Positie van de linkerbovenhoek op het vlak. |
| `width`, `height` | Grootte van de toets op het vlak. |
| `row`, `column` | Cel in de ledmatrix van de fabrikant. Beide `null` zolang onbekend — een geldige toestand, en waar de kalibratie voor is. |
| `scanCode` | Vervangt de standaardscancode. Alleen nodig waar de fysieke indeling de Amerikaanse naamgeving tegenspreekt. |
| `parts` | Verdere rechthoeken van dezelfde toets, voor toetsen die niet rechthoekig zijn. |
| `label` | Wat er op de toets gedrukt staat, voor toetsen die niets typen. |
| `labelSecondary` | Een tweede gedrukte regel, onder de eerste. |

### Opschriften horen bij het toetsenbord

`label` is wat er *op de toets gedrukt* staat, geen vertaling van wat de toets doet. Een Duits
toetsenbord zegt `strg`, een Frans `ctrl`, een Italiaans `bloc maiusc` — en elk zegt dat ongeacht
op welke taal de menu's van Keylegend staan. De interfacetaal wijzigen verandert de opschriften
nooit.

Toetsen die een teken opleveren dragen helemaal geen `label`. Hun opschrift komt uit de actieve
Windows-indeling en volgt daarmee vanzelf Shift, Caps Lock en AltGr.

### Toetsen met meer dan één rechthoek

De ISO-Enter is het standaardgeval: één toets over twee rijen.

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

De hoofdrechthoek draagt de cel; `parts` voegt de rest van de vorm toe. De expliciete `scanCode`
staat er omdat de bovenhelft de positie inneemt die ANSI voor de backslash gebruikt: zonder hem zou
de bovenkant van de Enter worden gekleurd alsof hij `\` typte.

### Scancodes voor toetsen die maar op één indeling bestaan

De standaardtabel in `Keylegend.Core` dekt wat een Amerikaans toetsenbord heeft. Toetsen die
alleen elders bestaan noemen hun code in het profiel, zodat er voor een indeling geen C# hoeft te
veranderen:

| Aanduiding | Toets | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, links van Backspace op JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, rechts van de rechter Shift op JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, links van de spatiebalk | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, rechts van de spatiebalk | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | de `/?`-toets rechts van de rechter Shift op ABNT-2 | `0x73` |

## Regels die de validator afdwingt

Ze worden in de continue integratie gecontroleerd, dus een profiel dat ze schendt kan niet worden
samengevoegd:

- Toets-id's zijn uniek
- Geen twee toetsen eisen dezelfde matrixcel op
- Geen twee toetsen overlappen op het vlak
- `row` en `column` zijn beide ingevuld of beide `null`
- Cellen liggen binnen de opgegeven matrix
- Toetsen liggen binnen het vlak
- Elke toets heeft een positieve grootte
- Een door `image` genoemde afbeelding bestaat echt

## Naamgeving en het verschil ISO/ANSI

De toets-id's volgen de Amerikaanse indeling, omdat de matrix van de fabrikant dat ook doet. Op
een Duits toetsenbord zit de fysieke `Z` daarom op `Keyboard_Y` en omgekeerd. Dit betreft alleen
de naam: noch positie noch gedrag hangen ervan af, want het werkelijke teken wordt tijdens de
uitvoering aan Windows gevraagd.

Twee id's bestaan alleen op ISO-toetsenborden:

| Aanduiding | Toets | Razer-cel |
|---|---|---|
| `Keyboard_NonUsBackslash` | de extra toets links van `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, rij 4 kolom 2 |
| `Keyboard_NonUsTilde` | de toets naast de Enter in de middelste rij (`#`, `'`) | `RZKEY_EUR_1`, rij 3 kolom 13 |

Op ISO-toetsenborden beslaat de hoge Enter twee matrixposities: de bovenhelft waar ANSI de
backslash heeft (rij 2, kolom 14), de onderhelft op `Keyboard_Enter` (rij 3, kolom 14).

**Of ze allebei werkelijk oplichten hangt van het model af.** De tabel van de fabrikant beschrijft
wat de matrix kan *adresseren*, niet wat een bepaald toetsenbord heeft *gemonteerd*. Op de
DeathStalker V2 bleek bij de kalibratie dat de bovenste cel helemaal geen led aanstuurt — de hele
Enter wordt door de onderste verlicht, en daarom modelleert het meegeleverde profiel de Enter als
één toets met twee rechthoeken in plaats van als twee toetsen.

Dit is precies het soort ding dat uit geen enkele documentatie is af te leiden, en de reden dat een
profiel niet `verified` zou moeten heten tot iemand het op hardware is doorgelopen.

## Afbeeldingen

`image` is optioneel en wordt op dit moment niet gebruikt: de voorvertoning op het scherm wordt uit
de geometrie hierboven getekend. Tekenen houdt de voorvertoning bij elk venstergrootte scherp en
maakt het onmogelijk dat afbeelding en profiel elkaar tegenspreken.

Voeg je er toch een toe, dan moet het een afbeelding zijn die **jij** hebt gemaakt of gefotografeerd.
Deze hele repository verschijnt onder de MIT-licentie, die iedereen het recht geeft de inhoud te
wijzigen en te verspreiden — een recht dat niemand kan verlenen over de productfotografie van een
toetsenbordfabrikant. Zie [NOTICE.md](../../NOTICE.md).

## Zie ook

- [Een toetsenbord toevoegen of corrigeren](adding-a-keyboard.md) — het praktische stappenplan
