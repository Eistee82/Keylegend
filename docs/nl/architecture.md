# Architectuur

## Het centrale idee

De hele beslislogica is een **zuivere berekening**, zonder toegang tot Windows, het netwerk of het
bestandssysteem:

```
(toetsenbordtoestand, aangesloten toetsenbord, toepassingsprofiel, kleurinstellingen) → kleur per toets
```

Daaruit volgen twee dingen, en beide verklaren waarom het ontwerp deze vorm heeft:

1. De voorvertoning op het scherm en het echte toetsenbord worden door **dezelfde code** gevuld.
   Wat je in het venster ziet, is wat oplicht.
2. De logica is volledig te testen zonder aangesloten toetsenbord en zonder geïnstalleerde
   Synapse.

Alles wat met de buitenwereld praat, zit in dunne adapters rond die kern.

## Projecten

| Project | Bevat | Mag afhangen van |
|---|---|---|
| `Keylegend.Core` | het aangesloten toetsenbord, categorieën, sneltoetssets, de beeldsamensteller, de toestandsmachine van de sessie | niets platformspecifieks |
| `Keylegend.Windows` | toetsenbordtoestand, tekenresolutie, voorgrondvenster | Windows-API's |
| `Keylegend.Chroma` | REST-client voor de Chroma SDK, hartslag | netwerk |
| `Keylegend.Engine` | de lus die het toetsenbord leest, een beeld samenstelt en verstuurt | Core, Chroma, Windows |
| `Keylegend.App` | WPF-interface, systeemvakpictogram, opslag van de configuratie | al het bovenstaande |

`Keylegend.Core` mag nooit naar de andere verwijzen. Als een wijziging dat lijkt te vereisen, zit
de abstractie op de verkeerde plaats.

## De toetsenbordtoestand lezen

Keylegend installeert **geen** globale toetsenbordhook. Zo'n hook is functioneel een keylogger,
zit in de invoerketen en wordt geregeld door anti-cheatsystemen aangemerkt.

In plaats daarvan wordt de toestand van de relevante toetsen ongeveer zestig keer per seconde
opgevraagd (`GetAsyncKeyState` voor ingedrukte modificatietoetsen, `GetKeyState` voor
vergrendelingen), en wordt er alleen een nieuw beeld samengesteld als er iets veranderd is. Geen
enkele toetsaanslag wordt ooit onderschept, doorgestuurd, gelogd of bewaard.

Met een gekozen typ-effect wordt dezelfde peiling doorgetrokken tot de toetsen die het aangesloten
bord meldt, in plaats van bij de modificatietoetsen te stoppen. Het is dezelfde vraag aan meer
toetsen — is deze op dit moment ingedrukt — en zij wordt alleen gesteld zolang een effect gekozen
is; zonder effect wordt naar de afzonderlijke toetsen nooit gekeken. Wat ervan blijft is weinig en
niet blijvend: `KeyActivity` houdt bij wanneer elke toets omlaag en weer omhoog ging, en vergeet een
toets die niemand sinds seconden heeft aangeraakt. De ene uitzondering is het hitte-effect, dat per
toets een afnemend getal bijhoudt zolang zij afkoelt — een spoor van het getypte in het geheugen,
nergens geschreven en met het proces voorbij.

### Linker- en rechtermodificatietoetsen

Windows meldt **AltGr als Ctrl plus rechter Alt**, en op Duitse indelingen levert Ctrl + linker Alt
dezelfde tekens op als AltGr. Ze worden onderscheiden door de zijde:

- **rechter Alt** → AltGr-laag, die de tekentoewijzing toont
- **Ctrl + linker Alt** → de sneltoetsset `Ctrl+Alt`

Linker- en rechtervarianten moeten dus apart worden beoordeeld (`VK_LMENU`/`VK_RMENU`, enzovoort).

## Bepalen wat een toets betekent

In plaats van een tabel met indelingen mee te leveren, vraagt Keylegend aan Windows welk teken een
toets in de huidige toetsenbordtoestand zou opleveren (`ToUnicodeEx`), en leidt de categorie af uit
het verkregen teken.

Daarom hebben Shift, Caps Lock en Num Lock geen bijzondere behandeling nodig: dezelfde toets
levert eenvoudigweg `A` op in plaats van `a` en belandt vanzelf in de categorie «hoofdletter». En
daarom werkt ook elke toetsenbordindeling zonder aanpassing.

### Welk toetsenbord is aangesloten

Dat wordt aan Razer Synapse gevraagd, want dat weet het al. Het schrijft een beschrijving van elk
aangesloten apparaat naar `…\Razer Chroma SDK\Devices\<guid>.json`: het model bij naam, de
fysieke indeling als getal, de matrixgrootte en de scancode van elke toets die de hardware
werkelijk heeft. `SdkDeviceDescription` leest dat, en over het toetsenbord wordt niets afgeleid.

Die beschrijving ontstaat wanneer Razers software opkomt en bestaat daarvoor niet, wat bij het
aanmelden een race is die Keylegend kan verliezen: op de machine waarop dit is ontwikkeld verscheen
het bestand vijfennegentig seconden na het opstarten van het systeem, en Keylegends eigen
opstartvermelding ging acht seconden later af. Het zoeken ernaar is daarom geen enkele poging
waarvan het mislukken het programma beëindigt. `AttachedKeyboardSearch` blijft zoeken — vlot zolang
er geen apparaat wordt genoemd, met een groeiende pauze zolang alleen de tekening ontbreekt —, het
pictogram in het systeemvak ontstaat vóór de eerste blik, en de motor wordt gebouwd zodra er een
toetsenbord opduikt.

Hoe het toetsenbord eruitziet komt uit dezelfde installatie. De interface van Synapse is een
webtoepassing, en de tekeningen die het voor een apparaat laadt blijven in zijn cache: rechthoeken
van toetsen met namen, de vorm van de behuizing met het volumewieltje en de mediastrook, en de
contouren van de tekens die op de toetsen gedrukt staan. `SvgLayoutSource` vindt die van het
aangesloten model en de aangesloten indeling exact en niet op vorm, omdat elke tekening naast een
configuratieobject wordt geleverd dat beide noemt.

Alleen maten en contouren worden overgenomen; de kleuren en vormgeving van Razer worden genegeerd,
en niets van dat materiaal wordt naar deze repository gekopieerd.

Het enige wat geen van beide zegt, is bij welke cel van de lichtmatrix een toets hoort. Dat is
`StandardKeyMatrix`, de eigen `RZKEY`-tabel van het protocol, op elk model gelijk.

## Toepassingsprofielen

Een profiel bindt verlichtingsregels aan een programma. Er worden er ongeveer negentig
meegeleverd, en de afwegingen daarachter zijn het waard om te noemen, want geen ervan is het voor de hand liggende antwoord.

### Profielen zijn data, geen code

Dezelfde regel als bij apparaatondersteuning: een profiel toevoegen is een JSON-bestand toevoegen
onder `profiles/`, en de build pikt het met een jokerteken op. Niemand hoeft C# aan te raken om
Keylegend een programma te leren, wat betekent dat een profiel kan worden aangedragen, nagekeken
en gecorrigeerd door iemand die alleen het programma kent. Als het ondersteunen van een nieuwe
toepassing ooit code zou vergen, deugt het formaat niet.

### Ingebed in de assembly in plaats van los op schijf

Toepassingsprofielen worden in de assembly gecompileerd in plaats van als bestanden naast het
uitvoerbare bestand te staan. Drie redenen, en elk zou op zichzelf volstaan. Een release als één
bestand draagt ze mee zonder map die kwijt kan raken. Niets op schijf kan per ongeluk worden
bewerkt, en juist dat geeft «terugzetten naar de meegeleverde versie» betekenis — de meegeleverde
versie moet buiten bereik zijn om het waard te zijn ernaar terug te keren. En een profiel dat niet
bouwt wordt een buildfout in plaats van een programma dat stilzwijgend geen profielen heeft.

### Overschrijvingen gaan per onderdeel

De bewerking van een gebruiker wordt nooit als kopie van het profiel opgeslagen. Ze wordt
opgeslagen als een overschrijving op de id van het profiel, met alleen de onderdelen die zijn
aangeraakt. Twee dingen volgen: terugzetten is überhaupt mogelijk, en een bijgewerkte build kan een
profiel dat iemand deels heeft bewerkt nog steeds verbeteren. De id draagt dit en mag na publicatie
nooit veranderen: hem hernoemen maakt iemands bewerkingen wees.

De korrelgrootte houdt stand tegenover beide voor de hand liggende alternatieven:

- **Per veld** oogt netter en levert toestanden op die niemand heeft ingesteld. Verkleur `W`, neem
  daarna een update die `Q` toevoegt, en het resultaat is een mengsel dat de gebruiker nooit heeft
  gebouwd en niet kan verklaren.
- **Per profiel** is de tegenovergestelde mislukking. Hernoem één ding en het profiel ligt voor
  altijd vast; het krijgt nooit meer een correctie te zien.

Een onderdeel is de korrelgrootte waarop de wijziging nog in één zin past: je hebt de accenten
bewerkt, dus de accenten zijn nu van jou.

### Een profiel wordt over de algemene set gelegd, regel voor regel

Sneltoetsen zijn geordend op modificatiecombinatie, en de regels van een profiel leggen zich over de
algemene in plaats van op hun plaats — regel voor regel, niet laag voor laag. Photoshop weet wat
`Ctrl+J` binnen Photoshop betekent; het weet niets van `Win+E`, dat Windows systeembreed toekent, en
niets van `Ctrl+C`, dat overal geldt waar een tekstcursor staat.

Per laag zou betekenen dat een profiel dat `Ctrl` voor zijn eigen opdrachten noemt de hele laag
meeneemt, en het klembord is wat dat kost: kopiëren, plakken, knippen, ongedaan maken en alles
selecteren gaan uit in een browser, in een chatprogramma, in een terminal — programma's waarin je
weinig anders doet dan typen en plakken. Per regel wint wie een toets noemt voor die toets, en er
beweegt niets anders. Een hele laag leegmaken is met opzet niet mogelijk.

Een profiel dat geen enkele laag noemt geeft de algemene catalogus onveranderd terug; het
gebruikelijke geval reserveert dus niets.

### Sneltoetsen en accenten dragen een label

Het label zegt wat de opdracht doet — «Laag dupliceren», niet «Ctrl+J». De hardware toont het
nooit: de leds dragen kleur en verder niets, dus het label kost tijdens de uitvoering niets. Het
betaalt zich elders drie keer terug. De voorvertoning binnen de toepassing kan het tonen, een test
kan tegenstrijdigheden tussen items vinden, en bij negentig profielen is het de enige manier
waarop iemand kan nakijken of een item klopt. `"j": "Bewerken"` valt nergens tegen af te zetten;
`"j": "Laag dupliceren"` wel.

### Een instellingenbestand in formaat 1 migreren

Een bestand in formaat 1 bewaart profielen in hun geheel, zonder id en zonder vastlegging van hun
herkomst. Een overschrijving heeft een id nodig om zich aan vast te maken, en terugzetten moet weten
dat er een meegeleverde versie is om naar terug te gaan: zo'n bestand kan dus niet zeggen welke van
zijn items de meegeleverde zijn.

Daarom worden ze allemaal gebruikersprofielen. Dat behoudt elke bewerking die iemand heeft gemaakt,
tegen de prijs dat het meegeleverde profiel naast de gemigreerde kopie verschijnt tot een van beide
wordt verwijderd — de juiste ruil, want de andere lezing wist stilzwijgend werk.

### Een instellingenbestand in formaat 2 migreren

Een bestand in formaat 2 somt alle kleuren op, ook de onaangeroerde, en kan dus niet zeggen welke
van zijn regels beslissingen zijn en welke teruggekaatste standaardwaarden. Ze allemaal opvolgen zet
het palet vast: een verbeterde meegeleverde kleur bereikt dan niemand die het programma ooit heeft
gestart.

Formaat 3 schrijft alleen wat van het meegeleverde palet afwijkt, dus een regel in het bestand
betekent dat iemand die heeft gekozen. Een ouder bestand migreren dwingt tot een gok over dat
onderscheid, en de aanname is: een regel gelijk aan het palet van die versie is een standaardwaarde,
al het andere is een keuze. `PaletteBeforeFormat3` bewaart dat palet als bevroren kopie in plaats van
het huidige te lezen — die vergelijking is zinloos op het moment dat het palet opnieuw verandert, en
dat is precies wanneer ze nodig is.

De prijs is dat wie een van die kleuren met opzet koos, haar verliest. Dat is de goede kant op: één
persoon kiest een kleur opnieuw, tegenover alle gebruikers die een palet houden dat niemand koos.

## Praten met het toetsenbord

De Chroma SDK wordt via zijn lokale REST-interface aangesproken. Kleuren zijn in BGR gecodeerde
gehele getallen; het hele toetsenbord wordt als een 6 × 22-matrix geschreven. Een sessie moet met
een hartslag in leven worden gehouden.

Gemeten op de ontwikkelmachine: een sessie aanmaken duurt 60 tot 125 ms, het eerste beeld na het
overnemen van een lopend Chroma Studio-effect ongeveer 500 ms, en elk beeld daarna rond de 2 ms.

### Elk antwoord zegt 200, dus beslist de body

De dienst beantwoordt **alles** met HTTP 200, ook verzoeken die hij heeft weggegooid. Een beeld met
de verkeerde matrixafmeting komt zo terug:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

met status 200. Wie alleen de statuscode controleert, meldt dus succes voor beelden die het
toetsenbord nooit heeft getoond: een stille mislukking, niet te onderscheiden van verlichting die
simpelweg niet verandert.

Daarom beslist `result` in de body: nul is succes, al het andere is een afwijzing. Waar de dienst
een `error` in gewone taal meelevert, wordt die ongewijzigd overgenomen, want die benoemt het
werkelijke gebrek beter dan welke hier bedachte formulering ook. De codes waar een gebruiker iets
mee kan, worden vertaald:

| Code | Betekenis |
|---|---|
| 4309 | Chroma is voor dit apparaat uitgeschakeld in Synapse |
| 1152 | een andere toepassing houdt de sessie vast |
| 1167 | er is geen Chroma-apparaat aangesloten |
| 5 | de toegang is geweigerd |
| 87 | het verzoek was onjuist opgebouwd |
| 50 | het verzoek wordt niet ondersteund |

Een geslaagde sessieopbouw draagt helemaal geen `result` — die levert in plaats daarvan de
sessiegegevens —, dus het ontbreken ervan telt als succes.

### Hoe vaak beelden worden verstuurd

Dit lijkt een detail en is dat niet: beide voor de hand liggende antwoorden zijn fout.

**Alleen bij verandering sturen** laat de overname verhongeren. Een gewone toetsaanslag verandert
de toetsenbordtoestand niet — alleen modificatie- en vergrendeltoetsen doen dat — dus levert een
overname precies één beeld op. Chroma gooit beelden weg terwijl het nog de controle overneemt, en
meldt daarvoor succes, zodat dat ene beeld kan verdwijnen en het toetsenbord op het vorige effect
laat staan tot de gebruiker toevallig een modificatietoets indrukt.

**Zo snel mogelijk sturen** verwoest de reactiesnelheid. Beelden komen in de interface in de wacht
te staan, en een toestandswijziging wacht dan achter alles wat al verstuurd is: op Shift drukken
doet er zichtbaar een seconde of twee over om te verschijnen.

Wat wél werkt is sturen om drie verschillende redenen op drie verschillende tempo's:

| Reden | Tempo |
|---|---|
| De toetsenbordtoestand is veranderd | onmiddellijk — gemeten op 1 ms van begin tot eind |
| Binnen drie seconden na een overname | elke 120 ms, tot de overdracht is uitgewerkt |
| Anders | elke 750 ms, puur als verzekering tegen een verloren beeld |

## Sessiebeheer

| Toestand | Gedrag |
|---|---|
| **Inactief** | Geen sessie. Chroma Studio stuurt de verlichting aan. Alleen de goedkope activiteitspeiling loopt. |
| **Actief** | Sessie open, hartslag loopt, een nieuw beeld bij elke toestandswijziging. |
| **Gepauzeerd** | Verlichting losgelaten tot er wordt hervat. |

Keylegend neemt het bij de eerste toetsaanslag over en laat het toetsenbord na een instelbare
periode van stilte los, zodat je eigen Chroma Studio-effect terugkomt. De wektijd van ongeveer
500 ms wordt dus alleen na een echte pauze betaald, nooit tijdens het typen.

Slechts één kopie van Keylegend stuurt het toetsenbord aan. Twee zouden twee sessies voor hetzelfde
apparaat openen; de dienst geeft het aan één ervan, en de andere verlicht niets terwijl ze succes
blijft melden — wat er precies uitziet als een programma dat stilletjes is opgehouden te werken. Wat
een tweede start doet, hangt af van wat er al draait. Hetzelfde programma van dezelfde plek betekent
dat iemand op het pictogram heeft geklikt terwijl het in het systeemvak stond: dan komt dat venster
op en trekt de tweede start zich terug, dus er wordt niets afgesloten en de verlichting knippert
niet. Al het andere — een oudere versie, of dezelfde uit een andere map — wordt vervangen: er wordt
gevraagd te stoppen, de sessie wordt teruggegeven, en pas als er binnen twee seconden geen antwoord
komt wordt zonder meer beëindigd.
