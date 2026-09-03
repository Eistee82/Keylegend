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

Un profilo si sovrappone all'insieme generale, voce per voce. Photoshop dice cosa significa lì
`Ctrl+J`; `Ctrl+C` copia ancora, perché un profilo che nomina il livello Ctrl non sta affermando che
Ctrl non significhi nient'altro. E `Win+E` apre ancora Esplora file, perché Windows assegna quella
combinazione a livello di sistema e vale qualunque cosa sia in primo piano.

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

`settings.json` porta `formatVersion` 3. I file più vecchi vengono migrati al caricamento.

Un file della versione 1 non conosce né identificatori né la provenienza di un profilo, e non può
quindi dire quali delle sue voci siano quelle incluse. Tutte diventano profili utente. Non si perde
nulla, ma i profili inclusi compaiono accanto, quindi all'inizio possono esserci due voci per lo
stesso programma; quella di troppo si può eliminare o nascondere.

Un file della versione 2 elenca tutti i colori, compresi quelli che nessuno ha toccato, e con questo
fissa la tavolozza: un colore di serie migliorato non raggiunge nessuno che abbia già avviato il
programma. Un colore uguale alla tavolozza di quella versione viene perciò letto come valore
predefinito e scartato nella migrazione; tutto il resto è una sua scelta e resta.

## Comportamento

| Impostazione | Significato |
|---|---|
| Restituisci l'illuminazione quando inattivo | Se venga restituita del tutto. Disattivato, Keylegend tiene la tastiera finché non la metti in pausa o la chiudi — e la prende all'avvio anziché aspettare una battuta. |
| Periodo di inattività | Secondi senza attività della tastiera prima della restituzione. 60 di default: riprendersela costa uno o due secondi, quindi un periodo breve ne fa un'interruzione continua. Il valore viene conservato mentre la restituzione è disattivata. |
| Luminosità | Fattore globale da 0 a 100 %, applicato a ogni colore mentre il fotogramma viene composto. |
| Usa i profili delle applicazioni | Se i profili vengano consultati del tutto. Disattivato, gli insiemi predefiniti valgono ovunque, qualunque cosa ci sia davanti. |
| Avvia con Windows | Registra l'applicazione nella chiave `Run`, con l'opzione `--minimized`. Avviata così, Keylegend compare nell'area di notifica: nessuna finestra, nessun fumetto. Avviata a mano mostra sempre la finestra. Una voce scritta da una versione precedente viene aggiornata al riavvio successivo. |
| Effetto durante la digitazione | Come l'illuminazione risponde a una pressione, *nessuno* per impostazione predefinita. Uno alla volta; gli otto sono descritti sotto. Senza effetto, Keylegend non guarda mai quali singoli tasti siano premuti, solo se si sta digitando. |

### Effetti durante la digitazione

Ogni effetto è una curva sul tempo trascorso da quando un tasto è stato premuto o rilasciato,
sovrapposta all'immagine finita invece di entrare nella decisione su cosa significhi un tasto: i
colori continuano a dire ciò che dicevano, e la tastiera nella finestra mostra la stessa cosa di
quella sulla scrivania. Un effetto che schiarisce un tasto lo fa mescolando del bianco, fino al
bianco puro a piena intensità — ogni colore incluso porta già un canale a 255, quindi non esiste
un blu più luminoso. Gli effetti che si spostano ricevono la distanza da un angolo della tastiera
all'altro, così un'onda attraversa l'intera tastiera, qualunque tastiera sia.

| Effetto | Cosa succede |
|---|---|
| Dissolvenza | Il tasto premuto si spegne finché è tenuto e torna al suo colore in un secondo una volta rilasciato. |
| Lampo | Il tasto premuto diventa bianco a piena intensità e ricade subito nel proprio colore, in meno di un quinto di secondo. |
| Bagliore residuo | Il tasto premuto resta luminoso finché è tenuto e si spegne nell'arco di quasi un secondo dopo il rilascio — la scia che la digitazione lascia dietro di sé. |
| Impatto | Il tasto premuto si accende, e i tasti intorno, fino a due altezze e mezza di tasto, rispondono un istante dopo, quelli più lontani ancora più tardi — come se la pressione avesse scosso la tastiera. Finisce in un quinto di secondo. |
| Goccia d'acqua | Un anello di luce stretto parte dal tasto premuto verso l'esterno e svanisce lungo il percorso; attraversa la tastiera in meno di un secondo. |
| Onda scura | Lo stesso anello, scuro: la tastiera si apre intorno alla pressione invece di accendersi con essa. |
| Scintille | Una pressione lancia fino a tre scintille sui tasti vicini, mai sul tasto premuto. Brillano calde e si spengono entro mezzo secondo. Dove cadono è questione di caso. |
| Calore | I tasti si scaldano a ogni pressione e si raffreddano di nuovo, perdendo metà del calore ogni quattro secondi; un tasto usato spesso brilla più caldo di uno premuto una volta. L'unico effetto che conserva qualcosa tra una pressione e l'altra, e lo conserva solo in memoria: un numero per tasto che decade e sparisce non appena il tasto è freddo. |

La scelta è conservata in `settings.json` sotto `Effect`, per nome — `None`, `Fade`, `Flash`,
`Afterglow`, `Impact`, `Ripple`, `DarkWave`, `Sparks` o `Heat`. Un nome che il programma non
conosce significa nessun effetto.

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

- **Le diciture dei tasti** sulla tastiera raffigurata. Vengono dal disegno di Razer e devono corrispondere alla tastiera che hai davanti, non alla lingua dei menu: una tastiera ISO tedesca
  mostra `strg` ed `entf` che l'interfaccia sia in inglese o meno.
- **I nomi dei modificatori** (Shift, Ctrl, Alt, Alt Gr, Bloc Num …). Gli stessi nomi li produce il
  meccanismo delle scorciatoie per gli elenchi dei livelli, che sta fuori dalla traduzione; mezza
  traduzione si leggerebbe peggio di nessuna.

Tutto ciò che non ha traduzione ricade sull'inglese, così un file di lingua incompiuto costa le
righe che gli mancano e non l'intera interfaccia.

## Se Synapse non è ancora avviato

All'accesso il software di Razer e Keylegend partono insieme, e la descrizione della tastiera
collegata non esiste finché la parte di Razer non è finita. Keylegend non lo considera un guasto. La
sua icona è nell'area di notifica prima ancora che guardi, e poi continua a guardare: ogni due
secondi finché nessuna tastiera è nominata, con una pausa crescente fino a mezzo minuto finché manca
solo il disegno. L'illuminazione comincia da sola nel momento in cui c'è qualcosa da illuminare.

Un avvio dalla lista di avvio automatico di Windows non apre alcuna finestra per questo: la tastiera
davanti a te mostra se funziona, e intanto lo dice il suggerimento nell'area di notifica. Un avvio a
mano mostra una finestrella appena il primo sguardo torna vuoto, dicendo che cosa manca e quando ha
provato l'ultima volta. Chiudere quella finestra non cambia nulla: la ricerca prosegue e Keylegend
resta nell'area di notifica.

## Se l'illuminazione non funziona

Il dialogo con il servizio Chroma può fallire: il servizio è fermo, Synapse è stato chiuso, un altro
programma tiene la sessione. Keylegend continua a riprovare, con una pausa crescente fra i
tentativi, e mentre lo fa dice che cosa non va:

- la riga di stato in fondo alla finestra porta il motivo, in ambra invece del solito grigio
- l'area di notifica lo dice nel suo suggerimento, così una finestra chiusa non lo nasconde
- un fumetto lo annuncia, una volta per guasto e non una volta per tentativo

Tutti e tre spariscono appena passa di nuovo un fotogramma. Se non compare nulla e la tastiera
continua a non accendersi, il programma non è in esecuzione: cerca la sua icona nell'area di
notifica.

## Se si accendono i tasti sbagliati

La tastiera nella finestra è la tastiera sulla scrivania: le riempie lo stesso codice, quindi la
finestra mostra come dovrebbe apparire l'hardware. La verifica è tenere le due accanto.

A quale cella della matrice di illuminazione appartenga un tasto è l'unica cosa che né Synapse né il
disegno dicono: viene dalla tabella del protocollo Chroma stesso. Se dunque sull'hardware si accende
un tasto diverso da quello acceso nella finestra, quella tabella è sbagliata per il suo modello.
Vale la pena aprire una segnalazione che dica quale tastiera e quale tasto.
