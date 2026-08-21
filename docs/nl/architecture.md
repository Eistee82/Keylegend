# Architectuur

## Het centrale idee

De hele beslislogica is een **zuivere berekening**, zonder toegang tot Windows, het netwerk of het
bestandssysteem:

```
(toetsenbordtoestand, apparaatprofiel, toepassingsprofiel, kleurinstellingen) → kleur per toets
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
| `Keylegend.Core` | apparaatprofielen, categorieën, sneltoetssets, de beeldsamensteller, de toestandsmachine van de sessie | niets platformspecifieks |
| `Keylegend.Windows` | toetsenbordtoestand, tekenresolutie, voorgrondvenster | Windows-API's |
| `Keylegend.Chroma` | REST-client voor de Chroma SDK, hartslag | netwerk |
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

## Toepassingsprofielen

Een profiel bindt verlichtingsregels aan een programma. Er worden er ongeveer negentig
meegeleverd, en de afwegingen daarachter zijn het waard om te noemen, want elk was het tweede
antwoord en niet het eerste.

### Profielen zijn data, geen code

Dezelfde regel als bij apparaatondersteuning: een profiel toevoegen is een JSON-bestand toevoegen
onder `profiles/`, en de build pikt het met een jokerteken op. Niemand hoeft C# aan te raken om
Keylegend een programma te leren, wat betekent dat een profiel kan worden aangedragen, nagekeken
en gecorrigeerd door iemand die alleen het programma kent. Als het ondersteunen van een nieuwe
toepassing ooit code zou vergen, deugt het formaat niet.

### Ingebed in de assembly in plaats van los op schijf

Apparaatprofielen staan naast het uitvoerbare bestand; toepassingsprofielen niet. Drie redenen, en
elk zou op zichzelf volstaan. Een release als één bestand draagt ze mee zonder map die kwijt kan
raken. Niets op schijf kan per ongeluk worden bewerkt, en juist dat geeft «terugzetten naar de
meegeleverde versie» betekenis — de meegeleverde versie moet buiten bereik zijn om het waard te
zijn ernaar terug te keren. En een profiel dat niet bouwt wordt een buildfout in plaats van een
programma dat stilzwijgend geen profielen heeft.

### Overschrijvingen gaan per onderdeel

De bewerking van een gebruiker wordt nooit als kopie van het profiel opgeslagen. Ze wordt
opgeslagen als een overschrijving op de id van het profiel, met alleen de onderdelen die zijn
aangeraakt. Twee dingen volgen: terugzetten is überhaupt mogelijk, en een bijgewerkte build kan een
profiel dat iemand deels heeft bewerkt nog steeds verbeteren. De id draagt dit en mag na publicatie
nooit veranderen: hem hernoemen maakt iemands bewerkingen wees.

De korrelgrootte is gekozen tegenover beide voor de hand liggende alternatieven:

- **Per veld** oogt netter en levert toestanden op die niemand heeft ingesteld. Verkleur `W`, neem
  daarna een update die `Q` toevoegt, en het resultaat is een mengsel dat de gebruiker nooit heeft
  gebouwd en niet kan verklaren.
- **Per profiel** is de tegenovergestelde mislukking. Hernoem één ding en het profiel ligt voor
  altijd vast; het krijgt nooit meer een correctie te zien.

Een onderdeel is de korrelgrootte waarop de wijziging nog in één zin past: je hebt de accenten
bewerkt, dus de accenten zijn nu van jou.

### Een profiel vervangt alleen de lagen die het noemt

Sneltoetsen zijn gerangschikt op modificatiecombinatie en worden over de algemene catalogus
gelegd, niet ervoor in de plaats gezet. Photoshop weet wat `Ctrl` binnen Photoshop betekent; het
weet niets van `Win+E`, dat Windows systeembreed toewijst en dat waar is ongeacht wat er
vooraan staat. De hele catalogus vervangen zou een profiel verantwoordelijk maken voor feiten
waarover het geen mening heeft. Een profiel dat geen enkele laag noemt, geeft de algemene
catalogus ongewijzigd terug, zodat het gewone geval niets alloceert.

### Sneltoetsen en accenten dragen een label

Het label zegt wat de opdracht doet — «Laag dupliceren», niet «Ctrl+J». De hardware toont het
nooit: de leds dragen kleur en verder niets, dus het label kost tijdens de uitvoering niets. Het
betaalt zich elders drie keer terug. De voorvertoning binnen de toepassing kan het tonen, een test
kan tegenstrijdigheden tussen items vinden, en bij negentig profielen is het de enige manier
waarop iemand kan nakijken of een item klopt. `"j": "Bewerken"` valt nergens tegen af te zetten;
`"j": "Laag dupliceren"` wel.

### Een instellingenbestand in formaat 1 migreren

Formaat 1 bewaarde profielen in hun geheel, zonder id en zonder vastlegging van hun herkomst. Juist
dat lost het nieuwe formaat op: een overschrijving heeft een id nodig om zich aan vast te maken, en
terugzetten moet weten dat er een meegeleverde versie is om naar terug te gaan.

Het gevolg voor de migratie is dat een oud bestand niet kan zeggen welke van zijn items ooit
meegeleverd waren. Dus worden ze allemaal gebruikersprofielen. Dat behoudt elke bewerking die
iemand heeft gemaakt, tegen de prijs dat het meegeleverde profiel naast de gemigreerde kopie
verschijnt tot een van beide wordt verwijderd — en dat is de juiste ruil, want de andere lezing zou
stilzwijgend werk wissen.

## Praten met het toetsenbord

De Chroma SDK wordt via zijn lokale REST-interface aangesproken. Kleuren zijn in BGR gecodeerde
gehele getallen; het hele toetsenbord wordt als een 6 × 22-matrix geschreven. Een sessie moet met
een hartslag in leven worden gehouden.

Gemeten op de ontwikkelmachine: een sessie aanmaken duurt 60 tot 125 ms, het eerste beeld na het
overnemen van een lopend Chroma Studio-effect ongeveer 500 ms, en elk beeld daarna rond de 2 ms.

### Hoe vaak beelden worden verstuurd

Dit lijkt een detail en is dat niet; beide voor de hand liggende antwoorden zijn fout, en beide
zijn geprobeerd.

**Alleen bij verandering sturen** laat de overname verhongeren. Een gewone toetsaanslag verandert
de toetsenbordtoestand niet — alleen modificatie- en vergrendeltoetsen doen dat — dus leverde een
overname precies één beeld op. Chroma gooit beelden weg terwijl het nog de controle overneemt, en
meldt daarvoor succes, zodat dat ene beeld kon verdwijnen en het toetsenbord vastgevroren liet op
het vorige effect tot de gebruiker toevallig een modificatietoets indrukte.

**Zo snel mogelijk sturen** verwoest de reactiesnelheid. Beelden komen in de interface in de wacht
te staan, en een toestandswijziging wacht dan achter alles wat al verstuurd is: op Shift drukken
deed er zichtbaar een seconde of twee over om te verschijnen.

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
