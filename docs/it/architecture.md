# Architettura

## L'idea centrale

Tutta la logica decisionale è un **calcolo puro**, senza accesso a Windows, alla rete o al file
system:

```
(stato della tastiera, tastiera collegata, profilo di applicazione, impostazioni dei colori) → colore per tasto
```

Ne discendono due conseguenze, ed entrambe spiegano perché il progetto ha questa forma:

1. L'anteprima a schermo e la tastiera vera vengono riempite dallo **stesso codice**. Quello che
   vedi nella finestra è quello che si accende.
2. La logica è interamente collaudabile senza una tastiera collegata e senza Synapse installato.

Tutto ciò che parla con il mondo esterno sta in sottili adattatori attorno a quel nucleo.

## Progetti

| Progetto | Contiene | Può dipendere da |
|---|---|---|
| `Keylegend.Core` | la tastiera collegata, categorie, insiemi di scorciatoie, il compositore di fotogrammi, la macchina a stati della sessione | nulla di specifico per una piattaforma |
| `Keylegend.Windows` | stato della tastiera, risoluzione dei caratteri, finestra in primo piano | API di Windows |
| `Keylegend.Chroma` | client REST per l'SDK Chroma, battito | rete |
| `Keylegend.Engine` | il ciclo che legge la tastiera, compone un fotogramma e lo invia | Core, Chroma, Windows |
| `Keylegend.App` | interfaccia WPF, icona nell'area di notifica, archiviazione della configurazione | tutto quanto sopra |

`Keylegend.Core` non deve mai fare riferimento agli altri. Se una modifica sembra richiederlo, è
l'astrazione a essere nel posto sbagliato.

## Leggere lo stato della tastiera

Keylegend **non** installa alcun hook globale della tastiera. Un hook del genere è di fatto un
keylogger, si inserisce nella catena di input e viene regolarmente segnalato dai sistemi
anti-cheat.

Al suo posto lo stato dei tasti che interessano viene interrogato (`GetAsyncKeyState` per i
modificatori tenuti premuti, `GetKeyState` per i blocchi) circa sessanta volte al secondo, e un
nuovo fotogramma viene composto solo se qualcosa è cambiato. Nessuna battuta viene mai
intercettata, inoltrata, registrata o conservata.

### Modificatori sinistro e destro

Windows segnala **Alt Gr come Ctrl più Alt destro**, e sui layout tedeschi Ctrl + Alt sinistro
produce gli stessi caratteri di Alt Gr. Si distinguono dal lato:

- **Alt destro** → livello Alt Gr, che mostra l'assegnazione dei caratteri
- **Ctrl + Alt sinistro** → l'insieme di scorciatoie `Ctrl+Alt`

Le varianti sinistra e destra vanno quindi valutate separatamente (`VK_LMENU`/`VK_RMENU` e così
via).

## Stabilire che cosa significa un tasto

Anziché portarsi dietro una tabella di layout, Keylegend chiede a Windows quale carattere un tasto
produrrebbe nello stato attuale della tastiera (`ToUnicodeEx`), e ricava la categoria dal carattere
ottenuto.

Per questo Maiusc, Bloc Maiusc e Bloc Num non richiedono alcun trattamento speciale: lo stesso
tasto restituisce semplicemente `A` invece di `a` e finisce da sé nella categoria «maiuscola». Ed
è anche il motivo per cui qualsiasi layout di tastiera funziona senza modifiche.

### Quale tastiera è collegata

Lo si chiede a Razer Synapse, perché lo sa già. Scrive una descrizione di ogni dispositivo collegato
in `…\Razer Chroma SDK\Devices\<guid>.json`: il modello per nome, la disposizione fisica come
numero, la dimensione della matrice e il codice di scansione di ogni tasto che l'hardware ha
davvero. `SdkDeviceDescription` legge quello, e della tastiera non si deduce nulla.

Come è fatta la tastiera viene dalla stessa installazione. L'interfaccia di Synapse è
un'applicazione web, e i disegni che carica per un dispositivo restano nella sua cache: rettangoli
dei tasti con i nomi, la forma della scocca con la rotella del volume e la striscia multimediale, e
i contorni dei caratteri stampati sui tasti. `SvgLayoutSource` trova quello del modello e della
disposizione collegati in modo esatto e non dalla forma, perché ogni disegno viene consegnato
accanto a un oggetto di configurazione che nomina entrambi.

Si prendono soltanto misure e contorni; i colori e lo stile di Razer vengono ignorati, e niente di
quel materiale viene copiato in questo repository.

L'unica cosa che nessuno dei due dice è a quale cella della matrice di illuminazione appartenga un
tasto. Quella è `StandardKeyMatrix`, la tabella `RZKEY` del protocollo stesso, identica su ogni
modello.

## Profili delle applicazioni

Un profilo lega regole di illuminazione a un programma. Ne sono inclusi circa novanta, e vale la
pena enunciare le decisioni che ci stanno dietro, perché nessuna di esse è la
risposta ovvia.

### I profili sono dati, non codice

La stessa regola del supporto dei dispositivi: aggiungere un profilo significa aggiungere un file
JSON sotto `profiles/`, e la compilazione lo raccoglie con un carattere jolly. Nessuno deve
toccare il C# per insegnare un programma a Keylegend, il che significa che un profilo può essere
proposto, riletto e corretto da qualcuno che conosce soltanto il programma. Se supportare una
nuova applicazione richiedesse del codice, il formato sarebbe sbagliato.

### Incorporati nell'assembly anziché sparsi su disco

I profili delle applicazioni sono compilati nell'assembly invece di stare come file accanto
all'eseguibile. Tre motivi, e ciascuno basterebbe da solo. Una versione a file unico se li porta
dietro senza cartelle da perdere. Nulla su disco può essere modificato per sbaglio, ed è proprio
questo a dare un senso a «ripristina la versione inclusa»: la versione inclusa deve essere fuori
portata per meritare che ci si torni. E un profilo che non compila diventa un errore di
compilazione invece che un programma silenziosamente privo di profili.

### Le sostituzioni sono per sezione

La modifica di un utente non viene mai salvata come copia del profilo. Viene salvata come una
sostituzione indicizzata sull'identificatore del profilo, contenente solo le sezioni toccate. Ne
seguono due cose: il ripristino è possibile del tutto, e una versione aggiornata può ancora
migliorare un profilo che qualcuno ha modificato in parte. L'identificatore regge tutto questo e
non deve mai cambiare una volta pubblicato: rinominarlo rende orfane le modifiche di qualcuno.

La granularità tiene contro entrambe le alternative ovvie:

- **Per campo** sembra più ordinato e produce stati che nessuno ha configurato. Ricolora `W`, poi
  accetta un aggiornamento che aggiunge `Q`, e il risultato è un miscuglio che l'utente non ha mai
  costruito e non sa spiegare.
- **Per profilo** è il fallimento opposto. Rinomina una cosa e il profilo resta congelato per
  sempre; non vedrà mai più una correzione.

Una sezione è la granularità alla quale il cambiamento sta ancora in una frase: hai modificato le
evidenziazioni, quindi le evidenziazioni da adesso sono tue.

### Un profilo si sovrappone all'insieme generale, voce per voce

Le scorciatoie sono indicizzate per combinazione di modificatori, e le voci di un profilo si posano
su quelle generali invece di prenderne il posto — voce per voce, non livello per livello. Photoshop
sa cosa significa `Ctrl+J` dentro Photoshop; non sa nulla di `Win+E`, che Windows assegna a livello
di sistema, né di `Ctrl+C`, che vale dovunque ci sia un cursore di testo.

Per livello significherebbe che un profilo che nomina `Ctrl` per i propri comandi si porta via
l'intero livello, e gli appunti sono ciò che questo costa: copia, incolla, taglia, annulla e
seleziona tutto si spengono in un browser, in un programma di chat, in un terminale — programmi in
cui non si fa quasi altro che scrivere e incollare. Per voce, chi nomina un tasto vince per quel
tasto e nient'altro si muove. Svuotare un livello intero non è possibile di proposito.

Un profilo che non nomina alcun livello restituisce il catalogo generale invariato; il caso
frequente non alloca dunque nulla.

### Scorciatoie ed evidenziazioni portano un'etichetta

L'etichetta dice che cosa fa il comando: «Duplica livello», non «Ctrl+J». L'hardware non la mostra
mai: i LED portano colore e nient'altro, quindi l'etichetta non costa nulla in esecuzione. Si
ripaga tre volte altrove. L'anteprima dentro l'applicazione può mostrarla, un test può trovare
contraddizioni fra le voci, e a novanta profili è l'unico modo perché qualcuno possa controllare
se una voce è corretta. `"j": "Modifica"` non si può confrontare con nulla; `"j": "Duplica
livello"` sì.

### Migrare un file di impostazioni in formato 1

Un file in formato 1 salva i profili interi, senza identificatore e senza traccia della loro
provenienza. Una sostituzione ha bisogno di un identificatore a cui agganciarsi, e il ripristino ha
bisogno di sapere che esiste una versione inclusa a cui tornare: un file così non può quindi dire
quali delle sue voci siano quelle incluse.

Perciò tutte diventano profili utente. Così si conserva ogni modifica fatta da qualcuno, al prezzo
che il profilo incluso compaia accanto alla copia migrata finché uno dei due non venga rimosso — il
compromesso giusto, perché l'altra lettura cancella del lavoro in silenzio.

### Migrare un file di impostazioni in formato 2

Un file in formato 2 elenca tutti i colori, compresi quelli che nessuno ha toccato, e non può quindi
dire quali delle sue voci siano decisioni e quali valori predefiniti restituiti. Rispettarli tutti
fissa la tavolozza: un colore incluso migliorato non raggiunge allora nessuno che abbia mai avviato
il programma.

Il formato 3 scrive solo ciò che si discosta dalla tavolozza inclusa, così una voce nel file
significa che qualcuno l'ha scelta. Migrare un file più vecchio impone di indovinare quella
distinzione, e l'ipotesi è: una voce uguale alla tavolozza di quella versione è un valore
predefinito, qualunque altra è una scelta. `PaletteBeforeFormat3` tiene quella tavolozza come copia
congelata invece di leggere quella attuale — quel confronto perde senso nel momento in cui la
tavolozza cambia di nuovo, cioè esattamente quando serve.

Il prezzo è che chi ha scelto di proposito uno di quei colori lo perde. È il verso giusto: una
persona risceglie un colore, contro tutti gli utenti che si tengono una tavolozza che nessuno ha
scelto.

## Parlare con la tastiera

L'SDK Chroma viene raggiunto tramite la sua interfaccia REST locale. I colori sono interi
codificati in BGR; l'intera tastiera si scrive come una matrice 6 × 22. Una sessione va tenuta in
vita con un battito.

Misurato sulla macchina di sviluppo: creare una sessione richiede 60–125 ms, il primo fotogramma
dopo aver preso il comando da un effetto di Chroma Studio in corso circa 500 ms, e ogni fotogramma
successivo intorno ai 2 ms.

### Ogni risposta dice 200, quindi decide il corpo

Il servizio risponde **a tutto** con HTTP 200, comprese le richieste che ha scartato. Un fotogramma
con la dimensione di matrice sbagliata torna così:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

con stato 200. Controllare solo il codice di stato segnala quindi un successo per fotogrammi che la
tastiera non ha mai mostrato: un fallimento silenzioso, indistinguibile da un'illuminazione che
semplicemente non cambia.

Perciò decide `result` nel corpo: zero è successo, qualunque altra cosa è un rifiuto. Dove il
servizio fornisce un `error` in chiaro viene ripreso così com'è, perché nomina il difetto reale
meglio di qualsiasi formulazione inventata qui. I codici su cui un utente può intervenire vengono
tradotti:

| Codice | Significato |
|---|---|
| 4309 | Chroma è disattivato per questo dispositivo in Synapse |
| 1152 | un'altra applicazione tiene la sessione |
| 1167 | nessun dispositivo Chroma collegato |
| 5 | l'accesso è stato negato |
| 87 | la richiesta era malformata |
| 50 | la richiesta non è supportata |

Un avvio di sessione riuscito non porta alcun `result` — restituisce invece i dati della sessione —,
quindi la sua assenza conta come successo.

### Con quale frequenza vengono inviati i fotogrammi

Sembra un dettaglio e non lo è: entrambe le risposte ovvie sono sbagliate.

**Inviare solo al cambiamento** lascia a secco la presa di controllo. Una battuta ordinaria non
cambia lo stato della tastiera — lo fanno solo i modificatori e i blocchi — quindi una presa di
controllo produce esattamente un fotogramma. Chroma scarta i fotogrammi mentre sta ancora
prendendo il controllo, e per essi segnala successo: quell'unico fotogramma può perciò svanire e
lasciare la tastiera bloccata sull'effetto precedente finché l'utente non preme per caso un
modificatore.

**Inviare il più in fretta possibile** rovina la reattività. I fotogrammi si accodano dentro
l'interfaccia, e un cambio di stato aspetta poi dietro a tutto ciò che è già stato inviato:
premere Maiusc mette un secondo o due, visibilmente, a comparire.

Ciò che funziona è inviare per tre motivi distinti a tre ritmi diversi:

| Motivo | Ritmo |
|---|---|
| Lo stato della tastiera è cambiato | subito — misurato a 1 ms da un capo all'altro |
| Entro tre secondi da una presa di controllo | ogni 120 ms, finché il passaggio non si assesta |
| Altrimenti | ogni 750 ms, puramente come assicurazione contro un fotogramma perso |

## Gestione della sessione

| Stato | Comportamento |
|---|---|
| **Inattivo** | Nessuna sessione. Chroma Studio guida l'illuminazione. Gira solo l'economico sondaggio di attività. |
| **Attivo** | Sessione aperta, battito in corso, un nuovo fotogramma a ogni cambio di stato. |
| **In pausa** | Illuminazione rilasciata finché non si riprende. |

Keylegend prende il comando alla prima battuta e rilascia la tastiera dopo un periodo di
inattività configurabile, così che il tuo effetto di Chroma Studio ritorni. Il costo di risveglio
di circa 500 ms si paga quindi solo dopo una pausa vera, mai mentre si scrive.

Una sola copia di Keylegend guida la tastiera. Due aprirebbero due sessioni per lo stesso
dispositivo; il servizio lo assegna a una delle due, e l'altra non illumina nulla pur continuando a
riportare successo — che è esattamente l'aspetto di un programma che ha smesso di funzionare in
silenzio. Cosa faccia un secondo avvio dipende da ciò che è già in esecuzione. Lo stesso programma
dallo stesso posto significa che qualcuno ha fatto doppio clic sull'icona mentre stava nell'area di
notifica: compare la sua finestra e il secondo avvio si ritira, quindi non viene terminato nulla e
l'illuminazione non lampeggia. Tutto il resto — una versione precedente, o la stessa da un'altra
cartella — viene sostituito: le si chiede di uscire, restituisce la sua sessione, e viene terminata
d'ufficio solo se non risponde entro due secondi.
