# Een toetsenbord toevoegen of corrigeren

Ondersteuning voor een toetsenbord is **data, geen code**. Je hebt geen C# en geen buildgereedschap
nodig — een teksteditor en je eigen toetsenbord volstaan.

De meesten die hier terechtkomen hoeven niets toe te voegen, want voor hun indeling bestaat al een
profiel. Wat die profielen missen is het enige dat niet te genereren valt: iemand die met de
hardware erbij bevestigt dat elke toets oplicht waar het profiel beweert. **Dat is het werk uit
[deel 2](#2-een-profiel-corrigeren), en het kost ongeveer tien minuten.**

---

## Wat een profiel weet, en hoe zeker het dat weet

Een profiel beantwoordt twee losse vragen, en ze zijn niet even betrouwbaar:

| Vraag | Waar het antwoord vandaan komt | Hoe zeker |
|---|---|---|
| Waar zit elke toets, en hoe groot is die? | Het genormeerde raster van 19,05 mm, dat elk toetsenbord sinds de IBM Model M volgt | **Zeker.** De geometrie volgt uit de indeling. |
| Welke cel van de ledmatrix laat die toets oplichten? | De door de fabrikant gepubliceerde matrix, uitgaand van een standaardbord | **Een gok.** Modellen verplaatsen toetsen, laten cellen onbezet en voegen eigen toe. |

Die scheiding is de hele reden dat de vlag `verified` bestaat. Een profiel met
`"verified": false` heeft vrijwel zeker gelijk over het plaatje en kan er heel goed naast zitten
over welke toets oplicht.

---

## 1. Een ontbrekende indeling toevoegen

Controleer eerst of die echt ontbreekt: `devices/` bevat al volledige profielen voor ANSI-US,
ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP en
ABNT2-BR, plus tenkeyless-, 75 %-, 65 %- en 60 %-varianten. Zit de jouwe erbij, ga dan naar deel 2.

### De gegenereerde weg

`tools/make-layout.py` bouwt een profiel uit de genormeerde afmetingen. Er een toetsenbord aan
toevoegen is één regel in de lijst `PROFILES`, onderaan het bestand:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argument | Wat het bepaalt |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` of `abnt2` — de vorm van de Enter en welke extra toetsen er zijn |
| `legends` | Welke set gedrukte opschriften wordt gebruikt: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` of `fn` — wat er tussen de rechter Alt en de menutoets zit |

Voer het daarna uit:

```bash
python tools/make-layout.py --only iso-tr
```

Staan de opschriften van jouw toetsenbord niet tussen de vijf sets, voeg er dan een toe: kopieer
`LEGENDS_EN` in hetzelfde bestand, vertaal de items en registreer hem in `LEGEND_SETS`. Alleen
toetsen die *niets* typen hebben een opschrift nodig — de rest wordt tijdens de uitvoering aan
Windows gevraagd, en dat is wat één profiel elke software-indeling op dezelfde hardware laat
bedienen.

### De handgeschreven weg

Voor een toetsenbord dat geen variant op een standaardindeling is — ortholineair, gesplitst, met
een rij macrotoetsen die niemand anders heeft — schrijf je `device.json` rechtstreeks. De
[formaatbeschrijving](device-profile-format.md) somt elk veld op, en
`devices/device-profile.schema.json` geeft de meeste editors aanvulling en inline fouten.

De eerste ronde hoeft niet exact te zijn. Zet de toetsen ongeveer goed, laat `row` en `column` op
`null` waar je twijfelt, en laat de kalibratie de rest doen.

---

## 2. Een profiel corrigeren

Dit is het deel dat de hardware nodig heeft, en het deel waar het echt om gaat.

### Kijk er eerst naar

Voordat je het toetsenbord aanraakt: bekijk het plaatje.

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-de/device.json
```

Dat schrijft `preview.svg` naast het profiel; open het in een willekeurige browser. Vergelijk het
met het toetsenbord voor je en let op:

- ontbrekende toetsen, of getekende toetsen die jouw toetsenbord niet heeft
- een Enter met de verkeerde vorm — hoog en L-vormig bij ISO, breed en plat bij ANSI
- een onderste rij met het verkeerde aantal modificatietoetsen, wat sterker varieert dan al het
  andere
- **rode omlijningen**, die toetsen zonder matrixcel markeren. Die zullen nooit oplichten.

Geometrie corrigeren is rekenen, geen gissen: het raster is één eenheid per toets, en een eenheid
is de `width` die de gewone lettertoetsen hebben.

### Kalibreer daarna

De kalibratie laat één toets tegelijk oplichten en noemt hem, zodat je kunt bevestigen dat de
toets die wit oplicht de toets is die het profiel beweert. Alleen zo kom je tot zekerheid; al het
andere is gevolgtrekking uit een fabrikantentabel.

```bash
keylegend-cli --profile devices/<jouw-map>/device.json --calibrate
```

Hij loopt de toegewezen toetsen in leesvolgorde af:

| Toets | Wat die doet |
|---|---|
| `Enter` of `→` | deze klopt, door naar de volgende |
| `F` | de verkeerde toets lichtte op — vastleggen |
| `←` | één toets terug |
| `A` | alle toegewezen toetsen tegelijk laten oplichten |
| `S` | door naar de samenvatting |
| `Q` of `Esc` | stoppen |

Omdat de toets-id's de Amerikaanse indeling volgen, toont de aanwijzing ook wat elke toets op
*jouw* machine daadwerkelijk typt — op een Belgisch of Duits toetsenbord hoor je dus «de ß-toets»
en niet `Keyboard_MinusAndUnderscore`.

Bevindingen worden gaandeweg naar `calibration-findings.txt` geschreven, niet pas aan het eind.
Kalibreren is geduldig werk en een gesloten venster mag je dat niet kosten.

Tijdens het werk helpt een tweede plaatje — dit labelt elke toets met de cel die hij opeist in
plaats van met zijn opschrift:

```bash
python tools/preview-layout.py devices/<jouw-map>/device.json --cells
```

### Pas toe wat je gevonden hebt

`tools/apply-calibration.ps1` schrijft het terug in het profiel en houdt een `.bak`-kopie:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<jouw-map>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` is voor toetsen die helemaal niets lieten oplichten: de matrix kan de cel adresseren, maar
dit model heeft daar geen led. Zulke toetsen behouden hun geometrie — de toets bestaat, en de
voorvertoning moet hem tekenen — en verliezen hun `row`/`column`, zodat er niets de leegte in
wordt gestuurd. `-Remap` is voor toetsen die aan de verkeerde cel hangen.

### Waar je op moet rekenen

Dit zijn de plekken waar een gegenereerd profiel het vaakst mis heeft:

| Waar | Wat er gebeurt |
|---|---|
| **De ISO-Enter** | Hij beslaat twee cellen. Op veel toetsenborden is alleen de onderste van een led voorzien, en wordt de bovenhelft door de buur verlicht of helemaal niet. |
| **De onderste rij** | Aantal en breedte van de modificatietoetsen verschillen per model. Gametoetsenborden zetten `Fn` waar kantoortoetsenborden een tweede Windows-toets hebben. |
| **Macro- en mediatoetsen** | Vaak op kolom 0 of op de buitenste kolommen, en vaak op helemaal geen cel. |
| **Compacte toetsenborden** | De matrix houdt zijn volle 6 × 22; een 60 %-bord laat er simpelweg het grootste deel van leeg. Cellen worden niet hernummerd. |
| **De hoge toetsen van het numerieke blok** | Plus en Enter beslaan twee rijen maar luisteren naar één cel, meestal de bovenste. |

Een toets die geen led blijkt te hebben behoudt zijn geometrie en verliest zijn cel:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Hij wordt nog steeds getekend, zodat de voorvertoning bij de hardware past; hij licht alleen nooit
op. Dat is correct, geen gebrek.

### Markeer het als geverifieerd

Als elke cel klopt, geef hetzelfde script `-MarkVerified` mee, of zet `"verified": true` met de
hand, en haal de `note` weg die zegt dat het profiel gegenereerd is. Die vlag vertelt de volgende
persoon met jouw toetsenbord dat erop te vertrouwen valt.

---

## 3. Test het

```bash
dotnet test
```

De tests van de meegeleverde profielen valideren elk profiel onder `devices/`, ook het jouwe. Ze
vangen dubbele id's, twee toetsen die dezelfde led opeisen, toetsen die over elkaar heen zijn
getekend, cellen buiten de matrix en geometrie die van het vlak is gegleden.

## 4. Open een pull request

Vermeld welk toetsenbord en welke fysieke indeling je hebt gecontroleerd, en of je de kalibratie
hebt doorlopen. Zie [CONTRIBUTING.md](../../CONTRIBUTING.md).

Profielen met `"verified": false` zijn ook welkom — ze geven de volgende persoon met dat
toetsenbord een voorsprong. Een correctie op een bestaand profiel is net zoveel waard als een
nieuw profiel.

### Over afbeeldingen

Het veld `image` is optioneel en wordt op dit moment niet gebruikt: de voorvertoning wordt uit de
geometrie getekend, waardoor die op elk formaat scherp blijft en het profiel niet kan tegenspreken.
Voeg je er toch een toe, dan moet het er een zijn die **jij** hebt gefotografeerd of getekend. Een
productrender van een fabrikant kan niet onder de MIT-licentie van dit project worden uitgebracht,
en aan een pull request met zo'n render wordt gevraagd hem te verwijderen.

## Zie ook

- [Apparaatprofielformaat](device-profile-format.md) — elk veld, in detail
- [Architectuur](architecture.md) — waarom de betekenis van toetsen van Windows komt en niet uit een tabel
