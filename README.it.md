# Keylegend

**Illuminazione della tastiera interattiva per Razer Chroma: i tasti si accendono in base a ciò che fanno davvero.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Versione 1.2.0.** Illuminazione, interfaccia, rilevamento dei giochi e profili delle
> applicazioni funzionano. [Scarica l'installatore o la copia portatile](https://github.com/Eistee82/Keylegend/releases/latest),
> oppure compila dai sorgenti. Vedi [CHANGELOG.md](CHANGELOG.md).

![Keylegend colora i tasti in base al loro significato del momento e cambia profilo quando un'altra applicazione passa in primo piano](docs/images/keylegend.png)

---

## Che cosa fa

Quasi tutti i software RGB trattano la tastiera come un ornamento. Keylegend la tratta come un
**display**.

Ogni tasto è colorato in base a ciò che significa *in quel momento*, e quel colore cambia
nell'istante in cui cambia il suo significato:

- **I blocchi a colpo d'occhio.** Bloc Num, Bloc Maiusc e Bloc Scorr mostrano il proprio stato
  sul tasto stesso.
- **Un colore per classe di carattere.** Cifre, minuscole, maiuscole, simboli e tasti di comando
  hanno ciascuno il proprio colore.
- **Tieni premuto un modificatore e vedi il suo livello.** Premi `Alt Gr` e restano accesi solo i
  tasti che portano davvero un carattere Alt Gr. Premi `Windows` e si accendono le scorciatoie di
  Windows, raggruppate per funzione. Lo stesso per `Alt`, `Ctrl` e le loro combinazioni.
- **Maiusc e Bloc Maiusc funzionano da soli.** Poiché il carattere prodotto da ogni tasto viene
  chiesto a Windows in tempo reale, le lettere passano da sole dal colore «minuscola» a quello
  «maiuscola». Il tastierino si ricolora come navigazione quando Bloc Num è spento.
- **I giochi hanno un trattamento a parte.** Vengono rilevati automaticamente — anche in finestra
  senza bordi — e WASD, i tasti attorno e la fila dei numeri assumono colori fissi: mentre giochi
  conta dove vanno le mani, non quale lettera scrive un tasto.
- **Profili per applicazione, circa novanta inclusi.** Photoshop, Visual Studio Code, Excel,
  Elden Ring e gli altri si applicano appena il programma ha il fuoco, e un profilo che nomina un
  programma prevale sul profilo di gioco generale. Modificane uno e solo la parte modificata
  smette di seguire la versione inclusa; il resto continua a migliorare con le versioni
  successive.
- **L'illuminazione può rispondere alla digitazione.** Otto effetti tra cui scegliere, *nessuno*
  per impostazione predefinita: il tasto premuto si spegne e torna, lampeggia o resta acceso, una
  goccia d'acqua o un'onda scura attraversa la tastiera, i tasti intorno tremano, volano
  scintille, oppure i tasti si scaldano con l'uso e si raffreddano di nuovo. Sovrapposto ai
  colori, non mescolato a essi: ogni tasto continua a dire ciò che significa.
- **Restituisce l'illuminazione.** Dopo un periodo di inattività configurabile (60 s
  predefiniti), Keylegend rilascia la tastiera e il tuo effetto di Chroma Studio riprende il
  comando.
- **Undici lingue.** Inglese, tedesco, spagnolo, francese, italiano, olandese, polacco,
  portoghese, russo, ucraino e cinese semplificato. L'interfaccia segue la lingua di
  visualizzazione di Windows e si può cambiare nelle impostazioni. Le diciture dei tasti non ne
  risentono: seguono la tua tastiera, non i menu.

Poiché il significato dei tasti viene dal **layout di tastiera attivo di Windows** e non da una
tabella fissa, Keylegend funziona con qualsiasi layout — italiano, tedesco, americano, Dvorak —
senza modifiche.

## Come funziona

Keylegend chiede a Windows quale carattere produrrebbe ogni tasto nello stato attuale della
tastiera (`ToUnicodeEx`), ne ricava una categoria e invia la mappa di colori risultante all'SDK
Razer Chroma attraverso la sua interfaccia REST locale.

Deliberatamente **non** installa alcun hook globale della tastiera. Legge *stati* — se un tasto è
premuto in questo momento — e non intercetta, non inoltra e non registra mai una battuta. Senza un
effetto di digitazione scelto guarda solo lo stato dei modificatori e dei blocchi; un effetto chiede
in più quali tasti di questa tastiera siano premuti, e nulla oltre.
Vedi [docs/it/architecture.md](docs/it/architecture.md).

## Requisiti

- Windows 10 o 11
- Razer Synapse con il servizio Chroma SDK in esecuzione
- Una tastiera Razer Chroma, collegata (vedi sotto)
- Il runtime .NET 10

## Installazione

```powershell
winget install Eistee82.Keylegend
```

È la via più breve: winget porta con sé il runtime .NET come dipendenza dichiarata, quindi non
resta nulla da installare a mano. Altrimenti, prendi un file:

[**Scarica l'ultima versione.**](https://github.com/Eistee82/Keylegend/releases/latest)

| File | Che cos'è |
|---|---|
| `Keylegend-1.2.0-setup.exe` | Si installa per l'utente corrente — nessun diritto di amministratore. Voce nel menu Start, e una disinstallazione che rimuove anche la voce di avvio automatico. |
| `Keylegend-1.2.0-portable.zip` | Lo stesso programma, da estrarre. Tieni le cartelle delle lingue (`de`, `fr`, …) accanto all'eseguibile, altrimenti l'interfaccia torna all'inglese. |

Nessuno dei due è firmato, quindi Windows dichiarerà sconosciuto l'autore: un certificato costa
all'anno più di quanto questo progetto abbia. Ogni versione porta `SHA256SUMS.txt` per verificare
il download, e il registro di compilazione che l'ha prodotta è pubblico.

## Tastiere supportate

**Qualsiasi tastiera Razer Chroma.** Non c’è un elenco né un file per modello, perché Keylegend
non ha bisogno di riconoscere la tua tastiera: la interroga. Razer Synapse descrive quella collegata
— il modello per nome, il layout fisico come numero e i tasti che l’hardware ha davvero. Il
disegno che Razer fa di quel modello fornisce il resto: le misure reali dei tasti, il telaio con la
sua rotella e i tasti multimediali, e i contorni dei caratteri stampati sui cappucci, nella lingua
giusta.

L’unica cosa che il disegno non dice è a quale cella della matrice di illuminazione appartenga
ogni tasto. Quella è una costante del protocollo Chroma, identica su ogni modello — ed è il motivo
per cui nemmeno Synapse ha bisogno di una tabella per modello. Verificato sulla sola tastiera
calibrata a mano: tutti i 105 tasti coincidono.

Il **layout fisico** descrive la *forma* della tastiera, non la lingua con cui scrivi. Quale carattere
produce un tasto viene chiesto a Windows in esecuzione, così una tastiera tedesca funziona
correttamente anche con Windows impostato su US o Dvorak.

**Richiede Razer Synapse**, installato e in esecuzione, con la tastiera collegata. È lì che la
tastiera viene descritta e lì che si trova il suo disegno.

## Documentazione

| Argomento | |
|---|---|
| Architettura | come si decide la colorazione, e perché non c'è alcun hook della tastiera |
| Aggiungere un profilo | colorazione per applicazione |
| Configurazione | impostazioni, file delle impostazioni, avvio automatico |

Disponibile in undici lingue:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

Inglese e tedesco sono gli originali mantenuti; dove una traduzione li contraddice, è il testo
inglese quello giusto. Le correzioni sono benvenute, vedi [CONTRIBUTING.md](CONTRIBUTING.md).

## Compilare ed eseguire

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd Keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) è tutto il programma: finestra, icona nell'area di notifica,
impostazioni. L'unica opzione che vale la pena conoscere: `--verify` controlla che una copia porti i
profili inclusi e tutte e undici le lingue, scrive quello che trova nel percorso indicato dopo di
essa e risponde tramite il proprio codice di uscita. È ciò che lo script di rilascio esegue contro
una copia impacchettata.

Le impostazioni risiedono in `%APPDATA%\Keylegend\settings.json` e vengono scritte
dall'applicazione.

## Contribuire

Segnalazioni di errori, profili di applicazione e traduzioni sono tutti benvenuti — vedi
[CONTRIBUTING.md](CONTRIBUTING.md) e [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licenza

[MIT](LICENSE). Fanno eccezione due pulsanti di donazione di terzi, e qui non compaiono codice,
header, librerie o materiali grafici di alcun produttore — vedi [NOTICE.md](NOTICE.md).

## Avviso sui marchi

Questo progetto **non è affiliato a Razer Inc., né da essa approvato o sponsorizzato.**

RAZER e RAZER CHROMA sono marchi, registrati o meno, di Razer Inc. Sono usati qui unicamente per
identificare l'hardware e l'interfaccia software con cui questo progetto lavora, come consente
l'uso referenziale. Keylegend è un progetto indipendente, mantenuto dalla comunità.

Lo stesso vale per ogni altro nome presente in questo repository. I profili di applicazione e di
gioco nominano circa novanta programmi — Photoshop, Visual Studio Code, Excel, Elden Ring e altri —
e la documentazione nomina produttori e modelli di tastiera. Sono marchi dei rispettivi titolari e
compaiono solo per dire a quale programma o a quale tastiera qualcosa si riferisce. Keylegend non è
associato a nessuno di essi e non contiene né il loro codice né i loro materiali. Vedi
[NOTICE.md](NOTICE.md).