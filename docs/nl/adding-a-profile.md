# Een profiel toevoegen

Een toepassingsprofiel is **data, geen code**. Je hebt geen C# en geen buildgereedschap nodig — een
teksteditor en echte kennis van het programma volstaan, en dat tweede is het lastigste deel.

Wil je alleen een profiel voor jezelf, maak het dan in de interface: het wordt in `settings.json`
opgeslagen en heeft niets hiervan nodig. Een bestand onder `profiles/` is hoe een profiel voor
iedereen met de toepassing meekomt.

## 1. Het bestand aanmaken

```
profiles/apps/<id>.json      programma's
profiles/games/<id>.json     games
```

De bestandsnaam moet gelijk zijn aan de `id` erin. Kleine letters, `a-z0-9-`. De build bedt elk
bestand in deze twee mappen met een jokerteken in, dus er is geen projectbestand om te bewerken.

Een id is blijvend. Overschrijvingen van gebruikers en items van verborgen profielen hangen eraan,
dus er een hernoemen in een latere release maakt iemands bewerkingen wees. Kies een naam die nog
klopt nadat het programma van merk verandert — `adobe-photoshop`, niet `photoshop-2026`.

## 2. Het invullen

De velden, de drie onderdelen, de functiegroepen, de modificatiecombinaties en de kleurafspraken
staan beschreven in [profiles/FORMAT.md](../../profiles/FORMAT.md). Lees dat eerst; dat is de
naslag en deze pagina herhaalt hem niet.

Wat volgt is het deel dat misgaat zelfs als het formaat gelezen is.

## 3. Posities en tekens zijn niet hetzelfde

Toets-id's komen uit het apparaatprofiel en noemen **Amerikaanse posities**. `Keyboard_Y` is de
fysieke toets die op een Amerikaans toetsenbord `Y` typt — op een Duits typt die toets `Z`. Het
formaat kent dus twee manieren om een toets te benoemen, en de verkeerde kiezen levert een profiel
op dat op elke niet-Amerikaanse indeling zichtbaar fout is terwijl het op de machine waarop het
geschreven werd perfect lijkt.

De vraag die je je per item moet stellen is waar het werkelijk over gaat:

- **Waar de hand ligt → positie.** Een accent voor WASD gaat over de vorm die je vingers maken,
  niet over de letters. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` zijn overal de
  juiste toetsen.
- **Wat de opdracht is → teken.** `Ctrl+Z` betekent «de toets die z typt». Als positie geschreven
  lijken ongedaan maken en opnieuw op een Duits toetsenbord verwisseld.
- **Toetsen die niets typen → weer positie.** Esc, Tab, Enter, Backspace, de pijltjes en de
  functietoetsen hebben geen teken, dus `shortcuts.keys` noemt ze zonder dubbelzinnigheid bij id.

### Voor accenten hangt het af van hoe het programma het toetsenbord leest

QWERTZ en QWERTY verschillen op precies twee plekken, dus `Keyboard_Y` en `Keyboard_Z` zijn de
enige id's waar dit mis kan gaan. En het gaat stilzwijgend mis.

De id van een accent is altijd een **fysieke positie**. De vraag is welke fysieke toets het
programma bedoelt, en dat volgt uit hoe het het toetsenbord leest:

| Het programma bindt aan | Voorbeelden | `Z` in de documentatie betekent |
|---|---|---|
| het **teken** (virtuele toetscodes van Windows, die de indeling volgen) | Photoshop, Blender, GIMP, Krita — toepassingen in het algemeen | `Keyboard_Y` — de toets in de bovenste rij, die op een Duits bord `Z` typt |
| de **positie** (scancodes, zoals de meeste game-engines, zodat WASD blijft zitten) | games in het algemeen | `Keyboard_Z` — de toets in de onderste rij |

Als je niet kunt vaststellen hoe een bepaald programma het toetsenbord leest, laat de items `Y` en
`Z` dan weg. Elke andere letter blijft onaangetast.

## 4. Laat weg waar je niet zeker van bent

Een verkeerde sneltoets is erger dan een ontbrekende. Een ontbrekend item laat een toets donker en
kost niets; een verkeerd item laat het toetsenbord iets onwaars beweren, en de gebruiker heeft geen
manier om te weten dat het onwaar is. Het label maakt de bewering expliciet — het maakt haar niet
juist.

Dus:

- Schrijf alleen op waarvan je zeker weet dat het de **standaard**koppeling van het programma is,
  zoals het uit de doos komt. Je eigen installatie is geen bron; je hebt waarschijnlijk dingen
  veranderd en dat vergeten.
- Controleer tegen de documentatie van het programma, of tegen het programma zelf met ongewijzigde
  instellingen.
- Waar standaardwaarden per versie verschillen, volg de huidige.
- Verzin niets. Heeft een programma geen algemeen bekende sneltoets voor iets, dan krijgt het geen
  item.

Twaalf juiste sneltoetsen zijn meer waard dan dertig waarvan er vier fout zijn. Hetzelfde geldt
voor de labels van accenten: kun je niet zeggen wat een toets doet, dan is dat een teken dat het
item nog niet in het profiel thuishoort.

## 5. Test het

```bash
dotnet test
```

De profieltests controleren elk bestand onder `profiles/`: de id is uniek en komt overeen met de
bestandsnaam, `kind` komt overeen met de map, elke toets-id bestaat in een meegeleverd
apparaatprofiel, kleuren zijn te lezen, groepen en modificatiecombinaties zijn geldig en
canoniek geschreven, elke sneltoets draagt een label, geen lettertoets staat onder
`shortcuts.keys` (die hoort onder `characters`), geen profiel is leeg, en geen twee profielen
eisen hetzelfde uitvoerbare bestand op zonder zich via `titleContains` te onderscheiden.

Eén ding wordt bewust **niet** gecontroleerd: hetzelfde label dat twee keer onder één
modificatietoets voorkomt. Het leek een manier om knip-en-plakfouten te vangen en ving in plaats
daarvan echte aliassen — browsers sluiten een tabblad zowel met `Ctrl+W` als met `Ctrl+F4`. Een
controle die op juiste data afgaat is erger dan geen.

Wat geen enkele test kan controleren is of een sneltoets *waar* is. Daarvoor is de review, en dat
is de reden dat elk item een label heeft om na te kijken.

## 6. Probeer het tegen het programma

Start Keylegend, breng het programma naar de voorgrond en houd de modificatietoetsen ingedrukt die
je profiel definieert. De voorvertoning toont hetzelfde als het toetsenbord, dus hiervoor volstaat
een laptop zonder Chroma-hardware. Vergelijk met de menu's van het programma zelf — een opdracht
waarvan je het label niet in het programma kunt vinden, is het eerste wat weg moet.

## 7. Open een pull request

Vermeld tegen welk programma en welke versie je hebt gecontroleerd, en hoe je de koppelingen hebt
geverifieerd: de documentatie van het programma, het programma zelf, of beide. Zie
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Een klein, zeker profiel is een goede bijdrage. Een groot, half onthouden profiel niet.
