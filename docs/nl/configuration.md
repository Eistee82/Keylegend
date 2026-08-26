# Configuratie

De instellingen staan in `%APPDATA%\Keylegend\` en worden via de interface bewerkt. Bij de eerste
start wordt een volledige standaardconfiguratie weggeschreven.

## Kleuren

Eén kleur per categorie:

| Categorie | Geldt voor |
|---|---|
| Cijfer | `1`, `7`, en het numerieke blok terwijl Num Lock aan staat |
| Kleine letter | `a`, `ë` |
| Hoofdletter | `A`, `Ë` |
| Symbool | `+`, `#`, `€`, `\|`, en de operatoren van het numerieke blok |
| Besturingstoets | Esc, Tab, Enter, Backspace, modificatietoetsen, pijltjes, navigatieblok, en het numerieke blok terwijl Num Lock uit staat |
| Functietoets | F1 tot en met F12 |
| Dode toets | `^`, `´`, `` ` `` — toetsen die een tweede aanslag nodig hebben om een teken op te leveren |
| Niet toegewezen | toetsen zonder betekenis in de huidige context; standaard donker. De middelste toets van het numerieke blok met Num Lock uit is het duidelijkste voorbeeld |

Vergrendeltoetsen hebben elk twee kleuren — één voor aan, één voor uit.

## Sneltoetssets

Een sneltoetsset koppelt toetsen aan **functiegroepen** en wordt gekozen op grond van de
modificatietoetsen die je ingedrukt houdt. Meegeleverde sets: `Win`, `Win+Shift`, `Win+Ctrl`,
`Alt`, `Ctrl`, `Ctrl+Shift`, `Ctrl+Alt`.

Elke groep heeft een eigen kleur, zodat verwante opdrachten als een blok lezen — bijvoorbeeld
bewerken (`X`/`C`/`V`/`Z`/`Y`/`A`) in de ene kleur en bestandsbewerkingen (`N`/`O`/`S`/`P`/`W`) in
een andere.

Windows-sneltoetsen liggen systeembreed vast en zijn daarom altijd accuraat. Ctrl-sneltoetsen
verschillen per programma; de meegeleverde set dekt de gebruikelijke Windows-conventies.

## Toepassingsprofielen

Een profiel beschrijft wat het toetsenbord moet tonen terwijl een bepaald programma vooraan staat.
Er komen er ongeveer negentig met de toepassing mee — programma's als Photoshop, Visual Studio
Code of Excel, en games als Elden Ring of Counter-Strike 2. Ze treden vanzelf in werking: zodra
het bijbehorende venster de focus heeft geldt het profiel, en gaat de focus verder, dan gelden de
standaardsets weer. Waar geen profiel past, verandert er niets.

Herkenning gaat op naam van het uitvoerbare bestand. Passen er meerdere profielen, dan wint het
profiel dat het programma noemt — een game met een eigen profiel houdt dat dus, ook al slaat de
gamedetectie eveneens aan. Prioriteit beslecht alleen de resterende gelijkspelen.

Een profiel wordt over de algemene set gelegd, regel voor regel. Photoshop zegt wat `Ctrl+J` daar
betekent; `Ctrl+C` kopieert nog steeds, want een profiel dat de Ctrl-laag noemt beweert niet dat Ctrl
verder niets betekent. En `Win+E` opent nog steeds de Verkenner, omdat Windows die combinatie
systeembreed toekent en die geldt wat er ook vooraan staat.

### Wat een profiel bevat

| Onderdeel | Inhoud |
|---|---|
| Overeenkomst | Op welke programma's het profiel van toepassing is: namen van uitvoerbare bestanden, of het gedetecteerde games in het algemeen dekt, en de prioriteit |
| Accenten | Toetsen op een vaste kleur gezet, ongeacht het teken dat ze opleveren — WASD in een game, de gereedschapstoetsen van een beeldbewerker |
| Sneltoetsen | Vervangingen van afzonderlijke modificatielagen: welke toets welke opdracht draagt onder `Ctrl`, gekleurd per functiegroep |

Accenten en sneltoetsen dragen ook een label dat zegt wat de opdracht doet — «Laag dupliceren»,
«Springen». Niets daarvan is op het toetsenbord te zien; de leds tonen alleen kleur. Het label
verschijnt in de voorvertoning binnen de toepassing, en bij negentig profielen is het de enige
manier om überhaupt te controleren of een item klopt.

### Bewerken en terugzetten

De drie onderdelen worden apart overschreven. Bewerk de accenten van een meegeleverd profiel en de
accenten zijn vanaf dat moment van jou: ze liggen vast en volgen de meegeleverde versie niet meer.
De overeenkomst en de sneltoetsen blijven die wel volgen en pikken de verbeteringen op die een
nieuwe release brengt.

Alleen het onderdeel dat je veranderde wordt opgeslagen, onder de id van het profiel — nooit een
kopie van het hele profiel. Juist daarom bestaat terugzetten, en daarom kan een update een profiel
dat je deels hebt bewerkt nog steeds verbeteren.

Terugzetten werkt dan ook per onderdeel: de sneltoetsen teruggeven terwijl je je eigen accenten
houdt, kan. Het hele profiel terugzetten neemt elk onderdeel terug, plus een gewijzigde naam en een
verborgen toestand.

Meegeleverde profielen kunnen **verborgen maar niet verwijderd** worden. Ze zitten in het
programmabestand; er een verwijderen zou maar duren tot de volgende start. Een verborgen profiel
wordt overgeslagen bij het kiezen van een profiel, maar blijft in de lijst staan en kan weer worden
getoond.

### Je eigen profielen

Een profiel dat je zelf maakt wordt in zijn geheel in `settings.json` opgeslagen, want er is niets
om het mee te vergelijken. Het kan daarom niet worden teruggezet, alleen verwijderd. Verder gedraagt
het zich als een meegeleverd profiel: dezelfde drie onderdelen, dezelfde keuzeregel.

Als een profiel voor iedereen zou moeten gelden en niet alleen voor jou, hoort het als bestand in
het project thuis — zie [Een profiel toevoegen](adding-a-profile.md).

### Formaat van het instellingenbestand

`settings.json` draagt `formatVersion` 3. Oudere bestanden worden bij het laden gemigreerd.

Een bestand van versie 1 kent noch id's noch de herkomst van een profiel, en kan dus niet zeggen
welke van zijn items de meegeleverde zijn. Ze worden allemaal gebruikersprofielen. Er gaat niets
verloren, maar de meegeleverde profielen verschijnen ernaast, dus er kunnen aanvankelijk twee items
voor hetzelfde programma zijn; het overtollige kan worden verwijderd of verborgen.

Een bestand van versie 2 somt alle kleuren op, ook de onaangeroerde, en zet daarmee het palet vast:
een verbeterde meegeleverde kleur bereikt niemand die het programma eerder heeft gestart. Een kleur
gelijk aan het palet van die versie wordt bij de migratie daarom als standaardwaarde gelezen en
laten vallen; al het andere is uw keuze en blijft.

## Gedrag

| Instelling | Betekenis |
|---|---|
| Verlichting teruggeven bij inactiviteit | Of ze überhaupt wordt teruggegeven. Uitgeschakeld houdt Keylegend het toetsenbord tot je pauzeert of afsluit — en neemt het bij de start in plaats van op een toetsaanslag te wachten. |
| Inactiviteitstijd | Seconden zonder toetsenbordactiviteit vóór de teruggave. Standaard 60 — het terugnemen kost één tot twee seconden, dus een korte tijd maakt daar een constante onderbreking van. De waarde blijft bewaard terwijl de teruggave uit staat. |
| Helderheid | Globale factor van 0 tot 100 %, toegepast op elke kleur terwijl het beeld wordt samengesteld. |
| Toepassingsprofielen gebruiken | Of profielen überhaupt worden geraadpleegd. Uitgeschakeld gelden de standaardsets overal, wat er ook vooraan staat. |
| Met Windows starten | Registreert de toepassing in de `Run`-sleutel, met de schakelaar `--minimized`. Zo gestart komt Keylegend in het systeemvak op: geen venster, geen ballon. Met de hand gestart toont het altijd zijn venster. Een item dat door een oudere versie is geschreven wordt bij de volgende start bijgewerkt. |

## Taal

De interface volgt de weergavetaal van Windows en is beschikbaar in elf talen: Engels, Duits,
Spaans, Frans, Italiaans, Nederlands, Pools, Portugees, Russisch, Oekraïens en vereenvoudigd
Chinees. **Instellingen → Taal** gaat daaroverheen; het omschakelen werkt onmiddellijk, zonder
herstart.

Elke taal noemt zichzelf in die lijst in plaats van vertaald te worden. Hem vertalen zou betekenen
dat elk van de elf tien namen voor de andere meedraagt, en wie de interface aantreft in een taal
die hij niet kan lezen, zou zijn eigen taal moeten zoeken in een taal die hij evenmin kan lezen.

De keuze wordt in `settings.json` onder `language` opgeslagen als `Automatic`, `English`,
`German`, `Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian`
of `ChineseSimplified`. Een onbekende waarde valt terug op `Automatic` in plaats van te weigeren
te starten, wat een met de hand bewerkt bestand hoogstwaarschijnlijk toch wil.

Wat vertaald is, zijn de menu's en de uitleg. Twee dingen zijn dat **niet**, beide met opzet:

- **De toetsopschriften** op het afgebeelde toetsenbord. Die komen uit Razers tekening en moeten passen bij het toetsenbord voor je, niet bij de taal van de menu's — een Duits
  ISO-toetsenbord toont `strg` en `entf`, of de interface nu in het Engels draait of niet.
- **De namen van de modificatietoetsen** (Shift, Ctrl, Alt, AltGr, Num Lock …). Diezelfde namen
  produceert de sneltoetsmachinerie voor de laaglijsten, en die staat buiten de vertaling; een
  halve vertaling zou slechter lezen dan geen.

Alles zonder vertaling valt terug op het Engels, zodat een onafgemaakt taalbestand de regels kost
die het mist en niet de hele interface.

## Als de verlichting niet werkt

Het gesprek met de Chroma-service kan mislukken: de service is gestopt, Synapse is afgesloten, een
ander programma houdt de sessie. Keylegend blijft het proberen, met een groeiende pauze tussen de
pogingen, en zegt daarbij wat er mis is:

- de statusregel onderaan het venster draagt de reden, in amber in plaats van het gewone grijs
- het systeemvak zegt het in zijn tooltip, zodat een gesloten venster het niet verbergt
- één ballon meldt het, eenmaal per storing en niet eenmaal per poging

Alle drie verdwijnen zodra er weer een beeld doorkomt. Verschijnt er niets en licht het toetsenbord
nog steeds niet op, dan loopt het programma niet — kijk in het systeemvak naar zijn pictogram.

## Als de verkeerde toetsen oplichten

Het toetsenbord in het venster is het toetsenbord op het bureau: ze worden door dezelfde code
gevuld, dus het venster toont hoe de hardware eruit zou moeten zien. De controle is de twee naast
elkaar houden.

Bij welke cel van de lichtmatrix een toets hoort, is het enige wat noch Synapse noch de tekening
zegt: dat komt uit de tabel van het Chroma-protocol zelf. Licht er op de hardware dus een andere
toets op dan in het venster, dan is die tabel verkeerd voor jouw model. Een issue met welk
toetsenbord en welke toets is dan de moeite waard.
