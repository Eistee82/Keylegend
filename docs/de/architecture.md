# Architektur

## Der Leitgedanke

Die gesamte Entscheidungslogik ist eine **reine Berechnung** ohne Zugriff auf Windows, Netzwerk
oder Dateisystem:

```
(Tastaturzustand, Geräteprofil, Anwendungsprofil, Farbeinstellungen) → Farbe je Taste
```

Daraus folgen zwei Eigenschaften, und sie sind der Grund für diesen Zuschnitt:

1. Die Vorschau auf dem Bildschirm und die echte Tastatur werden von **demselben Code**
   befüllt. Was im Fenster zu sehen ist, leuchtet auch.
2. Die Logik ist vollständig testbar, ohne angeschlossene Tastatur und ohne installiertes
   Synapse.

Alles, was mit der Außenwelt spricht, liegt in dünnen Adaptern darum herum.

## Projekte

| Projekt | Enthält | Darf abhängen von |
|---|---|---|
| `Keylegend.Core` | Geräteprofile, Kategorien, Kürzelsätze, Bilderzeugung, Zustandsautomat | nichts Plattformspezifischem |
| `Keylegend.Windows` | Tastaturzustand, Zeichenermittlung, Vordergrundfenster | Windows-Schnittstellen |
| `Keylegend.Chroma` | REST-Anbindung an das Chroma SDK, Heartbeat | Netzwerk |
| `Keylegend.App` | WPF-Oberfläche, Tray-Symbol, Konfigurationsablage | allem Vorgenannten |

`Keylegend.Core` darf die anderen niemals referenzieren. Wenn eine Änderung das nötig zu machen
scheint, sitzt die Abstraktion an der falschen Stelle.

## Auslesen des Tastaturzustands

Keylegend installiert **keinen** globalen Tastatur-Hook. Ein solcher Hook ist funktional ein
Keylogger, sitzt in der Eingabekette und wird von Anticheat-Systemen regelmäßig beanstandet.

Stattdessen werden die Zustände der interessierenden Tasten abgefragt (`GetAsyncKeyState` für
gehaltene Modifier, `GetKeyState` für Lock-Zustände), etwa sechzigmal je Sekunde, und nur bei
einer Änderung wird ein neues Bild erzeugt. Es wird kein Tastenanschlag abgefangen,
weitergeleitet, protokolliert oder gespeichert.

### Linke und rechte Modifier

Windows meldet **AltGr als Strg plus rechtes Alt**, und auf deutschen Layouts erzeugt Strg +
linkes Alt dieselben Zeichen wie AltGr. Unterschieden wird nach Seite:

- **rechtes Alt** → AltGr-Ebene, es wird die Zeichenbelegung angezeigt
- **Strg + linkes Alt** → der Kürzelsatz `Strg+Alt`

Linke und rechte Varianten müssen daher getrennt ausgewertet werden (`VK_LMENU`/`VK_RMENU` und
so weiter).

## Ermitteln der Tastenbedeutung

Statt eine Tabelle aller Layouts mitzuliefern, fragt Keylegend bei Windows ab, welches Zeichen
eine Taste im aktuellen Tastaturzustand erzeugen würde (`ToUnicodeEx`), und leitet die Kategorie
aus dem entstehenden Zeichen ab.

Deshalb brauchen Umschalt-, Feststell- und Num-Taste keine Sonderbehandlung: Dieselbe Taste
liefert schlicht `A` statt `a` und landet von selbst in der Kategorie „Großbuchstabe“. Und
deshalb funktioniert jedes Tastaturlayout ohne Anpassung.

### Welche Tastatur angeschlossen ist

Das Chroma-SDK kann es nicht sagen. Seine REST-Schnittstelle hat keinen Abfrage-Endpunkt — der
Aufbau einer Sitzung liefert eine Kennung und eine URI zurück, und ein `GET` auf diese URI wird
mit „Not Supported" beantwortet. Die native DLL bietet `QueryDevice`, das aber beantwortet nur
„ist *diese* GUID vorhanden?", ein Modell nach dem anderen; die Anfrage nach einer Liste
angeschlossener Geräte liegt im aktivsten Community-Wrapper seit 2016 unbeantwortet.

Windows beantwortet sie in einem Aufruf. `ConnectedKeyboards` fragt Raw Input nach den
angeschlossenen Geräten und liest aus deren Namen die USB-Hersteller- und Produktkennung heraus
— `1532:0295` bei einer DeathStalker V2. Ein Geräteprofil mit passendem `usb`-Paar gewinnt dann
ohne Weiteres.

Zwei Dinge verdienen Genauigkeit. Raw Input wird hier **ausschließlich zum Aufzählen von
Geräten** verwendet, niemals um Eingaben von ihnen zu empfangen: Tastaturen aufzulisten heißt
nicht, sie abzuhören, und die Zusage weiter oben bleibt unangetastet. Und ein Hersteller
verwendet dieselbe Produktkennung über alle Layouts hinweg, die Erkennung engt die Auswahl also
auf ein *Modell* ein; welche ISO- oder ANSI-Variante davon gilt, entscheidet danach das aktive
Windows-Tastaturlayout — als Hinweis und nur zum Auflösen dieses Gleichstands.

Das wiegt schwerer, als es aussieht. Solange ein einziges Profil mitgeliefert wurde, war „die
erste gefundene Datei" dasselbe wie „die richtige". Bei zweiunddreißig war es ein 60-%-Layout,
das zwei Drittel einer Vollformat-Tastatur dunkel ließ — ein Profil, das eine Taste nicht
erwähnt, kann sie auch nicht beleuchten.

## Anwendungsprofile

Ein Profil bindet Beleuchtungsregeln an ein Programm. Rund neunzig werden mitgeliefert, und die
Entscheidungen dahinter sind erwähnenswert, weil jede davon nicht die erste, sondern die zweite
Antwort war.

### Profile sind Daten, kein Code

Dieselbe Regel wie bei der Geräteunterstützung: Ein Profil hinzuzufügen heißt, eine JSON-Datei
unter `profiles/` anzulegen, die der Build über ein Platzhaltermuster einsammelt. Niemand muss
C# anfassen, um Keylegend ein Programm beizubringen. Damit kann ein Profil von jemandem
beigesteuert, geprüft und korrigiert werden, der nur das Programm kennt und sonst nichts. Würde
ein neues Programm Code erfordern, wäre das Format falsch.

### Eingebettet statt lose auf der Platte

Geräteprofile liegen neben der ausführbaren Datei, Anwendungsprofile nicht. Dafür gibt es drei
Gründe, und jeder einzelne würde genügen. Eine Einzeldatei-Fassung trägt die Profile mit sich,
ohne einen Ordner, der verlorengehen kann. Auf der Platte lässt sich nichts versehentlich
ändern, und erst das gibt dem „auf den mitgelieferten Stand zurücksetzen“ überhaupt eine
Bedeutung — der mitgelieferte Stand muss unerreichbar sein, um ein Stand zu sein, auf den sich
zurücksetzen lohnt. Und ein Profil, das nicht lesbar ist, wird zum Fehler beim Bauen statt zu
einem Programm, das stillschweigend keine Profile hat.

### Überschrieben wird abschnittsweise

Eine Änderung des Anwenders wird nie als Kopie des Profils gespeichert, sondern als
Überschreibung unter der Kennung des Profils, und zwar nur für die Abschnitte, die er angefasst
hat. Zweierlei folgt daraus: dass es ein Zurücksetzen überhaupt geben kann, und dass eine neue
Fassung ein Profil noch verbessern kann, das jemand teilweise bearbeitet hat. Die Kennung trägt
diese Mechanik und darf sich nach der Auslieferung nie mehr ändern — eine Umbenennung macht die
Änderungen eines Anwenders heimatlos.

Die Feinheit wurde gegen beide naheliegenden Alternativen gewählt:

- **Je Feld** wirkt aufgeräumter und erzeugt Zustände, die niemand eingerichtet hat. Wer `W`
  umfärbt und dann eine Fassung übernimmt, die `Q` ergänzt, bekommt eine Mischung, die er nie
  gebaut hat und nicht erklären kann.
- **Je ganzem Profil** ist der umgekehrte Fehler. Eine einzige Umbenennung friert das Profil
  für immer ein; es erfährt nie wieder eine Korrektur.

Ein Abschnitt ist die Feinheit, zu der die Änderung noch einen Satz hat: Du hast die
Hervorhebungen bearbeitet, also gehören die Hervorhebungen jetzt dir.

### Ein Profil ersetzt nur die Ebenen, die es nennt

Kürzel sind nach Modifier-Kombination abgelegt und werden über den allgemeinen Satz gelegt,
nicht an dessen Stelle gesetzt. Photoshop weiß, was `Strg` in Photoshop bedeutet; über
`Windows+E` weiß es nichts, denn das vergibt Windows systemweit und es trifft zu, gleich was im
Vordergrund ist. Den ganzen Satz zu ersetzen machte ein Profil für Tatsachen zuständig, zu
denen es keine Meinung hat. Nennt ein Profil keine einzige Ebene, wird der allgemeine Satz
unverändert zurückgegeben, sodass der Normalfall nichts kostet.

### Kürzel und Hervorhebungen tragen eine Beschriftung

Die Beschriftung sagt, was der Befehl tut — „Ebene duplizieren“, nicht „Strg+J“. Auf der
Hardware ist sie nie zu sehen: Die LEDs tragen Farbe und sonst nichts, die Beschriftung kostet
zur Laufzeit also nichts. Sie zahlt sich an drei anderen Stellen aus. Die Vorschau in der
Anwendung kann sie anzeigen, ein Test kann Widersprüche zwischen Einträgen finden, und bei
neunzig Profilen ist sie der einzige Weg, überhaupt zu beurteilen, ob ein Eintrag richtig ist.
`"j": "Edit"` lässt sich mit nichts abgleichen, `"j": "Duplicate layer"` schon.

### Umstellung einer Einstellungsdatei der Version 1

Version 1 speicherte Profile vollständig, ohne Kennung und ohne jeden Vermerk, woher ein Profil
stammt. Genau das behebt das neue Format: Eine Überschreibung braucht eine Kennung, an der sie
hängt, und ein Zurücksetzen muss wissen, dass es eine mitgelieferte Fassung gibt, auf die
zurückgesetzt werden kann.

Für die Umstellung folgt daraus, dass eine alte Datei nicht sagen kann, welche ihrer Einträge
einmal mitgeliefert waren. Also werden alle zu eigenen Profilen. Das bewahrt jede Änderung, die
jemand vorgenommen hat, um den Preis, dass das mitgelieferte Profil neben der übernommenen
Fassung steht, bis eines von beiden entfernt wird. Das ist der richtige Tausch, denn die andere
Lesart würde stillschweigend Arbeit löschen.

## Ansteuerung der Tastatur

Das Chroma SDK wird über seine lokale REST-Schnittstelle angesprochen. Farben sind BGR-kodierte
Ganzzahlen, die gesamte Tastatur wird als 6 × 22-Matrix geschrieben. Eine Sitzung muss mit einem
Heartbeat am Leben gehalten werden.

Auf dem Entwicklungsrechner gemessen: Eine Sitzung anzulegen dauert 60–125 ms, das erste Bild
nach der Übernahme von einem laufenden Chroma-Studio-Effekt rund 500 ms, jedes weitere Bild
etwa 2 ms.

### Jede Antwort lautet 200, also entscheidet der Rumpf

Der Dienst beantwortet **alles** mit HTTP 200, auch Anfragen, die er verworfen hat. Ein Bild mit
falscher Matrixgröße kommt zurück als:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

— mit Status 200. Wer nur den Statuscode prüft, meldet also Erfolg für Bilder, die die Tastatur
nie angezeigt hat: ein stiller Fehlschlag, nicht zu unterscheiden davon, dass sich die
Beleuchtung einfach nicht ändert.

Deshalb entscheidet `result` im Rumpf: null bedeutet Erfolg, alles andere eine Ablehnung. Wo der
Dienst einen `error` im Klartext mitliefert, wird der unverändert übernommen — er benennt den
tatsächlichen Mangel besser als jede hier erfundene Formulierung. Die Codes, mit denen ein
Nutzer etwas anfangen kann, werden übersetzt:

| Code | Bedeutung |
|---|---|
| 4309 | Chroma ist für dieses Gerät in Synapse abgeschaltet |
| 1152 | eine andere Anwendung hält die Sitzung |
| 1167 | kein Chroma-Gerät angeschlossen |
| 87 | die Anfrage war fehlerhaft |
| 50 | die Anfrage wird nicht unterstützt |

Ein erfolgreicher Sitzungsaufbau trägt überhaupt kein `result` — er liefert stattdessen die
Sitzungsdaten —, deshalb zählt dessen Fehlen als Erfolg.

### Wie oft Bilder gesendet werden

Das wirkt wie eine Nebensache und ist keine. Beide naheliegenden Antworten sind falsch, und
beide wurden ausprobiert.

**Nur bei Änderung senden** lässt die Übernahme verhungern. Ein gewöhnlicher Tastendruck ändert
den Tastaturzustand nicht — das tun nur Modifier und Lock-Tasten —, also entstand bei einer
Übernahme genau ein Bild. Chroma verwirft Bilder, solange es die Kontrolle noch übernimmt, und
meldet dafür Erfolg. Dieses eine Bild konnte damit spurlos verschwinden und die Tastatur blieb
auf dem vorherigen Effekt eingefroren, bis der Anwender zufällig einen Modifier drückte.

**So schnell wie möglich senden** zerstört die Reaktionsfähigkeit. Die Bilder stauen sich in der
Schnittstelle, und eine Zustandsänderung wartet dann hinter allem bereits Gesendeten — ein
Druck auf die Umschalttaste brauchte sichtbar ein bis zwei Sekunden.

Was funktioniert: aus drei verschiedenen Anlässen mit drei verschiedenen Raten senden.

| Anlass | Rate |
|---|---|
| Der Tastaturzustand hat sich geändert | sofort — gemessen 1 ms von Ende zu Ende |
| Innerhalb von drei Sekunden nach einer Übernahme | alle 120 ms, bis die Übernahme greift |
| Sonst | alle 750 ms, rein zur Absicherung gegen ein verlorenes Bild |

## Sitzungsverwaltung

| Zustand | Verhalten |
|---|---|
| **Ruhend** | Keine Sitzung. Chroma Studio steuert die Beleuchtung. Nur die sparsame Aktivitätsabfrage läuft. |
| **Aktiv** | Sitzung offen, Heartbeat läuft, neues Bild bei jeder Zustandsänderung. |
| **Pausiert** | Beleuchtung freigegeben, bis fortgesetzt wird. |

Keylegend übernimmt beim ersten Tastendruck und gibt die Tastatur nach einer einstellbaren
Ruhezeit wieder frei, sodass dein eigener Chroma-Studio-Effekt zurückkehrt. Die rund 500 ms
Anlaufzeit fallen daher nur nach einer echten Pause an, nie während des Tippens.
