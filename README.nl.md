# Keylegend

**Interactieve toetsenbordverlichting voor Razer Chroma — je toetsen lichten op naar wat ze werkelijk doen.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Versie 1.0.0.** Verlichting, interface, gamedetectie en toepassingsprofielen werken.
> [Download het installatieprogramma of de draagbare kopie](https://github.com/Eistee82/Keylegend/releases/latest),
> of bouw vanuit de broncode. Zie [CHANGELOG.md](CHANGELOG.md).

![Keylegend kleurt de toetsen naar wat ze op dat moment betekenen en wisselt van profiel zodra een andere toepassing op de voorgrond komt](docs/images/keylegend.png)

---

## Wat het doet

De meeste RGB-software behandelt je toetsenbord als versiering. Keylegend behandelt het als een
**display**.

Elke toets krijgt de kleur van wat hij op dat *moment* betekent — en die kleur verandert zodra
zijn betekenis verandert:

- **Vergrendelingen in één oogopslag.** Num Lock, Caps Lock en Scroll Lock tonen hun toestand op
  de toets zelf.
- **Kleur per tekenklasse.** Cijfers, kleine letters, hoofdletters, symbolen en besturingstoetsen
  krijgen elk hun eigen kleur.
- **Houd een modificatietoets vast en zie de laag.** Druk op `AltGr` en alleen de toetsen die
  werkelijk een AltGr-teken dragen blijven branden. Druk op `Windows` en de Windows-sneltoetsen
  lichten op, gegroepeerd per functie. Hetzelfde voor `Alt`, `Ctrl` en hun combinaties.
- **Shift en Caps Lock werken vanzelf.** Doordat het teken dat elke toets oplevert live bij
  Windows wordt opgevraagd, springen letters uit zichzelf van de kleur «kleine letter» naar die
  van «hoofdletter». Het numerieke blok verkleurt naar navigatie zodra Num Lock uit staat.
- **Games krijgen hun eigen behandeling.** Ze worden automatisch herkend — ook in een randloos
  venster — en WASD, de toetsen eromheen en de cijferrij krijgen vaste kleuren: tijdens het
  spelen telt waar je handen liggen, niet welke letter een toets typt.
- **Profielen per toepassing, ongeveer negentig meegeleverd.** Photoshop, Visual Studio Code,
  Excel, Elden Ring en de rest treden in werking zodra het programma de focus heeft, en een
  profiel dat een programma noemt gaat vóór het algemene gameprofiel. Bewerk er één en alleen het
  bewerkte deel volgt de meegeleverde versie niet meer; de rest blijft met latere releases
  verbeteren.
- **Het geeft de verlichting terug.** Na een instelbare periode zonder activiteit (standaard
  60 s) laat Keylegend het toetsenbord los, zodat je Chroma Studio-effect het weer overneemt.
- **Elf talen.** Engels, Duits, Spaans, Frans, Italiaans, Nederlands, Pools, Portugees, Russisch,
  Oekraïens en vereenvoudigd Chinees. De interface volgt de weergavetaal van Windows en is in de
  instellingen te wijzigen. De toetsopschriften veranderen niet mee: die volgen je toetsenbord,
  niet de menu's.

Omdat de betekenis van de toetsen uit de **actieve Windows-toetsenbordindeling** komt en niet uit
een vaste tabel, werkt Keylegend met elke indeling — Nederlands, Duits, Amerikaans, Dvorak —
zonder aanpassing.

## Hoe het werkt

Keylegend vraagt Windows welk teken elke toets in de huidige toetsenbordtoestand zou opleveren
(`ToUnicodeEx`), leidt daaruit een categorie af, en stuurt de resulterende kleurenkaart naar de
Razer Chroma SDK via diens lokale REST-interface.

Het installeert bewust **geen** globale toetsenbordhook. Het leest alleen de *toestand* van
modificatie- en vergrendeltoetsen; het onderschept, verstuurt of registreert nooit een
toetsaanslag. Zie [docs/nl/architecture.md](docs/nl/architecture.md).

## Vereisten

- Windows 10 of 11
- Razer Synapse met de Chroma SDK-service actief
- Een Chroma-geschikt toetsenbord met een apparaatprofiel (zie hieronder)
- De .NET 10-runtime

## Installeren

```powershell
winget install Eistee82.Keylegend
```

Dat is de kortste weg: winget haalt de .NET-runtime op als opgegeven afhankelijkheid, dus er valt
niets met de hand te installeren. Anders pak je een bestand:

[**De nieuwste versie downloaden.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Bestand | Wat het is |
|---|---|
| `Keylegend-1.0.0-setup.exe` | Installeert voor de huidige gebruiker — geen beheerdersrechten. Menu-item in Start, en een verwijdering die ook het opstartitem weghaalt. |
| `Keylegend-1.0.0-portable.zip` | Hetzelfde programma, om uit te pakken. Houd de map `devices` naast het uitvoerbare bestand. |

Beide zijn niet ondertekend, dus Windows noemt de uitgever onbekend — een certificaat kost per
jaar meer dan dit project heeft. Elke uitgave bevat `SHA256SUMS.txt` om de download te
controleren, en het bouwlogboek dat haar maakte is openbaar.

## Ondersteunde toetsenborden

Ondersteuning voor een toetsenbord is **data, geen code**. Een toetsenbord is één bestand in
`devices/`: `device.json`, met de toetsgeometrie en de koppeling van toetsen aan cellen van de
Chroma-matrix.

Er worden tweeëndertig profielen meegeleverd. Eén daarvan is op echte hardware doorlopen; de rest
is gegenereerd uit de genormeerde toetsmaten, wat hun geometrie exact maakt en hun LED-koppeling
een beredeneerde gok.

| Toetsenbord | Indeling | Status |
|---|---|---|
| Razer DeathStalker V2 | ISO-DE | **op hardware geverifieerd** |
| Razer DeathStalker V2, BlackWidow V4, Huntsman V3 Pro, Ornata V3 | ANSI-US, ISO-DE | gegenereerd |
| Volledig formaat, 105/104 toetsen | ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP, ABNT2-BR | gegenereerd |
| Tenkeyless | ANSI-US, ISO-DE, ISO-UK, ISO-FR | gegenereerd |
| 75 %, 65 %, 60 % | ANSI-US, ISO-DE | gegenereerd |

`physicalLayout` beschrijft de *vorm* van het toetsenbord, niet de taal waarin je typt. Welk
teken een toets oplevert wordt onderweg aan Windows gevraagd, dus een ISO-indelingsprofiel
bedient jouw toetsenbord ook als Windows op Amerikaans of Dvorak staat.

**Lichten bij jou de verkeerde toetsen op?** Dat is precies wat «gegenereerd» betekent, en het
corrigeren vergt geen programmeerwerk — ongeveer tien minuten met de kalibratiemodus. Zie
[docs/nl/adding-a-keyboard.md](docs/nl/adding-a-keyboard.md). Correcties zijn net zo welkom als
nieuwe profielen en maken van een gok een `verified`-profiel voor iedereen met dat toetsenbord.

## Documentatie

| Onderwerp | |
|---|---|
| Architectuur | hoe de kleuring wordt bepaald, en waarom er geen toetsenbordhook is |
| Toetsenbord toevoegen of corrigeren | apparaatprofielen, kalibratie, en wat te doen als de verkeerde toetsen oplichten |
| Profiel toevoegen | kleuring per toepassing |
| Apparaatprofielformaat | elk veld, in detail |
| Configuratie | instellingen, instellingenbestand, automatisch starten |

Beschikbaar in elf talen:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

Engels en Duits zijn de onderhouden originelen; waar een vertaling ze tegenspreekt, is de Engelse
tekst de juiste. Correcties zijn welkom, zie [CONTRIBUTING.md](CONTRIBUTING.md).

## Bouwen en uitvoeren

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

Er komen twee programma's uit. **`Keylegend.exe`** (`src/Keylegend.App`) is de toepassing:
venster, pictogram in het systeemvak, instellingen. Dat is wat je voor normaal gebruik wilt.

**`keylegend-cli.exe`** (`src/Keylegend.Host`) is een consoleaansturing met de diagnostiek:

| Opdracht | Wat die doet |
|---|---|
| `keylegend-cli` | Start de verlichting. Neemt het bij de eerste aanslag over, geeft het na 10 s stilte terug. |
| `keylegend-cli --idle 30` | Hetzelfde, met een inactiviteitstijd van 30 seconden. |
| `keylegend-cli --once 10` | Tekent de huidige toestand eenmaal en houdt die tien seconden vast. Goede eerste controle. |
| `keylegend-cli --calibrate` | Licht de toetsen één voor één op om een apparaatprofiel te verifiëren. |
| `keylegend-cli --dump-layout` | Toont waar elke toets op uitkomt: gewoon / Shift / AltGr. |
| `keylegend-cli --watch-foreground` | Meldt wat de gamedetectie ziet terwijl vensters wisselen. |
| `keylegend-cli --profile <pad>` | Gebruikt een bepaalde `device.json`. |

De instellingen staan in `%APPDATA%\Keylegend\settings.json` en worden door de toepassing
geschreven.

## Bijdragen

Foutmeldingen, apparaatprofielen en vertalingen zijn allemaal welkom — zie
[CONTRIBUTING.md](CONTRIBUTING.md) en [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licentie

[MIT](LICENSE). Twee donatieknoppen van derden zijn uitgezonderd, en er staat hier geen code,
header, bibliotheek of beeldmateriaal van een fabrikant — zie [NOTICE.md](NOTICE.md).

## Merkenvermelding

Dit project is **niet verbonden aan Razer Inc. en wordt door Razer niet onderschreven of
gesponsord.**

RAZER en RAZER CHROMA zijn handelsmerken of gedeponeerde handelsmerken van Razer Inc. Ze worden
hier uitsluitend gebruikt om de hardware en de software-interface aan te duiden waarmee dit
project samenwerkt, zoals refererend gebruik toestaat. Keylegend is een onafhankelijk project dat
door de gemeenschap wordt onderhouden.

Hetzelfde geldt voor elke andere naam in deze repository. De toepassings- en gameprofielen noemen
ongeveer negentig programma's — Photoshop, Visual Studio Code, Excel, Elden Ring en meer — en de
apparaatprofielen noemen toetsenbordfabrikanten en -modellen. Dat zijn handelsmerken van hun
respectieve houders en ze staan er alleen om te zeggen voor welk programma of welk toetsenbord
iets bedoeld is. Keylegend is met geen van hen verbonden en bevat noch hun code noch hun
materiaal. Zie [NOTICE.md](NOTICE.md).
