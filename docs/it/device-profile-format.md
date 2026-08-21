# Formato del profilo di dispositivo

Un profilo di dispositivo descrive un modello di tastiera in un layout fisico. È un unico file in
una cartella sotto `devices/`, chiamata `<produttore>-<modello>-<layout>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometria e corrispondenza dei LED
```

`devices/device-profile.schema.json` descrive la stessa cosa in forma leggibile dalla macchina.
Nominarlo in una riga `$schema`, come fanno i profili inclusi, dà alla maggior parte degli editor
completamento ed errori in linea mentre scrivi.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Campo | Significato |
|---|---|
| `formatVersion` | Revisione del formato. Attualmente `1`. Una compilazione rifiuta un profilo numerato più in alto di quanto comprenda. |
| `name` | Quello che mostra l'interfaccia. |
| `vendor`, `model` | Chi la produce e quale modello. `"Generic"` per un profilo che descrive un layout anziché un prodotto. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — la *disposizione* fisica dei tasti, non il layout software. |
| `canvas` | Il sistema di coordinate a cui si riferiscono tutte le posizioni. Contano solo i rapporti; i profili inclusi ragionano in millimetri. |
| `matrix` | Dimensione della matrice LED del produttore. Le tastiere Razer sono 6 × 22, qualunque sia la loro taglia. |
| `verified` | `true` una volta che qualcuno ha confermato la corrispondenza su hardware reale. |
| `note` | Testo libero facoltativo per chi aprirà il file dopo. |
| `image` | Facoltativo, e al momento inutilizzato — vedi [Immagini](#immagini) più sotto. |
| `keys[]` | Una voce per tasto. |

### Layout fisico, non layout software

`physicalLayout` decide la *forma* della tastiera: se l'Invio è alto e a L, se c'è un tasto in più
a sinistra della `Z`, se la fila inferiore porta i tasti giapponesi di conversione.

Non dice nulla su quali caratteri quei tasti producano. Quello Keylegend lo chiede a Windows in
esecuzione, per il layout attivo. Un profilo ISO-IT serve quindi una tastiera italiana sia che
Windows sia impostato su italiano, su americano o su Dvorak — ecco perché c'è un profilo per
layout *fisico* e non uno per lingua.

### Voci di tasto

| Campo | Significato |
|---|---|
| `id` | Identificatore univoco. Segui la nomenclatura esistente: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Posizione dell'angolo in alto a sinistra sul piano. |
| `width`, `height` | Dimensione del tasto sul piano. |
| `row`, `column` | Cella nella matrice LED del produttore. Entrambi `null` finché sono ignoti — stato valido, ed è a questo che serve la calibrazione. |
| `scanCode` | Sostituisce il codice di scansione standard. Serve solo dove il layout fisico contraddice la nomenclatura americana. |
| `parts` | Ulteriori rettangoli appartenenti allo stesso tasto, per i tasti non rettangolari. |
| `label` | Ciò che è stampato sul tasto, per i tasti che non scrivono nulla. |
| `labelSecondary` | Una seconda riga stampata, sotto la prima. |

### Le diciture appartengono alla tastiera

`label` è ciò che è *stampato sul tasto*, non una traduzione di ciò che fa. Una tastiera tedesca
dice `strg`, una francese `ctrl`, una italiana `bloc maiusc` — e ciascuna lo dice a prescindere
dalla lingua in cui sono i menu di Keylegend. Cambiare la lingua dell'interfaccia non cambia mai
le diciture.

I tasti che producono un carattere non portano alcun `label`. La loro dicitura viene dal layout
Windows attivo, e segue quindi da sé Maiusc, Bloc Maiusc e Alt Gr.

### Tasti con più di un rettangolo

L'Invio ISO è il caso tipico: un tasto che copre due righe.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

Il rettangolo principale porta la cella; `parts` aggiunge il resto della forma. Il `scanCode`
esplicito è lì perché la metà superiore occupa la posizione che ANSI riserva alla barra rovesciata:
senza di esso la parte alta dell'Invio verrebbe colorata come se scrivesse `\`.

### Codici di scansione per tasti presenti su un solo layout

La tabella standard in `Keylegend.Core` copre ciò che ha una tastiera americana. I tasti che
esistono solo altrove dichiarano il proprio codice nel profilo, così non serve cambiare C# per un
layout:

| Identificatore | Tasto | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, a sinistra del Backspace su JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, a destra del Maiusc destro su JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, a sinistra della barra spaziatrice | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, a destra della barra spaziatrice | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | il tasto `/?` a destra del Maiusc destro su ABNT-2 | `0x73` |

## Regole imposte dal validatore

Vengono controllate in integrazione continua, quindi un profilo che le viola non può essere unito:

- Gli identificatori dei tasti sono univoci
- Non ci sono due tasti che rivendicano la stessa cella di matrice
- Non ci sono due tasti che si sovrappongono sul piano
- `row` e `column` sono entrambi impostati o entrambi `null`
- Le celle cadono dentro la matrice dichiarata
- I tasti cadono dentro il piano
- Ogni tasto ha una dimensione positiva
- Un'immagine nominata da `image` esiste davvero

## Nomenclatura e la differenza ISO/ANSI

Gli identificatori seguono il layout americano, perché è ciò che fa la matrice stessa del
produttore. Su una tastiera tedesca la `Z` fisica sta quindi su `Keyboard_Y` e viceversa. Riguarda
solo il nome: né la posizione né il comportamento ne dipendono, perché il carattere reale viene
chiesto a Windows in esecuzione.

Due identificatori esistono solo sulle tastiere ISO:

| Identificatore | Tasto | Cella Razer |
|---|---|---|
| `Keyboard_NonUsBackslash` | il tasto in più a sinistra di `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, riga 4 colonna 2 |
| `Keyboard_NonUsTilde` | il tasto accanto all'Invio nella fila centrale (`#`, `'`) | `RZKEY_EUR_1`, riga 3 colonna 13 |

Sulle tastiere ISO l'Invio alto copre due posizioni di matrice: la metà superiore dove ANSI ha la
barra rovesciata (riga 2, colonna 14), quella inferiore su `Keyboard_Enter` (riga 3, colonna 14).

**Che si accendano davvero entrambe dipende dal modello.** La tabella del produttore descrive ciò
che la matrice può *indirizzare*, non ciò che una data tastiera ha *montato*. Sulla
DeathStalker V2 la calibrazione ha mostrato che la cella superiore non pilota alcun LED: l'intero
Invio è illuminato da quella inferiore, ed è per questo che il profilo incluso modella l'Invio come
un tasto con due rettangoli anziché come due tasti.

È esattamente il genere di cosa che nessuna documentazione permette di dedurre, e la ragione per
cui un profilo non dovrebbe essere contrassegnato `verified` finché qualcuno non l'ha percorso su
hardware.

## Immagini

`image` è facoltativo e al momento inutilizzato: l'anteprima a schermo è disegnata a partire dalla
geometria qui sopra. Disegnarla mantiene l'anteprima nitida a ogni dimensione di finestra e rende
impossibile che immagine e profilo si contraddicano.

Se ne alleghi comunque una, deve essere un'immagine che **tu** hai scattato o realizzato. Tutto
questo repository esce sotto la licenza MIT, che concede a chiunque il diritto di modificare e
ridistribuire ciò che contiene — un diritto che nessuno può concedere sulla fotografia di prodotto
di un produttore di tastiere. Vedi [NOTICE.md](../../NOTICE.md).

## Vedi anche

- [Aggiungere o correggere una tastiera](adding-a-keyboard.md) — il percorso pratico
