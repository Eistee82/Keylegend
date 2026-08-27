# Keylegend

**Interaktive Tastaturbeleuchtung für Razer Chroma — die Tasten leuchten nach dem, was sie gerade bedeuten.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Version 1.1.0.** Beleuchtung, Oberfläche, Spielerkennung und Anwendungsprofile funktionieren.
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
- Eine angeschlossene Razer-Chroma-Tastatur (siehe unten)
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
| `Keylegend-1.1.0-setup.exe` | Installiert für den aktuellen Benutzer — keine Administratorrechte nötig. Startmenüeintrag, und eine Deinstallation, die auch den Autostart-Eintrag entfernt. |
| `Keylegend-1.1.0-portable.zip` | Dasselbe Programm zum Entpacken. Die Sprachordner (`de`, `fr`, …) müssen neben der ausführbaren Datei bleiben, sonst erscheint die Oberfläche auf Englisch. |

Beide sind nicht signiert, Windows meldet daher einen unbekannten Herausgeber — ein Zertifikat
kostet im Jahr mehr, als dieses Projekt hat. Jede Veröffentlichung enthält `SHA256SUMS.txt` zum
Prüfen des Downloads, und das Build-Protokoll dazu ist öffentlich.

## Unterstützte Tastaturen

**Jede Razer-Chroma-Tastatur.** Es gibt keine Liste und keine Datei pro Modell, denn Keylegend muss
deine Tastatur nicht erkennen — es fragt nach. Razer Synapse beschreibt die angeschlossene: das
Modell mit Namen, das physische Layout als Zahl, und die Tasten, die die Hardware wirklich hat.
Razers eigene Zeichnung dieses Modells liefert den Rest — die echten Tastenmaße, das Gehäuse mit
Drehregler und Medientasten, und die Umrisse der Zeichen, die auf den Kappen stehen, in der
richtigen Sprache.

Das Einzige, was die Zeichnung nicht sagt, ist die Zelle der Beleuchtungsmatrix zu jeder Taste. Die
ist eine Konstante des Chroma-Protokolls und auf jedem Modell dieselbe — deshalb braucht auch
Synapse keine Modelltabelle. Gegen die eine von Hand kalibrierte Tastatur geprüft: alle 105 Tasten
stimmen.

Das **physische Layout** beschreibt die *Form* der Tastatur, nicht die Sprache, in der du schreibst. Welches
Zeichen eine Taste erzeugt, wird zur Laufzeit bei Windows erfragt — eine deutsche Tastatur wird also
auch dann richtig bedient, wenn Windows auf US oder Dvorak steht.

**Setzt Razer Synapse voraus**, installiert und gestartet, mit angeschlossener Tastatur. Dort wird
die Tastatur beschrieben, und dort liegt ihre Zeichnung.

## Dokumentation

| Thema | |
|---|---|
| Architektur | wie die Einfärbung entschieden wird, und warum es keinen Tastatur-Hook gibt |
| Profil hinzufügen | Einfärbung je Anwendung |
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
cd Keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) ist das ganze Programm: Fenster, Symbol im
Benachrichtigungsbereich, Einstellungen. Der eine Schalter, der es wert ist: `--verify` prüft, ob
eine Kopie die mitgelieferten Profile und alle elf Sprachen trägt, schreibt den Befund in den
danach angegebenen Pfad und antwortet über den Rückgabewert. Genau das prüft das Release-Skript an
einem gepackten Stand.

Die Einstellungen liegen in `%APPDATA%\Keylegend\settings.json` und werden von der Anwendung
geschrieben.

## Mitwirken

Fehlermeldungen, Anwendungsprofile und Übersetzungen sind willkommen — siehe
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

Dasselbe gilt für alle anderen Namen in diesem Repository. Die Anwendungs- und Spielprofile nennen
rund neunzig Programme — Photoshop, Visual Studio Code, Excel, Elden Ring und weitere —, die
Dokumentation nennt Tastaturhersteller und -modelle. Das sind Marken ihrer jeweiligen Inhaber; sie
stehen hier ausschließlich, um zu benennen, für welches Programm oder welche Tastatur etwas gedacht
ist. Keylegend steht mit keinem von ihnen in Verbindung und enthält weder deren Code noch deren
Grafiken. Siehe [NOTICE.md](NOTICE.md).