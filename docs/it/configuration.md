# Configurazione

Le impostazioni risiedono in `%APPDATA%\Keylegend\` e si modificano dall'interfaccia. Al primo
avvio viene scritta una configurazione predefinita completa.

## Colori

Un colore per categoria:

| Categoria | Si applica a |
|---|---|
| Cifra | `1`, `7`, e il tastierino mentre Bloc Num è attivo |
| Minuscola | `a`, `è` |
| Maiuscola | `A`, `È` |
| Simbolo | `+`, `#`, `€`, `\|`, e gli operatori del tastierino |
| Tasto di comando | Esc, Tab, Invio, Backspace, modificatori, frecce, blocco di navigazione, e il tastierino mentre Bloc Num è spento |
| Tasto funzione | Da F1 a F12 |
| Tasto morto | `^`, `´`, `` ` `` — tasti che richiedono una seconda battuta per produrre un carattere |
| Non assegnato | tasti senza significato nel contesto attuale; spenti per impostazione predefinita. Il tasto centrale del tastierino con Bloc Num spento ne è l'esempio più chiaro |

I tasti di blocco hanno due colori ciascuno: uno per acceso, uno per spento.

## Insiemi di scorciatoie

Un insieme di scorciatoie associa tasti a **gruppi di funzioni** e viene scelto in base ai
modificatori tenuti premuti. Insiemi inclusi: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`,
`Ctrl+Shift`, `Ctrl+Alt`.

Ogni gruppo ha il proprio colore, così i comandi affini si leggono come un blocco — per esempio la
modifica (`X`/`C`/`V`/`Z`/`Y`/`A`) in un colore e le operazioni sui file (`N`/`O`/`S`/`P`/`W`) in
un altro.

Le scorciatoie di Windows sono fissate a livello di sistema e quindi sempre esatte. Quelle con
Ctrl variano da programma a programma; l'insieme incluso copre le convenzioni Windows più comuni.

## Profili delle applicazioni

Un profilo descrive che cosa deve mostrare la tastiera mentre un dato programma è in primo piano.
Con l'applicazione ne arrivano circa novanta: programmi come Photoshop, Visual Studio Code o
Excel, e giochi come Elden Ring o Counter-Strike 2. Si applicano da soli: appena la finestra
corrispondente ha il fuoco il profilo entra in vigore, e quando il fuoco passa altrove tornano gli
insiemi predefiniti. Dove nessun profilo corrisponde, non cambia nulla.

Il riconoscimento avviene per nome dell'eseguibile. Quando corrispondono più profili, vince quello
che nomina il programma: un gioco con un profilo proprio lo conserva quindi anche se scatta pure
il rilevamento dei giochi. La priorità scioglie solo i pareggi rimanenti.

Un profilo sostituisce soltanto i livelli di modificatori che nomina lui stesso. Photoshop
sostituisce il livello Ctrl, perché lì Ctrl significa altri comandi; `Win+E` apre ancora Esplora
file, perché Windows assegna quella combinazione a livello di sistema e vale qualunque cosa ci sia
davanti.

### Che cosa contiene un profilo

| Sezione | Contenuto |
|---|---|
| Corrispondenza | A quali programmi si applica il profilo: nomi degli eseguibili, se copre i giochi rilevati in generale, e la priorità |
| Evidenziazioni | Tasti fissati a un colore indipendentemente dal carattere che producono — WASD in un gioco, i tasti degli strumenti di un editor di immagini |
| Scorciatoie | Sostituzioni di singoli livelli di modificatori: quale tasto porta quale comando sotto `Ctrl`, colorato per gruppo di funzioni |

Evidenziazioni e scorciatoie portano anche un'etichetta che dice che cosa fa il comando: «Duplica
livello», «Salta». Nulla di ciò è visibile sulla tastiera; i LED mostrano solo colore. L'etichetta
compare nell'anteprima dentro l'applicazione, e a novanta profili è l'unico modo di controllare se
una voce sia corretta.

### Modificare e ripristinare

Le tre sezioni si sostituiscono separatamente. Modifica le evidenziazioni di un profilo incluso e
da quel momento le evidenziazioni sono tue: restano congelate e non seguono più la versione
inclusa. Corrispondenza e scorciatoie continuano a seguirla e raccolgono i miglioramenti che porta
una nuova versione.

Viene salvata solo la sezione che hai cambiato, sotto l'identificatore del profilo — mai una copia
dell'intero profilo. È esattamente per questo che esiste il ripristino, e per questo un
aggiornamento può ancora migliorare un profilo che hai modificato in parte.

Il ripristino funziona quindi anch'esso per sezione: restituire le scorciatoie mantenendo le
proprie evidenziazioni è possibile. Ripristinare l'intero profilo riprende ogni sezione, oltre a un
nome modificato e a uno stato nascosto.

I profili inclusi si possono **nascondere ma non eliminare**. Vivono dentro il file del programma;
eliminarne uno durerebbe solo fino al riavvio successivo. Un profilo nascosto viene saltato nella
scelta del profilo, ma resta nell'elenco e può essere rimostrato.

### I tuoi profili

Un profilo che crei tu viene salvato per intero in `settings.json`, perché non c'è nulla con cui
confrontarlo. Perciò non può essere ripristinato, solo eliminato. Per il resto si comporta come
uno incluso: le stesse tre sezioni, la stessa regola di scelta.

Se un profilo dovrebbe valere per tutti e non solo per te, il suo posto è nel progetto come file —
vedi [Aggiungere un profilo](adding-a-profile.md).

### Formato del file delle impostazioni

`settings.json` porta `formatVersion` 2. I file più vecchi vengono migrati al caricamento: la
versione 1 non conosceva né identificatori né la provenienza di un profilo, e non può quindi dire
quali delle sue voci fossero un tempo incluse. Tutte diventano profili utente. Non si perde nulla,
ma i profili inclusi compaiono accanto, quindi all'inizio possono esserci due voci per lo stesso
programma; quella di troppo si può eliminare o nascondere.

## Comportamento

| Impostazione | Significato |
|---|---|
| Restituisci l'illuminazione quando inattivo | Se venga restituita del tutto. Disattivato, Keylegend tiene la tastiera finché non la metti in pausa o la chiudi — e la prende all'avvio anziché aspettare una battuta. |
| Periodo di inattività | Secondi senza attività della tastiera prima della restituzione. 60 di default: riprendersela costa uno o due secondi, quindi un periodo breve ne fa un'interruzione continua. Il valore viene conservato mentre la restituzione è disattivata. |
| Luminosità | Fattore globale da 0 a 100 %, applicato a ogni colore mentre il fotogramma viene composto. |
| Usa i profili delle applicazioni | Se i profili vengano consultati del tutto. Disattivato, gli insiemi predefiniti valgono ovunque, qualunque cosa ci sia davanti. |
| Avvia con Windows | Registra l'applicazione nella chiave `Run`, con l'opzione `--minimized`. Avviata così, Keylegend compare nell'area di notifica: nessuna finestra, nessun fumetto. Avviata a mano mostra sempre la finestra. Una voce scritta da una versione precedente viene aggiornata al riavvio successivo. |

## Lingua

L'interfaccia segue la lingua di visualizzazione di Windows ed è disponibile in undici lingue:
inglese, tedesco, spagnolo, francese, italiano, olandese, polacco, portoghese, russo, ucraino e
cinese semplificato. **Impostazioni → Lingua** permette di scavalcarla; il cambio ha effetto
subito, senza riavvio.

Ogni lingua si nomina da sé in quell'elenco anziché essere tradotta. Tradurlo significherebbe che
ciascuna delle undici porti dieci nomi per le altre, e chi si trovasse l'interfaccia in una lingua
che non sa leggere dovrebbe cercare la propria in una lingua che pure non sa leggere.

La scelta viene salvata in `settings.json` sotto `language` come `Automatic`, `English`, `German`,
`Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian` o
`ChineseSimplified`. Un valore sconosciuto ricade su `Automatic` invece di rifiutarsi di
avviarsi, che è ciò che un file modificato a mano con ogni probabilità vuole comunque.

Ciò che è tradotto sono i menu e le spiegazioni. Due cose **non** lo sono, entrambe di proposito:

- **Le diciture dei tasti** sulla tastiera raffigurata. Vengono dal profilo di dispositivo e devono
  corrispondere alla tastiera che hai davanti, non alla lingua dei menu: una tastiera ISO tedesca
  mostra `strg` ed `entf` che l'interfaccia sia in inglese o meno.
- **I nomi dei modificatori** (Shift, Ctrl, Alt, Alt Gr, Bloc Num …). Gli stessi nomi li produce il
  meccanismo delle scorciatoie per gli elenchi dei livelli, che sta fuori dalla traduzione; mezza
  traduzione si leggerebbe peggio di nessuna.

Tutto ciò che non ha traduzione ricade sull'inglese, così un file di lingua incompiuto costa le
righe che gli mancano e non l'intera interfaccia.

## Calibrazione

La calibrazione è una modalità da riga di comando, non una pagina delle impostazioni:

```bash
keylegend-cli --profile devices/<cartella>/device.json --calibrate
```

Accende un tasto alla volta e lo nomina, così che un profilo di dispositivo possa essere
verificato su hardware reale. I riscontri vengono scritti man mano in `calibration-findings.txt`, e
`tools/apply-calibration.ps1` li riscrive nel profilo. Vedi
[Aggiungere o correggere una tastiera](adding-a-keyboard.md).
