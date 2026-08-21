# Format profilu urządzenia

Profil urządzenia opisuje jeden model klawiatury w jednym układzie fizycznym. To pojedynczy plik w
katalogu pod `devices/`, nazwanym `<producent>-<model>-<układ>`:

```
devices/razer-deathstalker-v2-de/
└── device.json     geometria i przypisanie diod
```

`devices/device-profile.schema.json` opisuje to samo w postaci czytelnej dla maszyny. Wskazanie go
w wierszu `$schema`, tak jak robią to dostarczone profile, daje większości edytorów uzupełnianie i
błędy w linii już podczas pisania.

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

| Pole | Znaczenie |
|---|---|
| `formatVersion` | Wersja formatu. Obecnie `1`. Kompilacja odrzuca profil o numerze wyższym, niż rozumie. |
| `name` | To, co pokazuje interfejs. |
| `vendor`, `model` | Kto to robi i który model. `"Generic"` dla profilu opisującego układ, a nie produkt. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — fizyczne *rozmieszczenie* klawiszy, nie układ programowy. |
| `canvas` | Układ współrzędnych, do którego odnoszą się wszystkie pozycje. Liczą się tylko proporcje; dostarczone profile liczą w milimetrach. |
| `matrix` | Rozmiar macierzy LED producenta. Klawiatury Razer mają 6 × 22, niezależnie od wielkości. |
| `verified` | `true`, gdy ktoś potwierdził przypisanie na prawdziwym sprzęcie. |
| `note` | Opcjonalny tekst dla osoby, która otworzy plik jako następna. |
| `image` | Opcjonalne i obecnie nieużywane — zobacz [Obrazy](#obrazy) niżej. |
| `keys[]` | Jeden wpis na klawisz. |

### Układ fizyczny, nie programowy

`physicalLayout` decyduje o *kształcie* klawiatury: czy Enter jest wysoki i w kształcie litery L,
czy jest dodatkowy klawisz na lewo od `Z`, czy dolny rząd niesie japońskie klawisze konwersji.

Nie mówi nic o tym, jakie znaki te klawisze wytwarzają. O to Keylegend pyta Windows w czasie
działania, dla aktualnie aktywnego układu. Profil ISO-PL obsłuży więc polską klawiaturę niezależnie
od tego, czy Windows ustawiony jest na polski, amerykański czy Dvoraka — i dlatego jest jeden
profil na układ *fizyczny*, a nie jeden na język.

### Wpisy klawiszy

| Pole | Znaczenie |
|---|---|
| `id` | Unikatowy identyfikator. Trzymaj się istniejącego nazewnictwa: `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Położenie lewego górnego narożnika na płaszczyźnie. |
| `width`, `height` | Rozmiar klawisza na płaszczyźnie. |
| `row`, `column` | Komórka w macierzy LED producenta. Oba `null`, dopóki nieznane — to stan poprawny i właśnie po to jest kalibracja. |
| `scanCode` | Zastępuje standardowy skankod. Potrzebny tylko tam, gdzie układ fizyczny przeczy nazewnictwu amerykańskiemu. |
| `parts` | Kolejne prostokąty należące do tego samego klawisza, dla klawiszy, które nie są prostokątne. |
| `label` | To, co jest nadrukowane na klawiszu, dla klawiszy, które nic nie piszą. |
| `labelSecondary` | Druga nadrukowana linia, pod pierwszą. |

### Opisy należą do klawiatury

`label` to, co jest *nadrukowane na klawiszu*, a nie tłumaczenie tego, co klawisz robi. Niemiecka
klawiatura mówi `strg`, francuska `ctrl`, włoska `bloc maiusc` — i każda mówi to niezależnie od
tego, na jaki język ustawione są menu Keylegend. Zmiana języka interfejsu nigdy nie zmienia opisów.

Klawisze wytwarzające znak nie noszą `label` w ogóle. Ich opis pochodzi z aktywnego układu Windows
i sam z siebie podąża za Shiftem, Caps Lockiem i AltGr.

### Klawisze o więcej niż jednym prostokącie

Enter w układzie ISO to przypadek wzorcowy: jeden klawisz na dwóch rzędach.

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

Prostokąt główny niesie komórkę, `parts` dodaje resztę kształtu. Wyraźny `scanCode` jest tam
dlatego, że górna połowa zajmuje pozycję, którą ANSI przeznacza na ukośnik odwrotny: bez niego
wierzch Entera byłby kolorowany tak, jakby pisał `\`.

### Skankody klawiszy występujących tylko w jednym układzie

Standardowa tabela w `Keylegend.Core` obejmuje to, co ma klawiatura amerykańska. Klawisze
występujące tylko gdzie indziej podają swój kod w profilu, żeby dla układu nie trzeba było zmieniać
C#:

| Identyfikator | Klawisz | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, na lewo od Backspace w JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, na prawo od prawego Shifta w JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, na lewo od spacji | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, na prawo od spacji | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | klawisz `/?` na prawo od prawego Shifta w ABNT-2 | `0x73` |

## Reguły, których pilnuje walidator

Sprawdza je integracja ciągła, więc profil, który je łamie, nie może zostać scalony:

- Identyfikatory klawiszy są unikatowe
- Żadne dwa klawisze nie roszczą sobie prawa do tej samej komórki macierzy
- Żadne dwa klawisze nie zachodzą na siebie na płaszczyźnie
- `row` i `column` są albo oba ustawione, albo oba `null`
- Komórki mieszczą się w zadeklarowanej macierzy
- Klawisze mieszczą się na płaszczyźnie
- Każdy klawisz ma dodatni rozmiar
- Obraz wskazany przez `image` naprawdę istnieje

## Nazewnictwo i różnica ISO/ANSI

Identyfikatory klawiszy trzymają się układu amerykańskiego, bo tak samo robi macierz producenta. Na
klawiaturze niemieckiej fizyczne `Z` siedzi więc pod `Keyboard_Y` i odwrotnie. Dotyczy to wyłącznie
nazwy: ani położenie, ani zachowanie od tego nie zależą, bo o rzeczywisty znak pyta się Windows w
czasie działania.

Dwa identyfikatory istnieją tylko na klawiaturach ISO:

| Identyfikator | Klawisz | Komórka Razer |
|---|---|---|
| `Keyboard_NonUsBackslash` | dodatkowy klawisz na lewo od `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, rząd 4 kolumna 2 |
| `Keyboard_NonUsTilde` | klawisz obok Entera w rzędzie środkowym (`#`, `'`) | `RZKEY_EUR_1`, rząd 3 kolumna 13 |

Na klawiaturach ISO wysoki Enter obejmuje dwie pozycje macierzy: górną połowę tam, gdzie ANSI ma
ukośnik odwrotny (rząd 2, kolumna 14), dolną pod `Keyboard_Enter` (rząd 3, kolumna 14).

**To, czy obie naprawdę się zapalają, zależy od modelu.** Tabela producenta opisuje, co macierz
potrafi *zaadresować*, a nie co dana klawiatura ma *obsadzone*. W DeathStalker V2 kalibracja
pokazała, że górna komórka nie steruje żadną diodą — cały Enter oświetla dolna, i właśnie dlatego
dostarczony profil modeluje Enter jako jeden klawisz z dwoma prostokątami, a nie jako dwa klawisze.

To dokładnie ten rodzaj rzeczy, którego nie da się wywnioskować z żadnej dokumentacji, i powód, dla
którego profilu nie należy oznaczać jako `verified`, dopóki ktoś nie przeszedł go na sprzęcie.

## Obrazy

`image` jest opcjonalne i obecnie nieużywane: podgląd na ekranie rysowany jest z powyższej
geometrii. Rysowanie utrzymuje podgląd ostry przy każdym rozmiarze okna i uniemożliwia, by obraz i
profil przeczyły sobie nawzajem.

Jeśli mimo to dołączasz obraz, musi to być obraz, który **ty** wykonałeś albo stworzyłeś. Całe to
repozytorium ukazuje się na licencji MIT, która daje każdemu prawo do zmiany i rozpowszechniania
jego zawartości — prawa, którego nikt nie może udzielić do fotografii produktowej producenta
klawiatur. Zobacz [NOTICE.md](../../NOTICE.md).

## Zobacz też

- [Dodawanie i poprawianie klawiatury](adding-a-keyboard.md) — praktyczny przebieg
