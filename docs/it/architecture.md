# Architettura

## L'idea centrale

Tutta la logica decisionale è un **calcolo puro**, senza accesso a Windows, alla rete o al file
system:

```
(stato della tastiera, profilo di dispositivo, profilo di applicazione, impostazioni dei colori) → colore per tasto
```

Ne discendono due conseguenze, ed entrambe spiegano perché il progetto ha questa forma:

1. L'anteprima a schermo e la tastiera vera vengono riempite dallo **stesso codice**. Quello che
   vedi nella finestra è quello che si accende.
2. La logica è interamente collaudabile senza una tastiera collegata e senza Synapse installato.

Tutto ciò che parla con il mondo esterno sta in sottili adattatori attorno a quel nucleo.

## Progetti

| Progetto | Contiene | Può dipendere da |
|---|---|---|
| `Keylegend.Core` | profili di dispositivo, categorie, insiemi di scorciatoie, il compositore di fotogrammi, la macchina a stati della sessione | nulla di specifico per una piattaforma |
| `Keylegend.Windows` | stato della tastiera, risoluzione dei caratteri, finestra in primo piano | API di Windows |
| `Keylegend.Chroma` | client REST per l'SDK Chroma, battito | rete |
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

## Profili delle applicazioni

Un profilo lega regole di illuminazione a un programma. Ne sono inclusi circa novanta, e vale la
pena enunciare le decisioni che ci stanno dietro, perché ciascuna è stata la seconda risposta e
non la prima.

### I profili sono dati, non codice

La stessa regola del supporto dei dispositivi: aggiungere un profilo significa aggiungere un file
JSON sotto `profiles/`, e la compilazione lo raccoglie con un carattere jolly. Nessuno deve
toccare il C# per insegnare un programma a Keylegend, il che significa che un profilo può essere
proposto, riletto e corretto da qualcuno che conosce soltanto il programma. Se supportare una
nuova applicazione richiedesse del codice, il formato sarebbe sbagliato.

### Incorporati nell'assembly anziché sparsi su disco

I profili di dispositivo stanno accanto all'eseguibile; quelli delle applicazioni no. Tre motivi,
e ciascuno basterebbe da solo. Una versione a file unico se li porta dietro senza cartelle da
perdere. Nulla su disco può essere modificato per sbaglio, ed è proprio questo a dare un senso a
«ripristina la versione inclusa»: la versione inclusa deve essere fuori portata per meritare che
ci si torni. E un profilo che non compila diventa un errore di compilazione invece che un
programma silenziosamente privo di profili.

### Le sostituzioni sono per sezione

La modifica di un utente non viene mai salvata come copia del profilo. Viene salvata come una
sostituzione indicizzata sull'identificatore del profilo, contenente solo le sezioni toccate. Ne
seguono due cose: il ripristino è possibile del tutto, e una versione aggiornata può ancora
migliorare un profilo che qualcuno ha modificato in parte. L'identificatore regge tutto questo e
non deve mai cambiare una volta pubblicato: rinominarlo rende orfane le modifiche di qualcuno.

La granularità è stata scelta contro entrambe le alternative ovvie:

- **Per campo** sembra più ordinato e produce stati che nessuno ha configurato. Ricolora `W`, poi
  accetta un aggiornamento che aggiunge `Q`, e il risultato è un miscuglio che l'utente non ha mai
  costruito e non sa spiegare.
- **Per profilo** è il fallimento opposto. Rinomina una cosa e il profilo resta congelato per
  sempre; non vedrà mai più una correzione.

Una sezione è la granularità alla quale il cambiamento sta ancora in una frase: hai modificato le
evidenziazioni, quindi le evidenziazioni da adesso sono tue.

### Un profilo sostituisce solo i livelli che nomina

Le scorciatoie sono indicizzate per combinazione di modificatori e sovrapposte al catalogo
generale, non sostituite a esso. Photoshop sa che cosa significa `Ctrl` dentro Photoshop; non sa
nulla di `Win+E`, che Windows assegna a livello di sistema e che vale qualunque cosa ci sia
davanti. Sostituire l'intero catalogo renderebbe un profilo responsabile di fatti sui quali non ha
alcuna opinione. Un profilo che non nomina alcun livello restituisce il catalogo generale
immutato, cosicché il caso comune non alloca nulla.

### Scorciatoie ed evidenziazioni portano un'etichetta

L'etichetta dice che cosa fa il comando: «Duplica livello», non «Ctrl+J». L'hardware non la mostra
mai: i LED portano colore e nient'altro, quindi l'etichetta non costa nulla in esecuzione. Si
ripaga tre volte altrove. L'anteprima dentro l'applicazione può mostrarla, un test può trovare
contraddizioni fra le voci, e a novanta profili è l'unico modo perché qualcuno possa controllare
se una voce è corretta. `"j": "Modifica"` non si può confrontare con nulla; `"j": "Duplica
livello"` sì.

### Migrare un file di impostazioni in formato 1

Il formato 1 salvava i profili interi, senza identificatore e senza traccia della loro
provenienza. È esattamente ciò che il nuovo formato corregge: una sostituzione ha bisogno di un
identificatore a cui agganciarsi, e il ripristino ha bisogno di sapere che esiste una versione
inclusa a cui tornare.

La conseguenza per la migrazione è che un vecchio file non può dire quali delle sue voci fossero
un tempo incluse. Perciò tutte diventano profili utente. Così si conserva ogni modifica fatta da
qualcuno, al prezzo che il profilo incluso compaia accanto alla copia migrata finché uno dei due
non venga rimosso — ed è il compromesso giusto, perché l'altra lettura cancellerebbe del lavoro in
silenzio.

## Parlare con la tastiera

L'SDK Chroma viene raggiunto tramite la sua interfaccia REST locale. I colori sono interi
codificati in BGR; l'intera tastiera si scrive come una matrice 6 × 22. Una sessione va tenuta in
vita con un battito.

Misurato sulla macchina di sviluppo: creare una sessione richiede 60–125 ms, il primo fotogramma
dopo aver preso il comando da un effetto di Chroma Studio in corso circa 500 ms, e ogni fotogramma
successivo intorno ai 2 ms.

### Con quale frequenza vengono inviati i fotogrammi

Sembra un dettaglio e non lo è; entrambe le risposte ovvie sono sbagliate, e ciascuna è stata
provata.

**Inviare solo al cambiamento** lascia a secco la presa di controllo. Una battuta ordinaria non
cambia lo stato della tastiera — lo fanno solo i modificatori e i blocchi — quindi una presa di
controllo produceva esattamente un fotogramma. Chroma scarta i fotogrammi mentre sta ancora
prendendo il controllo, e per essi segnala successo: quell'unico fotogramma poteva perciò svanire
e lasciare la tastiera bloccata sull'effetto precedente finché l'utente non premeva per caso un
modificatore.

**Inviare il più in fretta possibile** rovina la reattività. I fotogrammi si accodano dentro
l'interfaccia, e un cambio di stato aspetta poi dietro a tutto ciò che è già stato inviato:
premere Maiusc metteva un secondo o due, visibilmente, a comparire.

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
