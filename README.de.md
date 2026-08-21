# Keylegend

**Interaktive Tastaturbeleuchtung für Razer Chroma — die Tasten leuchten nach dem, was sie gerade bedeuten.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Version 1.0.0.** Beleuchtung, Oberfläche, Spielerkennung und Anwendungsprofile funktionieren.
> [Installer oder portable Fassung herunterladen](https://github.com/Eistee82/Keylegend/releases/latest),
> oder selbst übersetzen. Siehe [CHANGELOG.md](CHANGELOG.md).

![Keylegend färbt die Tasten nach ihrer aktuellen Bedeutung und wechselt das Profil, wenn eine andere Anwendung in den Vordergrund kommt](docs/images/keylegend.png)

---

## Worum es geht

Die meisten RGB-Programme behandeln die Tastatur als Dekoration. Keylegend behandelt sie als
**Anzeige**.

Jede Taste wird danach eingefärbt, was sie *gerade* bedeutet — und diese Einfärbung ändert
sich in dem Moment, in dem sich die Bedeutung ändert:

- **Lock-Zustände auf einen Blick.** Num, Feststell- und Rollen-Taste zeigen ihren Zustand an
  der Taste selbst.
- **Farbe nach Zeichenkategorie.** Ziffern, Klein- und Großbuchstaben, Sonderzeichen und
  Steuertasten haben jeweils eine eigene Farbe.
- **Modifier halten, Ebene sehen.** Bei gedrücktem `AltGr` leuchten nur noch die Tasten, die
  tatsächlich ein AltGr-Zeichen tragen. Bei `Windows` erscheinen die Windows-Kürzel, nach
  Funktionsgruppen eingefärbt. Ebenso für `Alt`, `Strg` und deren Kombinationen.
- **Umschalt und Feststelltaste funktionieren nebenbei.** Da das erzeugte Zeichen live bei
  Windows erfragt wird, wechseln Buchstaben von selbst von der Farbe für Kleinbuchstaben auf
  die für Großbuchstaben. Der Ziffernblock färbt sich bei ausgeschaltetem Num-Lock zur
  Navigationsfarbe um.
- **Spiele werden eigens behandelt.** Automatisch erkannt — auch im randlosen Fenstermodus —
  und WASD, die Tasten drumherum sowie die Zahlenreihe erhalten feste Farben. Beim Spielen
  zählt, wo die Hände liegen, und nicht, welchen Buchstaben eine Taste tippt.
- **Profile je Anwendung, rund neunzig davon mitgeliefert.** Photoshop, Visual Studio Code,
  Excel, Elden Ring und die übrigen greifen, sobald das Programm den Fokus hat, und ein Profil,
  das ein Programm namentlich nennt, schlägt das allgemeine Spielprofil. Änderst du eines, folgt
  nur der geänderte Teil der mitgelieferten Fassung nicht mehr — der Rest verbessert sich mit
  jeder neuen Fassung von Keylegend weiter.
- **Gibt die Beleuchtung wieder frei.** Nach einer einstellbaren Ruhezeit (Vorgabe 60 s)
  überlässt Keylegend die Tastatur wieder deinem Chroma-Studio-Effekt.
- **Elf Sprachen.** Deutsch, Englisch, Spanisch, Französisch, Italienisch, Niederländisch,
  Polnisch, Portugiesisch, Russisch, Ukrainisch und vereinfachtes Chinesisch.
  Die Oberfläche folgt der Anzeigesprache von Windows und lässt sich
  in den Einstellungen umschalten. Die Tastenbeschriftungen bleiben davon unberührt — sie
  richten sich nach deiner Tastatur, nicht nach der Menüsprache.

Weil die Tastenbedeutungen aus dem **aktiven Windows-Tastaturlayout** stammen und nicht aus
einer fest hinterlegten Tabelle, funktioniert Keylegend mit jedem Layout — Deutsch, US,
Französisch, Dvorak — ohne Anpassung.

## Funktionsweise

Keylegend fragt bei Windows ab, welches Zeichen eine Taste im aktuellen Tastaturzustand
erzeugen würde (`ToUnicodeEx`), leitet daraus eine Kategorie ab und schickt das entstehende
Farbbild über die lokale REST-Schnittstelle an das Razer Chroma SDK.

Bewusst **ohne globalen Tastatur-Hook**: Es werden ausschließlich Modifier- und Lock-*Zustände*
gelesen. Tastenanschläge werden weder abgefangen noch weitergeleitet oder gespeichert. Siehe
[docs/de/architecture.md](docs/de/architecture.md).

## Voraussetzungen

- Windows 10 oder 11
- Razer Synapse mit laufendem Chroma-SDK-Dienst
- Eine Chroma-fähige Tastatur mit Geräteprofil (siehe unten)
- .NET-10-Laufzeitumgebung

## Installation

```powershell
winget install Eistee82.Keylegend
```

Das ist der kürzeste Weg: winget holt die .NET-Runtime als angegebene Abhängigkeit mit, es bleibt
also nichts von Hand zu installieren. Sonst eine Datei nehmen:

[**Neueste Fassung herunterladen.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Datei | Was es ist |
|---|---|
| `Keylegend-1.0.0-setup.exe` | Installiert für den aktuellen Benutzer — keine Administratorrechte nötig. Startmenüeintrag, und eine Deinstallation, die auch den Autostart-Eintrag entfernt. |
| `Keylegend-1.0.0-portable.zip` | Dasselbe Programm zum Entpacken. Der Ordner `devices` muss neben der ausführbaren Datei bleiben. |

Beide sind nicht signiert, Windows meldet daher einen unbekannten Herausgeber — ein Zertifikat
kostet im Jahr mehr, als dieses Projekt hat. Jede Veröffentlichung enthält `SHA256SUMS.txt` zum
Prüfen des Downloads, und das Build-Protokoll dazu ist öffentlich.

## Unterstützte Tastaturen

Geräteunterstützung ist **Daten, kein Code**. Eine Tastatur ist eine Datei in `devices/`:
`device.json`, mit der Tastengeometrie und der Zuordnung der Tasten zu den Chroma-Matrixzellen.

Zweiunddreißig Profile liegen bei. Eines davon ist an echter Hardware durchgegangen worden, die
übrigen sind aus den genormten Tastenmaßen erzeugt — ihre Geometrie stimmt damit exakt, ihre
LED-Zuordnung ist eine begründete Vermutung.

| Tastatur | Layout | Stand |
|---|---|---|
| Razer DeathStalker V2 | ISO-DE | **an Hardware bestätigt** |
| Razer DeathStalker V2, BlackWidow V4, Huntsman V3 Pro, Ornata V3 | ANSI-US, ISO-DE | erzeugt |
| Vollformat, 105/104 Tasten | ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP, ABNT2-BR | erzeugt |
| Tenkeyless | ANSI-US, ISO-DE, ISO-UK, ISO-FR | erzeugt |
| 75 %, 65 %, 60 % | ANSI-US, ISO-DE | erzeugt |

`physicalLayout` beschreibt die *Form* der Tastatur, nicht die Sprache, in der du schreibst.
Welches Zeichen eine Taste erzeugt, wird zur Laufzeit bei Windows erfragt — ein ISO-DE-Profil
bedient deine deutsche Tastatur also auch dann, wenn Windows auf US oder Dvorak steht.

**Leuchten bei dir die falschen Tasten?** Genau das bedeutet „erzeugt", und es zu korrigieren
erfordert kein Programmieren — etwa zehn Minuten mit der Kalibrierung. Siehe
[docs/de/adding-a-keyboard.md](docs/de/adding-a-keyboard.md). Korrekturen sind genauso willkommen
wie neue Profile und machen aus einer Vermutung ein `verified`-Profil für alle mit dieser
Tastatur.

## Dokumentation

| Thema | |
|---|---|
| Architektur | wie die Einfärbung entschieden wird, und warum es keinen Tastatur-Hook gibt |
| Tastatur hinzufügen oder korrigieren | Geräteprofile, Kalibrierung, und was zu tun ist, wenn die falschen Tasten leuchten |
| Profil hinzufügen | Einfärbung je Anwendung |
| Geräteprofil-Format | jedes Feld im Detail |
| Konfiguration | Einstellungen, Einstellungsdatei, Autostart |

In elf Sprachen verfügbar:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

Die Oberfläche spricht dieselben elf Sprachen, folgt der Anzeigesprache von Windows und lässt
sich in den Einstellungen umstellen. Die Tastenbeschriftungen bleiben davon unberührt — sie
folgen deiner Tastatur, nicht den Menüs.

Englisch und Deutsch sind die gepflegten Originale; wo eine Übersetzung ihnen widerspricht, gilt
der englische Text. Korrekturen sind willkommen, siehe [CONTRIBUTING.md](CONTRIBUTING.md).

## Selbst übersetzen und starten

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

Es entstehen zwei Programme. **`Keylegend.exe`** (`src/Keylegend.App`) ist die Anwendung:
Fenster, Symbol im Infobereich, Einstellungen — das ist die Fassung für den normalen Gebrauch.

**`keylegend-cli.exe`** (`src/Keylegend.Host`) ist ein Konsolenprogramm mit den Diagnosemodi:

| Befehl | Wirkung |
|---|---|
| `keylegend-cli` | Startet die Beleuchtung. Übernimmt beim ersten Tastendruck, gibt nach 10 s Ruhe zurück. |
| `keylegend-cli --idle 30` | Dasselbe mit 30 Sekunden Ruhezeit. |
| `keylegend-cli --once 10` | Zeichnet den aktuellen Zustand einmal und hält ihn zehn Sekunden. Guter erster Test. |
| `keylegend-cli --calibrate` | Lässt die Tasten einzeln aufleuchten, um ein Geräteprofil zu prüfen. |
| `keylegend-cli --dump-layout` | Gibt aus, was jede Taste erzeugt — normal, mit Umschalt, mit AltGr. |
| `keylegend-cli --watch-foreground` | Zeigt, was die Spielerkennung beim Fensterwechsel sieht. |
| `keylegend-cli --profile <pfad>` | Verwendet eine bestimmte `device.json`. |

Die Einstellungen liegen in `%APPDATA%\Keylegend\settings.json` und werden von der Anwendung
geschrieben.

## Mitwirken

Fehlermeldungen, Geräteprofile und Übersetzungen sind willkommen — siehe
[CONTRIBUTING.md](CONTRIBUTING.md) und [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Lizenz

[MIT](LICENSE). Ausgenommen sind zwei fremde Spenden-Schaltflächen; Herstellercode, -header,
-bibliotheken oder -grafiken enthält dieses Repository nicht — siehe [NOTICE.md](NOTICE.md).

## Markenhinweis

Dieses Projekt steht **in keiner Verbindung zu Razer Inc.** und wird von Razer weder
unterstützt noch gefördert.

RAZER und RAZER CHROMA sind Marken oder eingetragene Marken der Razer Inc. Sie werden hier
ausschließlich verwendet, um die Hardware und die Softwareschnittstelle zu bezeichnen, mit
denen dieses Projekt zusammenarbeitet. Keylegend ist ein unabhängiges, von der Gemeinschaft
gepflegtes Projekt.

Dasselbe gilt für alle anderen Namen in diesem Repository. Die Anwendungs- und Spielprofile
nennen rund neunzig Programme — Photoshop, Visual Studio Code, Excel, Elden Ring und weitere —,
die Geräteprofile nennen Tastaturhersteller und -modelle. Das sind Marken ihrer jeweiligen
Inhaber; sie stehen hier ausschließlich, um zu benennen, für welches Programm oder welche
Tastatur etwas gedacht ist. Keylegend steht mit keinem von ihnen in Verbindung und enthält
weder deren Code noch deren Grafiken. Siehe [NOTICE.md](NOTICE.md).
