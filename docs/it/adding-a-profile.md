# Aggiungere un profilo

Un profilo di applicazione è **un dato, non codice**. Non servono né C# né strumenti di
compilazione: bastano un editor di testo e una conoscenza reale del programma, e la seconda parte
è quella più difficile.

Se vuoi un profilo solo per te, fallo nell'interfaccia: viene salvato in `settings.json` e non ha
bisogno di nulla di tutto ciò. Un file sotto `profiles/` è il modo in cui un profilo viene
distribuito con l'applicazione per tutti.

## 1. Creare il file

```
profiles/apps/<id>.json      programmi
profiles/games/<id>.json     giochi
```

Il nome del file deve essere uguale all'`id` contenuto dentro. Minuscole, `a-z0-9-`. La
compilazione incorpora con un carattere jolly ogni file di queste due cartelle, quindi non c'è
alcun file di progetto da modificare.

Un identificatore è definitivo. Le sostituzioni dell'utente e le voci dei profili nascosti vi si
agganciano, perciò rinominarne uno in una versione successiva rende orfane le modifiche di
qualcuno. Scegli un nome che sarà ancora giusto dopo un cambio di marchio del programma:
`adobe-photoshop`, non `photoshop-2026`.

## 2. Riempirlo

I campi, le tre sezioni, i gruppi di funzioni, le combinazioni di modificatori e le convenzioni
sui colori sono descritti in [profiles/FORMAT.md](../../profiles/FORMAT.md). Leggilo prima; è il
riferimento e questa pagina non lo ripete.

Quel che segue è la parte che va storta anche quando il formato è stato letto.

## 3. Posizioni e caratteri non sono la stessa cosa

Gli identificatori dei tasti vengono dal profilo di dispositivo e nominano **posizioni
americane**. `Keyboard_Y` è il tasto fisico che scrive `Y` su una tastiera americana; su una
tedesca quel tasto scrive `Z`. Il formato offre quindi due modi di nominare un tasto, e sceglierne
uno sbagliato produce un profilo visibilmente errato su ogni layout non americano, pur sembrando
perfetto sulla macchina su cui è stato scritto.

La domanda da porsi per ogni voce è di che cosa si tratti davvero:

- **Dove sta la mano → posizione.** Un'evidenziazione per WASD riguarda la forma che prendono le
  tue dita, non le lettere. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` sono i tasti
  giusti ovunque.
- **Qual è il comando → carattere.** `Ctrl+Z` significa «il tasto che scrive z». Scritto come
  posizione, annulla e ripristina appaiono scambiati su una tastiera tedesca.
- **Tasti che non scrivono nulla → di nuovo posizione.** Esc, Tab, Invio, Backspace, le frecce e i
  tasti funzione non hanno carattere, quindi `shortcuts.keys` li nomina per identificatore senza
  ambiguità.

### Per le evidenziazioni dipende da come il programma legge la tastiera

QWERTZ e QWERTY differiscono esattamente in due punti, quindi `Keyboard_Y` e `Keyboard_Z` sono gli
unici identificatori dove questo può andare storto. E va storto in silenzio.

L'identificatore di un'evidenziazione è sempre una **posizione fisica**. La domanda è quale tasto
fisico intenda il programma, e ciò discende da come legge la tastiera:

| Il programma si lega | Esempi | `Z` nella sua documentazione significa |
|---|---|---|
| al **carattere** (codici tasto virtuali di Windows, che seguono il layout) | Photoshop, Blender, GIMP, Krita — le applicazioni in generale | `Keyboard_Y` — il tasto della fila superiore, che su una tastiera tedesca scrive `Z` |
| alla **posizione** (codici di scansione, come fa la maggior parte dei motori di gioco, così WASD non si sposta) | i giochi in generale | `Keyboard_Z` — il tasto della fila inferiore |

Se non riesci a stabilire in che modo un dato programma legge la tastiera, lascia fuori le voci
`Y` e `Z`. Ogni altra lettera non ne risente.

## 4. Lascia fuori ciò di cui non sei sicuro

Una scorciatoia sbagliata è peggio di una mancante. Una voce mancante lascia un tasto spento e non
costa nulla; una sbagliata fa affermare alla tastiera qualcosa di falso, e l'utente non ha modo di
sapere che è falso. L'etichetta rende esplicita l'affermazione, non la rende corretta.

Quindi:

- Scrivi solo ciò di cui sei sicuro che sia l'assegnazione **predefinita** del programma, appena
  installato. La tua installazione non è una fonte; probabilmente hai cambiato delle cose e le hai
  dimenticate.
- Verifica sulla documentazione del programma, o sul programma stesso con le impostazioni intatte.
- Dove i valori predefiniti differiscono fra versioni, segui quella attuale.
- Non inventare. Se un programma non ha una scorciatoia ben nota per qualcosa, non ha una voce.

Dodici scorciatoie corrette valgono più di trenta di cui quattro sbagliate. Lo stesso vale per le
etichette delle evidenziazioni: se non sai dire che cosa fa un tasto, è segno che la voce non
appartiene ancora al profilo.

## 5. Provarlo

```bash
dotnet test
```

I test dei profili controllano ogni file sotto `profiles/`: l'identificatore è univoco e
corrisponde al nome del file, `kind` corrisponde alla cartella, ogni identificatore di tasto
esiste in un profilo di dispositivo incluso, i colori si interpretano, i gruppi e le combinazioni
di modificatori sono validi e scritti in forma canonica, ogni scorciatoia porta un'etichetta,
nessun tasto lettera sta sotto `shortcuts.keys` (il suo posto è sotto `characters`), nessun
profilo è vuoto, e non ci sono due profili che rivendicano lo stesso eseguibile senza distinguersi
tramite `titleContains`.

Una cosa **non** viene deliberatamente controllata: la stessa etichetta che compare due volte
sotto uno stesso modificatore. Sembrava un modo per cogliere sviste da copia e incolla e coglieva
invece veri alias — i browser chiudono una scheda sia con `Ctrl+W` sia con `Ctrl+F4`. Un controllo
che scatta su dati corretti è peggio di nessun controllo.

Ciò che nessun test può controllare è se una scorciatoia sia *vera*. A questo serve la rilettura,
ed è la ragione per cui ogni voce porta un'etichetta da rileggere.

## 6. Provarlo contro il programma

Avvia Keylegend, porta il programma in primo piano e tieni premuti i modificatori che il tuo
profilo definisce. L'anteprima mostra la stessa cosa della tastiera, quindi per questo basta un
portatile senza hardware Chroma. Confrontala con i menu del programma stesso: un comando la cui
etichetta non trovi nel programma è la prima cosa da togliere.

## 7. Aprire una pull request

Indica contro quale programma e quale versione hai verificato, e come hai controllato le
assegnazioni: la documentazione del programma, il programma stesso, o entrambi. Vedi
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Un profilo piccolo e sicuro è un buon contributo. Uno grande e ricordato a metà non lo è.
