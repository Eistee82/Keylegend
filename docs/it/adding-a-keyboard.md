# Aggiungere o correggere una tastiera

Il supporto di una tastiera è **un dato, non codice**. Non servono né C# né strumenti di
compilazione: bastano un editor di testo e la tua tastiera.

La maggior parte di chi arriva qui non deve aggiungere nulla, perché per il proprio layout esiste
già un profilo. A quei profili manca l'unica cosa che non si può generare: qualcuno che, con
l'hardware in mano, confermi che ogni tasto si accende dove il profilo sostiene. **È il lavoro
descritto nella [parte 2](#2-correggere-un-profilo), e richiede una decina di minuti.**

---

## Che cosa sa un profilo, e con quanta certezza

Un profilo risponde a due domande distinte, e non sono ugualmente affidabili:

| Domanda | Da dove viene la risposta | Quanto è certa |
|---|---|---|
| Dove si trova ogni tasto e quanto è grande? | Il passo normalizzato di 19,05 mm, che ogni tastiera segue dall'IBM Model M in poi | **Certa.** La geometria discende dal layout. |
| Quale cella della matrice LED accende quel tasto? | La matrice pubblicata dal produttore, dando per scontata una tastiera standard | **Un'ipotesi.** I modelli spostano tasti, lasciano celle non popolate e ne aggiungono di proprie. |

Questa separazione è l'intera ragione d'essere del contrassegno `verified`. Un profilo con
`"verified": false` ha quasi certamente ragione sul disegno e può benissimo sbagliare su quale
tasto si accende.

---

## 1. Aggiungere un layout mancante

Controlla prima che manchi davvero: `devices/` contiene già profili a formato completo per
ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL,
JIS-JP e ABNT2-BR, oltre alle varianti tenkeyless, 75 %, 65 % e 60 %. Se il tuo è fra questi, passa
alla parte 2.

### La via generata

`tools/make-layout.py` costruisce un profilo a partire dalle dimensioni standard. Aggiungerci una
tastiera è una voce nell'elenco `PROFILES`, in fondo al file:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argomento | Che cosa decide |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` o `abnt2` — la forma dell'Invio e quali tasti aggiuntivi esistono |
| `legends` | Quale insieme di diciture stampate usare: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` o `fn` — che cosa sta fra l'Alt destro e il tasto menu |

Poi eseguilo:

```bash
python tools/make-layout.py --only iso-tr
```

Se le diciture della tua tastiera non sono fra i cinque insiemi, aggiungine uno: copia
`LEGENDS_EN` nello stesso file, traduci le voci e registralo in `LEGEND_SETS`. Solo i tasti che
*non* scrivono nulla hanno bisogno di una dicitura; gli altri vengono chiesti a Windows in
esecuzione, ed è ciò che permette a un profilo di servire ogni layout software sullo stesso
hardware.

### La via scritta a mano

Per una tastiera che non è una variazione di un layout standard — ortolineare, divisa, con una
fila di tasti macro che nessun altro ha — scrivi `device.json` direttamente. La
[descrizione del formato](device-profile-format.md) elenca ogni campo, e
`devices/device-profile.schema.json` dà alla maggior parte degli editor completamento ed errori in
linea.

Non serve essere esatti alla prima passata. Metti i tasti all'incirca al posto giusto, lascia `row`
e `column` a `null` dove sei incerto, e lascia che sia la calibrazione a fare il resto.

---

## 2. Correggere un profilo

Questa è la parte che richiede l'hardware, e quella che conta davvero.

### Prima guardarlo

Prima di toccare la tastiera, esamina il disegno:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-it/device.json
```

Questo scrive `preview.svg` accanto al profilo; aprilo in un browser qualsiasi. Confrontalo con la
tastiera che hai davanti e cerca:

- tasti mancanti, o tasti disegnati che la tua tastiera non ha
- un Invio della forma sbagliata: alto e a L su ISO, largo e piatto su ANSI
- una fila inferiore con il numero sbagliato di modificatori, che varia più di ogni altra cosa
- **contorni rossi**, che segnalano tasti senza cella di matrice. Quelli non si accenderanno mai.

Correggere la geometria è aritmetica, non indovinello: la griglia è un'unità per tasto, e
un'unità è la `width` che hanno i normali tasti delle lettere.

### Poi calibrare

La calibrazione accende un tasto alla volta e lo nomina, così puoi confermare che il tasto che
brilla di bianco è quello che il profilo dichiara. È l'unico modo per esserne certi: tutto il
resto è deduzione da una tabella del produttore.

```bash
keylegend-cli --profile devices/<la-tua-cartella>/device.json --calibrate
```

Percorre i tasti mappati in ordine di lettura:

| Tasto | Che cosa fa |
|---|---|
| `Invio` o `→` | questo è corretto, si passa al successivo |
| `F` | si è acceso il tasto sbagliato — annotarlo |
| `←` | un tasto indietro |
| `A` | accendere tutti i tasti mappati insieme |
| `S` | saltare al riepilogo |
| `Q` o `Esc` | fermarsi |

Poiché gli identificatori seguono il layout americano, l'indicazione mostra anche che cosa scrive
davvero ogni tasto sulla *tua* macchina: su una tastiera italiana ti si parla quindi del «tasto è»
e non di `Keyboard_ApostropheAndDoubleQuote`.

I riscontri vengono scritti in `calibration-findings.txt` man mano, non alla fine. Calibrare è un
lavoro paziente e una finestra chiusa non deve costartelo.

Mentre lavori aiuta un secondo disegno: questo etichetta ogni tasto con la cella che rivendica
anziché con la sua dicitura:

```bash
python tools/preview-layout.py devices/<la-tua-cartella>/device.json --cells
```

### Applicare quel che hai trovato

`tools/apply-calibration.ps1` lo riscrive nel profilo, conservando una copia `.bak`:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<la-tua-cartella>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` riguarda i tasti che non hanno acceso proprio nulla: la matrice può indirizzare la cella,
ma quel modello lì non ha un LED. Quei tasti conservano la loro geometria — il tasto esiste, e
l'anteprima deve disegnarlo — e perdono `row`/`column`, così non si manda nulla nel vuoto.
`-Remap` riguarda i tasti mappati sulla cella sbagliata.

### Che cosa aspettarsi

Ecco i punti in cui un profilo generato sbaglia più spesso:

| Dove | Che cosa succede |
|---|---|
| **L'Invio ISO** | Copre due celle. Su molte tastiere solo quella inferiore è dotata di LED, e la metà superiore è illuminata dalla vicina o per niente. |
| **La fila inferiore** | Numero e larghezza dei modificatori variano da modello a modello. Le tastiere da gioco mettono `Fn` dove quelle da ufficio hanno un secondo tasto Windows. |
| **Tasti macro e multimediali** | Spesso sulla colonna 0 o sulle colonne esterne, e spesso su nessuna cella. |
| **Tastiere compatte** | La matrice conserva i suoi 6 × 22 pieni; una tastiera al 60 % ne lascia semplicemente vuota la maggior parte. Le celle non vengono rinumerate. |
| **I tasti alti del tastierino** | Più e Invio coprono due righe ma rispondono a una sola cella, di solito quella superiore. |

Un tasto che si riveli privo di LED conserva la geometria e perde la cella:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Viene ancora disegnato, così l'anteprima corrisponde all'hardware; semplicemente non si accende
mai. È corretto, non è un difetto.

### Contrassegnarlo come verificato

Quando ogni cella corrisponde, passa `-MarkVerified` allo stesso script, oppure metti
`"verified": true` a mano, e togli la `note` che dice che il profilo è stato generato. Quel
contrassegno dice alla prossima persona con la tua tastiera che può fidarsene.

---

## 3. Provarlo

```bash
dotnet test
```

I test dei profili inclusi validano ogni profilo sotto `devices/`, anche il tuo. Colgono
identificatori doppi, due tasti che rivendicano lo stesso LED, tasti disegnati uno sopra l'altro,
celle fuori dalla matrice e geometria scivolata fuori dal piano.

## 4. Aprire una pull request

Indica quale tastiera e quale layout fisico hai verificato, e se hai percorso la calibrazione.
Vedi [CONTRIBUTING.md](../../CONTRIBUTING.md).

Anche i profili con `"verified": false` sono benvenuti: danno un vantaggio alla prossima persona
con quella tastiera. Una correzione a un profilo esistente vale quanto uno nuovo.

### A proposito delle immagini

Il campo `image` è facoltativo e al momento inutilizzato: l'anteprima è disegnata dalla geometria,
il che la mantiene nitida a ogni dimensione e le impedisce di contraddire il profilo. Se ne alleghi
comunque una, deve essere un'immagine che **tu** hai fotografato o disegnato. Un rendering di
prodotto di un produttore non può essere pubblicato sotto la licenza MIT di questo progetto, e a
una pull request che ne contenga uno verrà chiesto di rimuoverlo.

## Vedi anche

- [Formato del profilo di dispositivo](device-profile-format.md) — ogni campo, in dettaglio
- [Architettura](architecture.md) — perché il significato dei tasti viene da Windows e non da una tabella
