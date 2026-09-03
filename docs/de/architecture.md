# Architektur

## Der Leitgedanke

Die gesamte Entscheidungslogik ist eine **reine Berechnung** ohne Zugriff auf Windows, Netzwerk
oder Dateisystem:

```
(Tastaturzustand, angeschlossene Tastatur, Anwendungsprofil, Farbeinstellungen) → Farbe je Taste
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
| `Keylegend.Core` | die angeschlossene Tastatur, Kategorien, Kürzelsätze, Bilderzeugung, Zustandsautomat | nichts Plattformspezifischem |
| `Keylegend.Windows` | Tastaturzustand, Zeichenermittlung, Vordergrundfenster | Windows-Schnittstellen |
| `Keylegend.Chroma` | REST-Anbindung an das Chroma SDK, Heartbeat | Netzwerk |
| `Keylegend.Engine` | die Schleife, die die Tastatur liest, ein Bild erzeugt und es sendet | Core, Chroma, Windows |
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

Mit gewähltem Tippeffekt wird dieselbe Abfrage bis zu den Tasten durchgezogen, die das
angeschlossene Brett meldet, statt bei den Modifiern aufzuhören. Es ist dieselbe Frage an mehr
Tasten — ist diese in diesem Augenblick unten — und sie wird nur gestellt, solange ein Effekt
gewählt ist; ohne einen wird auf die einzelnen Tasten nie gesehen. Was davon bleibt, ist wenig und
nicht von Dauer: `KeyActivity` hält fest, wann jede Taste unten war und wieder oben, und vergisst
eine Taste, die seit Sekunden niemand angerührt hat. Die eine Ausnahme ist der Hitze-Effekt, der je
Taste eine abklingende Zahl führt, solange sie zum Abkühlen braucht — eine Spur des Getippten im
Speicher, nirgends geschrieben und mit dem Prozess vorbei.

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

Razer Synapse wird gefragt, denn es weiß es bereits. Es schreibt eine Beschreibung jedes
angeschlossenen Geräts nach `…\Razer Chroma SDK\Devices\<guid>.json`: das Modell mit Namen, das
physische Layout als Zahl, die Matrixgröße und den Scancode jeder Taste, die die Hardware wirklich
hat. `SdkDeviceDescription` liest das. Nichts an der Tastatur wird erschlossen — nicht das
Modell, nicht das Layout, nicht welche Tasten es gibt.

Diese Beschreibung entsteht, wenn Razers Software hochkommt, und vorher gibt es sie nicht — beim
Anmelden ist das ein Rennen, das Keylegend verlieren kann: auf dem Rechner, auf dem dies entwickelt
wurde, erschien die Datei fünfundneunzig Sekunden nach dem Systemstart, und Keylegends eigener
Autostarteintrag griff acht Sekunden später. Die Suche danach ist deshalb kein einzelner Versuch,
dessen Fehlschlag das Programm beendet. `AttachedKeyboardSearch` sucht weiter — zügig, solange kein
Gerät genannt ist, mit wachsender Pause, solange nur die Zeichnung fehlt —, das Symbol im
Benachrichtigungsbereich entsteht vor dem ersten Blick, und der Motor wird gebaut, sobald eine
Tastatur auftaucht.

Die eigenen Schnittstellen des Chroma SDK können das nicht beantworten. Der REST-Weg hat keinen
Abfragepunkt — eine Sitzung anzulegen gibt eine Kennung und eine URI zurück, und ein `GET` darauf
antwortet „Not Supported“. Die native DLL bietet `QueryDevice`, das aber nur „ist *diese* GUID
vorhanden?“ beantwortet, ein Modell auf einmal; die Bitte um eine Liste angeschlossener Geräte
liegt im aktivsten Community-Wrapper seit 2016 offen.

Wie die Tastatur *aussieht*, kommt aus derselben Installation. Synapses Oberfläche ist eine
Web-Anwendung, und die Zeichnungen, die sie für ein Gerät lädt, bleiben in ihrem Cache: Tastenrechtecke
mit Namen, die Form des Gehäuses samt Drehregler und Medienleiste, und die Umrisse der Zeichen, die
auf den Kappen stehen. `SvgLayoutSource` findet die zum angeschlossenen Modell und physischen Layout
— genau, nicht nach Form: jede Zeichnung wird neben einem Konfigurationsobjekt ausgeliefert, das
beides nennt, und die Layout-Kennung darin ist dieselbe Zahl, die der Dienst meldet.

Übernommen werden nur Maße und Umrisse. Razers Farben und Gestaltung bleiben unbeachtet, und nichts
von der Zeichnung liegt in diesem Repository — sie wird zur Laufzeit aus der Installation gelesen,
die sie ohnehin schon hat.

Das Einzige, was weder Beschreibung noch Zeichnung sagt, ist die Zelle der Beleuchtungsmatrix zu
jeder Taste. Das ist `StandardKeyMatrix`, die `RZKEY`-Tabelle des Protokolls, auf jedem Modell
dieselbe — weshalb auch Synapse dafür keine Modelltabelle braucht.

**Es wird also überhaupt keine Tastaturbeschreibung mitgeliefert.** Es gibt keinen Ordner dafür,
keine Datei, die man für eine neue Tastatur schreiben müsste, und keine Liste unterstützter
Modelle. Die eine von Hand vermessene Tastatur bleibt als Testdatum, und `FromDrawingTests` prüft
den ganzen Aufbau dagegen: dieselben Tasten, und jede auf der Zelle, die an der Hardware gemessen
wurde.

## Anwendungsprofile

Ein Profil bindet Beleuchtungsregeln an ein Programm. Rund neunzig werden mitgeliefert, und die
Entscheidungen dahinter sind erwähnenswert, weil keine davon die naheliegende Antwort ist.

### Profile sind Daten, kein Code

Dieselbe Regel wie bei der Geräteunterstützung: Ein Profil hinzuzufügen heißt, eine JSON-Datei
unter `profiles/` anzulegen, die der Build über ein Platzhaltermuster einsammelt. Niemand muss
C# anfassen, um Keylegend ein Programm beizubringen. Damit kann ein Profil von jemandem
beigesteuert, geprüft und korrigiert werden, der nur das Programm kennt und sonst nichts. Würde
ein neues Programm Code erfordern, wäre das Format falsch.

### Eingebettet statt lose auf der Platte

Anwendungsprofile sind in die Assembly kompiliert und liegen nicht als Dateien neben der
ausführbaren Datei. Dafür gibt es drei Gründe, und jeder einzelne würde genügen. Eine Einzeldatei-
Fassung trägt die Profile mit sich, ohne einen Ordner, der verlorengehen kann. Auf der Platte lässt
sich nichts versehentlich ändern, und erst das gibt dem „auf den mitgelieferten Stand zurücksetzen“
überhaupt eine Bedeutung — der mitgelieferte Stand muss unerreichbar sein, um ein Stand zu sein,
auf den sich zurücksetzen lohnt. Und ein Profil, das nicht lesbar ist, wird zum Fehler beim Bauen
statt zu einem Programm, das stillschweigend keine Profile hat.

### Überschrieben wird abschnittsweise

Eine Änderung des Anwenders wird nie als Kopie des Profils gespeichert, sondern als
Überschreibung unter der Kennung des Profils, und zwar nur für die Abschnitte, die er angefasst
hat. Zweierlei folgt daraus: dass es ein Zurücksetzen überhaupt geben kann, und dass eine neue
Fassung ein Profil noch verbessern kann, das jemand teilweise bearbeitet hat. Die Kennung trägt
diese Mechanik und darf sich nach der Auslieferung nie mehr ändern — eine Umbenennung macht die
Änderungen eines Anwenders heimatlos.

Die Feinheit hält gegen beide naheliegenden Alternativen:

- **Je Feld** wirkt aufgeräumter und erzeugt Zustände, die niemand eingerichtet hat. Wer `W`
  umfärbt und dann eine Fassung übernimmt, die `Q` ergänzt, bekommt eine Mischung, die er nie
  gebaut hat und nicht erklären kann.
- **Je ganzem Profil** ist der umgekehrte Fehler. Eine einzige Umbenennung friert das Profil
  für immer ein; es erfährt nie wieder eine Korrektur.

Ein Abschnitt ist die Feinheit, zu der die Änderung noch einen Satz hat: Du hast die
Hervorhebungen bearbeitet, also gehören die Hervorhebungen jetzt dir.

### Ein Profil wird über den allgemeinen Satz gelegt, Eintrag für Eintrag

Kürzel sind nach Modifier-Kombination geordnet, und die Einträge eines Profils legen sich über die
allgemeinen statt an ihre Stelle — eintragsweise, nicht ebenenweise. Photoshop weiß, was `Strg+J`
in Photoshop bedeutet; es weiß nichts über `Windows+E`, das Windows systemweit vergibt, und nichts
über `Strg+C`, das überall gilt, wo eine Schreibmarke steht.

Ebenenweise hieße, dass ein Profil, das `Strg` für seine eigenen Befehle nennt, die ganze Ebene
mitnimmt, und die Zwischenablage ist der Preis dafür: Kopieren, Einfügen, Ausschneiden, Rückgängig
und Alles-markieren gehen im Browser aus, im Chat-Programm, im Terminal — Programmen, in denen man
kaum etwas anderes tut als schreiben und einfügen. Eintragsweise gewinnt, wer eine Taste nennt, für
diese Taste, und sonst bewegt sich nichts. Eine Ebene komplett zu leeren, ist bewusst nicht
möglich.

Ein Profil, das keine Ebene nennt, gibt den allgemeinen Katalog unverändert zurück; der häufige
Fall belegt also keinen Speicher.

### Kürzel und Hervorhebungen tragen eine Beschriftung

Die Beschriftung sagt, was der Befehl tut — „Ebene duplizieren“, nicht „Strg+J“. Auf der
Hardware ist sie nie zu sehen: Die LEDs tragen Farbe und sonst nichts, die Beschriftung kostet
zur Laufzeit also nichts. Sie zahlt sich an drei anderen Stellen aus. Die Vorschau in der
Anwendung kann sie anzeigen, ein Test kann Widersprüche zwischen Einträgen finden, und bei
neunzig Profilen ist sie der einzige Weg, überhaupt zu beurteilen, ob ein Eintrag richtig ist.
`"j": "Edit"` lässt sich mit nichts abgleichen, `"j": "Duplicate layer"` schon.

### Umstellung einer Einstellungsdatei der Version 1

Eine Datei der Version 1 speichert Profile vollständig, ohne Kennung und ohne jeden Vermerk, woher
ein Profil stammt. Eine Überschreibung braucht aber eine Kennung, an der sie hängt, und ein
Zurücksetzen muss wissen, dass es eine mitgelieferte Fassung gibt — eine solche Datei kann daher
nicht sagen, welche ihrer Einträge mitgelieferte sind.

Also werden alle zu eigenen Profilen. Das bewahrt jede Änderung, die jemand vorgenommen hat, um den
Preis, dass das mitgelieferte Profil neben der übernommenen Fassung steht, bis eines von beiden
entfernt wird — der richtige Tausch, denn die andere Lesart löscht stillschweigend Arbeit.

### Umstellung einer Einstellungsdatei der Version 2

Eine Datei der Version 2 führt jede Farbe auf, auch die unberührten, und kann deshalb nicht sagen,
welche ihrer Einträge Entscheidungen sind und welche zurückgespiegelte Vorgaben. Alle zu befolgen
nagelt die Palette fest: eine verbesserte mitgelieferte Farbe erreicht dann niemanden, der das
Programm jemals gestartet hat.

Version 3 schreibt nur, was von der mitgelieferten Palette abweicht; ein Eintrag in der Datei
bedeutet damit, dass jemand ihn gewählt hat. Die Umstellung einer älteren Datei muss diese
Unterscheidung erraten, und die Annahme ist: ein Eintrag, der der Palette jener Fassung entspricht,
ist eine Vorgabe, alles andere eine Wahl. `PaletteBeforeFormat3` hält diese Palette als eingefrorene
Kopie, statt die aktuelle zu lesen — dieser Vergleich ist in dem Moment bedeutungslos, in dem sich
die Palette erneut ändert, also genau dann, wenn er gebraucht wird.

Der Preis ist, dass jemand, der eine dieser Farben bewusst gewählt hat, sie verliert. Das ist die
richtige Richtung: eine Person wählt eine Farbe erneut, gegen alle Nutzer, die eine Palette
behalten, die niemand gewählt hat.

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
| 5 | der Zugriff wurde verweigert |
| 87 | die Anfrage war fehlerhaft |
| 50 | die Anfrage wird nicht unterstützt |

Ein erfolgreicher Sitzungsaufbau trägt überhaupt kein `result` — er liefert stattdessen die
Sitzungsdaten —, deshalb zählt dessen Fehlen als Erfolg.

### Wie oft Bilder gesendet werden

Das wirkt wie eine Nebensache und ist keine: beide naheliegenden Antworten sind falsch.

**Nur bei Änderung senden** lässt die Übernahme verhungern. Ein gewöhnlicher Tastendruck ändert
den Tastaturzustand nicht — das tun nur Modifier und Lock-Tasten —, also ist eine Übernahme genau
ein Bild. Chroma verwirft Bilder, solange es die Kontrolle noch übernimmt, und meldet dafür Erfolg.
Dieses eine Bild kann damit spurlos verschwinden und die Tastatur bleibt auf dem vorherigen Effekt,
bis der Anwender zufällig einen Modifier drückt.

**So schnell wie möglich senden** zerstört die Reaktionsfähigkeit. Die Bilder stauen sich in der
Schnittstelle, und eine Zustandsänderung wartet dann hinter allem bereits Gesendeten — ein
Druck auf die Umschalttaste braucht sichtbar ein bis zwei Sekunden.

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

Nur eine Kopie von Keylegend steuert die Tastatur. Zwei würden zwei Sitzungen für dasselbe Gerät
öffnen; der Dienst gibt es einer davon, und die andere beleuchtet nichts, meldet aber weiter Erfolg
— was genau so aussieht wie ein Programm, das stillschweigend aufgehört hat zu arbeiten. Was ein
zweiter Start bewirkt, hängt davon ab, was bereits läuft. Dasselbe Programm vom selben Ort heißt:
jemand hat das Symbol angeklickt, während es im Infobereich saß. Dann kommt dessen Fenster hoch und
der zweite Start tritt ab — nichts wird beendet, und die Beleuchtung flackert nicht. Alles andere —
eine ältere Fassung oder dieselbe aus einem anderen Ordner — wird abgelöst: sie wird gebeten zu
gehen, gibt ihre Sitzung zurück und wird nur dann hart beendet, wenn sie binnen zwei Sekunden nicht
antwortet.
