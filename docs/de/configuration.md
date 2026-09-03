# Konfiguration

Die Einstellungen liegen unter `%APPDATA%\Keylegend\` und werden über die Oberfläche
bearbeitet. Beim ersten Start wird eine vollständige Standardkonfiguration angelegt.

## Farben

Je eine Farbe pro Kategorie:

| Kategorie | Gilt für |
|---|---|
| Ziffer | `1`, `7` sowie den Ziffernblock bei eingeschaltetem Num-Lock |
| Kleinbuchstabe | `a`, `ö` |
| Großbuchstabe | `A`, `Ö` |
| Sonderzeichen | `+`, `#`, `€`, `\|` sowie die Rechenzeichen des Ziffernblocks |
| Steuertaste | Esc, Tab, Eingabe, Rücktaste, Modifier, Pfeile, Navigationsblock und der Ziffernblock bei ausgeschaltetem Num-Lock |
| Funktionstaste | F1 bis F12 |
| Tote Taste | `^`, `´`, `` ` `` — Tasten, die erst mit einem zweiten Anschlag ein Zeichen erzeugen |
| Ohne Belegung | Tasten ohne Bedeutung im aktuellen Kontext; standardmäßig dunkel. Deutlichstes Beispiel ist die mittlere Taste des Ziffernblocks bei ausgeschaltetem Num-Lock |

Die Lock-Tasten haben je zwei Farben — eine für ein, eine für aus.

## Kürzelsätze

Ein Kürzelsatz ordnet Tasten **Funktionsgruppen** zu und wird über die Menge der gerade
gehaltenen Modifier nachgeschlagen. Mitgeliefert werden: `Windows`, `Windows+Umschalt`,
`Windows+Strg`, `Alt`, `Strg`, `Strg+Umschalt`, `Strg+Alt`.

Jede Gruppe hat eine eigene Farbe, sodass zusammengehörige Befehle als Block erkennbar werden —
etwa Bearbeiten (`X`/`C`/`V`/`Z`/`Y`/`A`) in der einen und Dateibefehle (`N`/`O`/`S`/`P`/`W`) in
einer anderen Farbe.

Windows-Kürzel sind systemweit fest vergeben und daher immer zutreffend. Strg-Kürzel
unterscheiden sich je nach Programm; der mitgelieferte Satz bildet die verbreiteten
Windows-Gepflogenheiten ab.

## Anwendungsprofile

Ein Profil beschreibt, was die Tastatur zeigen soll, solange ein bestimmtes Programm im
Vordergrund ist. Rund neunzig sind mitgeliefert — Programme wie Photoshop, Visual Studio Code
oder Excel und Spiele wie Elden Ring oder Counter-Strike 2. Sie greifen ohne Zutun: Sobald das
zugehörige Fenster den Fokus hat, gilt das Profil, und beim Wechsel des Fokus gelten wieder
die Standardsätze. Greift kein Profil, ändert sich nichts.

Erkannt wird am Namen der ausführbaren Datei. Trifft mehr als ein Profil zu, gewinnt dasjenige,
das das Programm namentlich nennt — ein Spiel mit eigenem Profil behält seines also auch dann,
wenn die Spielerkennung anschlägt. Bei Gleichstand entscheidet die Priorität.

Ein Profil wird über den allgemeinen Satz gelegt, Eintrag für Eintrag. Photoshop sagt, was dort
`Strg+J` bedeutet; `Strg+C` kopiert weiterhin, denn ein Profil, das die Strg-Ebene nennt, behauptet
nicht, Strg bedeute sonst nichts. Und `Windows+E` bleibt „Explorer öffnen“, weil Windows diese
Kombination systemweit vergibt und sie unabhängig davon zutrifft, was im Vordergrund ist.

### Was in einem Profil steht

| Abschnitt | Inhalt |
|---|---|
| Zuordnung | Für welche Programme das Profil gilt: Namen der ausführbaren Dateien, ob es für erkannte Spiele allgemein gilt, und die Priorität |
| Hervorhebungen | Tasten mit fester Farbe, unabhängig vom erzeugten Zeichen — WASD im Spiel, die Werkzeugtasten einer Bildbearbeitung |
| Kürzel | Ersatz für einzelne Modifier-Ebenen: welche Taste unter `Strg` welchen Befehl trägt, nach Funktionsgruppe eingefärbt |

Hervorhebungen und Kürzel tragen zusätzlich eine Beschriftung, die sagt, was der Befehl tut —
„Ebene duplizieren“, „Springen“. Auf der Tastatur ist davon nichts zu sehen, die LEDs zeigen
nur Farbe. Die Beschriftung erscheint in der Vorschau der Anwendung, und sie ist bei neunzig
Profilen der einzige Weg, überhaupt zu prüfen, ob ein Eintrag stimmt.

### Ändern und Zurücksetzen

Die drei Abschnitte werden getrennt überschrieben. Änderst du die Hervorhebungen eines
mitgelieferten Profils, gehören ab da die Hervorhebungen dir: Sie sind eingefroren und folgen
der mitgelieferten Fassung nicht mehr. Zuordnung und Kürzel folgen weiter und übernehmen die
Verbesserungen, die eine neue Fassung von Keylegend mitbringt.

Gespeichert wird dabei nur der geänderte Abschnitt, abgelegt unter der Kennung des Profils —
nie eine Kopie des ganzen Profils. Genau deshalb gibt es ein Zurücksetzen, und deshalb kann ein
Update ein Profil verbessern, das du teilweise bearbeitet hast.

Zurückgesetzt wird entsprechend je Abschnitt: nur die Kürzel zurückgeben und die eigenen
Hervorhebungen behalten ist möglich. Das vollständige Zurücksetzen nimmt alle Abschnitte
zurück, dazu einen geänderten Namen und ein Ausblenden.

Mitgelieferte Profile lassen sich **ausblenden, aber nicht löschen**. Sie stecken in der
Programmdatei; ein Löschen hielte nur bis zum nächsten Start. Ein ausgeblendetes Profil wird
bei der Auswahl übergangen, bleibt aber in der Liste und lässt sich wieder einblenden.

### Eigene Profile

Ein selbst angelegtes Profil wird vollständig in `settings.json` gespeichert, denn es gibt
nichts, wogegen es sich vergleichen ließe. Es lässt sich daher nicht zurücksetzen, nur löschen.
Ansonsten verhält es sich wie ein mitgeliefertes: dieselben drei Abschnitte, dieselbe
Auswahlregel.

Soll ein Profil nicht nur bei dir gelten, sondern für alle mitgeliefert werden, gehört es als
Datei ins Projekt — siehe [Profil hinzufügen](adding-a-profile.md).

### Format der Einstellungsdatei

`settings.json` trägt `formatVersion` 3. Ältere Dateien werden beim Laden umgestellt.

Eine Datei der Version 1 kennt weder Kennungen noch die Herkunft eines Profils und kann daher nicht
sagen, welche ihrer Einträge mitgelieferte sind. Alle werden deshalb zu eigenen Profilen. Es geht
nichts verloren, aber die mitgelieferten Profile stehen daneben, sodass es zunächst zwei Einträge
für dasselbe Programm geben kann; den überzähligen kannst du löschen oder ausblenden.

Eine Datei der Version 2 führt jede Farbe auf, auch die unberührten, und nagelt damit die Palette
fest: eine verbesserte mitgelieferte Farbe erreicht niemanden, der das Programm zuvor gestartet
hat. Eine Farbe, die der Palette jener Fassung entspricht, wird bei der Umstellung deshalb als
Vorgabe gelesen und verworfen; alles andere ist deine Wahl und bleibt.

## Verhalten

| Einstellung | Bedeutung |
|---|---|
| Beleuchtung bei Ruhe zurückgeben | Ob überhaupt zurückgegeben wird. Ausgeschaltet behält Keylegend die Tastatur, bis Sie pausieren oder das Programm schließen — und übernimmt sie dann schon beim Start, ohne auf einen Tastendruck zu warten. |
| Ruhezeit | Sekunden ohne Tastaturaktivität, bis zurückgegeben wird. Vorgabe 60 — das Zurückholen kostet ein bis zwei Sekunden, eine kurze Ruhezeit macht daraus eine ständige Unterbrechung. Der Wert bleibt erhalten, während die Rückgabe ausgeschaltet ist. |
| Helligkeit | Globaler Faktor von 0 bis 100 %, der beim Erzeugen des Farbbilds auf alle Farben angewandt wird. |
| Anwendungsprofile verwenden | Ob Profile überhaupt herangezogen werden. Ausgeschaltet gelten überall die Standardsätze, unabhängig davon, was im Vordergrund ist. |
| Mit Windows starten | Trägt die Anwendung im `Run`-Schlüssel ein, mit dem Schalter `--minimized`. So gestartet kommt Keylegend im Infobereich hoch: kein Fenster, keine Sprechblase. Von Hand gestartet zeigt es sein Fenster wie gewohnt. Ein Eintrag aus einer früheren Fassung wird beim nächsten Start aufgefrischt. |
| Effekt beim Tippen | Wie die Beleuchtung auf einen Anschlag antwortet, Vorgabe *keiner*. Immer nur einer; die acht sind unten beschrieben. Ohne Effekt sieht Keylegend nie nach, welche einzelnen Tasten unten sind — nur, ob überhaupt getippt wird. |

### Effekte beim Tippen

Jeder Effekt ist eine Kurve über die Zeit seit dem Drücken oder Loslassen einer Taste und liegt
über dem fertigen Bild, statt in die Entscheidung einzugehen, was eine Taste bedeutet: Die Farben
sagen weiter, was sie sagten, und die Tastatur im Fenster zeigt dasselbe wie die auf dem Tisch.
Ein Effekt, der eine Taste aufhellt, mischt Weiß hinein, bei voller Stärke bis zu reinem Weiß —
jede mitgelieferte Farbe treibt ohnehin einen Kanal auf 255, ein helleres Blau gibt es nicht.
Die Effekte, die wandern, bekommen die Strecke von einer Ecke des Bretts zur anderen, sodass eine
Welle die ganze Tastatur überquert, welche Tastatur es auch ist.

| Effekt | Was passiert |
|---|---|
| Faden | Die getippte Taste wird dunkel, solange sie gehalten wird, und kehrt nach dem Loslassen binnen einer Sekunde zu ihrer Farbe zurück. |
| Aufblitzen | Die getippte Taste wird weiß in voller Helligkeit und fällt sofort in ihre eigene Farbe zurück, in weniger als einer Fünftelsekunde. |
| Nachleuchten | Die getippte Taste bleibt hell, solange sie gehalten wird, und klingt nach dem Loslassen über knapp eine Sekunde ab — die Spur, die das Tippen hinterlässt. |
| Einschlag | Die getippte Taste flackert auf, und die Tasten ringsum, bis zweieinhalb Tastenhöhen weit, antworten einen Augenblick später, die entfernteren noch später — als hätte der Anschlag das Brett erschüttert. Nach einer Fünftelsekunde vorbei. |
| Wassertropfen | Ein schmaler heller Ring läuft von der getippten Taste nach außen und verblasst dabei; das Brett überquert er in unter einer Sekunde. |
| Dunkle Welle | Derselbe Ring, dunkel: Das Brett weicht um den Anschlag zurück, statt mit ihm aufzuleuchten. |
| Funken | Ein Anschlag wirft bis zu drei Funken auf Tasten in der Nähe, nie auf die getippte Taste selbst. Sie glühen warm auf und erlöschen binnen einer halben Sekunde. Wo sie landen, ist Zufall. |
| Hitze | Tasten werden mit jedem Anschlag wärmer und kühlen wieder ab, alle vier Sekunden um die Hälfte; eine oft benutzte Taste glüht wärmer als eine einmal getippte. Der einzige Effekt, der zwischen zwei Anschlägen etwas behält, und er behält es nur im Arbeitsspeicher: eine abklingende Zahl je Taste, weg, sobald die Taste kalt ist. |

Die Wahl steht in `settings.json` unter `Effect`, als Name — `None`, `Fade`, `Flash`,
`Afterglow`, `Impact`, `Ripple`, `DarkWave`, `Sparks` oder `Heat`. Ein Name, den das Programm
nicht kennt, bedeutet keinen Effekt.

## Sprache

Die Oberfläche folgt der Anzeigesprache von Windows und ist in elf Sprachen verfügbar: Deutsch,
Englisch, Spanisch, Französisch, Italienisch, Niederländisch, Polnisch, Portugiesisch, Russisch,
Ukrainisch und vereinfachtes Chinesisch. Unter **Einstellungen → Sprache** lässt sich das
übersteuern; die Umschaltung wirkt sofort, ein Neustart ist nicht nötig.

Jede Sprache nennt sich in dieser Liste selbst, statt übersetzt zu werden. Sie zu übersetzen
hieße, dass jede der elf zehn Namen für die anderen mitführt — und wer eine Oberfläche in einer
Sprache vorfindet, die er nicht lesen kann, müsste seine eigene in einer Sprache suchen, die er
ebenfalls nicht lesen kann.

Gespeichert wird die Wahl in `settings.json` unter `language` als `Automatic`, `English`,
`German`, `Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`,
`Ukrainian` oder `ChineseSimplified`. Ein unbekannter Wert fällt auf `Automatic` zurück, statt
den Start zu verweigern — was eine von Hand bearbeitete Datei ohnehin am ehesten will.

Was übersetzt ist, sind die Menüs und Erklärungen. **Nicht** übersetzt sind zwei Dinge, und
beides mit Absicht:

- **Die Tastenbeschriftungen** auf der abgebildeten Tastatur. Sie stammen aus Razers Zeichnung und müssen zu der Tastatur passen, die vor Ihnen steht — nicht zur Sprache der Menüs. Eine
  deutsche ISO-Tastatur zeigt `strg` und `entf`, gleichgültig ob die Oberfläche englisch läuft.
- **Die Modifier-Namen** (Shift, Ctrl, Alt, AltGr, Num Lock …). Dieselben Namen erzeugt die
  Kürzelverwaltung für die Ebenenlisten, und die liegt außerhalb der Übersetzung; halb
  übersetzt läse sich schlechter als durchgehend englisch.

Fehlt eine Übersetzung, erscheint der englische Text. Eine unvollständige Sprachdatei kostet
also die betroffenen Zeilen, nicht die ganze Oberfläche.

## Wenn Synapse noch nicht läuft

Beim Anmelden starten Razers Software und Keylegend gleichzeitig, und die Beschreibung der
angeschlossenen Tastatur gibt es erst, wenn Razers Teil davon fertig ist. Keylegend hält das nicht
für einen Fehler. Sein Symbol steht im Benachrichtigungsbereich, bevor es überhaupt nachsieht, und
danach sieht es weiter nach — alle zwei Sekunden, solange gar keine Tastatur genannt ist, und mit
wachsender Pause bis höchstens einer halben Minute, solange nur die Zeichnung fehlt. Die Beleuchtung
beginnt von selbst, sobald es etwas zu beleuchten gibt.

Ein Start aus der Windows-Autostartliste öffnet dafür kein Fenster: die Tastatur vor dir zeigt, ob
es läuft, und der Hinweistext im Benachrichtigungsbereich sagt es unterdessen. Ein Start von Hand
zeigt ein kleines Fenster, sobald der erste Blick leer ausgeht; es nennt, was fehlt, und wann
zuletzt gesucht wurde. Dieses Fenster zu schließen ändert nichts — die Suche läuft weiter, und
Keylegend bleibt im Benachrichtigungsbereich.

## Wenn die Beleuchtung nicht funktioniert

Das Gespräch mit dem Chroma-Dienst kann scheitern: der Dienst ist gestoppt, Synapse wurde
geschlossen, ein anderes Programm hält die Sitzung. Keylegend versucht es weiter, mit wachsender
Pause zwischen den Versuchen, und sagt dabei, was nicht stimmt:

- die Statuszeile am unteren Fensterrand trägt den Grund, in Amber statt im üblichen Grau
- der Benachrichtigungsbereich sagt es in seinem Hinweistext, damit ein geschlossenes Fenster es
  nicht verbirgt
- eine Sprechblase meldet es, einmal je Störung und nicht einmal je Versuch

Alle drei verschwinden, sobald wieder ein Bild durchkommt. Erscheint gar nichts und die Tastatur
leuchtet dennoch nicht, läuft das Programm nicht — sieh im Benachrichtigungsbereich nach seinem
Symbol.

## Wenn die falschen Tasten leuchten

Die Tastatur im Fenster ist die Tastatur auf dem Tisch: beide werden von demselben Code gefüllt,
also zeigt das Fenster, wie die Hardware aussehen soll. Die Prüfung ist, beide nebeneinander zu
halten.

Welcher Zelle der Beleuchtungsmatrix eine Taste gehört, ist das Einzige, was weder Synapse noch die
Zeichnung sagt — es kommt aus der Tabelle des Chroma-Protokolls. Leuchtet auf der Hardware also eine
andere Taste als im Fenster, ist diese Tabelle für dein Modell falsch. Dann lohnt ein Bericht, der
Tastatur und Taste nennt.
