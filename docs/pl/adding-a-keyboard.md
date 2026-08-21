# Dodawanie i poprawianie klawiatury

Obsługa klawiatury to **dane, nie kod**. Nie potrzebujesz C# ani narzędzi budowania — wystarczą
edytor tekstu i własna klawiatura.

Większość tych, którzy tu trafiają, nie musi niczego dodawać, bo profil dla ich układu już
istnieje. Tym profilom brakuje jedynej rzeczy, której nie da się wygenerować: kogoś, kto ze
sprzętem pod ręką potwierdzi, że każdy klawisz zapala się tam, gdzie profil twierdzi. **To zadanie
opisane w [części 2](#2-poprawianie-profilu), i zajmuje jakieś dziesięć minut.**

---

## Co profil wie i jak bardzo jest tego pewien

Profil odpowiada na dwa odrębne pytania, a nie są one równie wiarygodne:

| Pytanie | Skąd bierze się odpowiedź | Jak pewna |
|---|---|---|
| Gdzie leży każdy klawisz i jak jest duży? | Znormalizowana siatka 19,05 mm, której każda klawiatura trzyma się od czasów IBM Model M | **Pewna.** Geometria wynika z układu. |
| Która komórka macierzy LED zapala ten klawisz? | Opublikowana przez producenta macierz, przy założeniu typowej klawiatury | **Domysł.** Modele przesuwają klawisze, zostawiają komórki nieobsadzone i dodają własne. |

Ten podział to cały powód istnienia znacznika `verified`. Profil oznaczony `"verified": false"`
niemal na pewno ma rację co do rysunku i całkiem możliwe, że myli się co do tego, który klawisz się
zapala.

---

## 1. Dodanie brakującego układu

Najpierw sprawdź, czy naprawdę brakuje: w `devices/` są już pełnowymiarowe profile dla ANSI-US,
ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL, JIS-JP i
ABNT2-BR, a do tego warianty tenkeyless, 75 %, 65 % i 60 %. Jeśli twój jest wśród nich, przejdź do
części 2.

### Droga generowana

`tools/make-layout.py` buduje profil ze znormalizowanych wymiarów. Dodanie do niego klawiatury to
jeden wpis na liście `PROFILES` na końcu pliku:

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argument | Co ustala |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` lub `abnt2` — kształt Entera i to, jakie dodatkowe klawisze istnieją |
| `legends` | Który zestaw nadrukowanych opisów zastosować: `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` albo `fn` — co siedzi między prawym Altem a klawiszem menu |

Potem uruchom:

```bash
python tools/make-layout.py --only iso-tr
```

Jeśli opisów twojej klawiatury nie ma wśród pięciu zestawów, dodaj jeden: skopiuj `LEGENDS_EN` w
tym samym pliku, przetłumacz wpisy i zarejestruj go w `LEGEND_SETS`. Opisu potrzebują tylko
klawisze, które *nic* nie piszą — o pozostałe pyta się Windows w czasie działania, i to właśnie
sprawia, że jeden profil obsługuje każdy układ programowy na tym samym sprzęcie.

### Droga pisana ręcznie

Dla klawiatury, która nie jest odmianą układu standardowego — ortoliniowej, dzielonej, z rzędem
klawiszy makro, którego nikt inny nie ma — napisz `device.json` wprost. [Opis
formatu](device-profile-format.md) wymienia każde pole, a `devices/device-profile.schema.json` daje
większości edytorów uzupełnianie i błędy w linii.

Pierwsze podejście nie musi być dokładne. Ustaw klawisze mniej więcej dobrze, zostaw `row` i
`column` na `null` wszędzie tam, gdzie masz wątpliwości, a resztę zostaw kalibracji.

---

## 2. Poprawianie profilu

To ta część, która wymaga sprzętu, i ta, na której naprawdę zależy.

### Najpierw popatrz

Zanim dotkniesz klawiatury, obejrzyj rysunek:

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-pl/device.json
```

To zapisze `preview.svg` obok profilu; otwórz go w dowolnej przeglądarce. Porównaj z klawiaturą
przed sobą i szukaj:

- brakujących klawiszy albo narysowanych klawiszy, których twoja klawiatura nie ma
- Entera o złym kształcie — wysokiego i w kształcie litery L przy ISO, szerokiego i płaskiego przy
  ANSI
- dolnego rzędu z niewłaściwą liczbą modyfikatorów, co zmienia się bardziej niż cokolwiek innego
- **czerwonych obrysów**, którymi oznaczone są klawisze bez komórki macierzy. Te nigdy się nie
  zapalą.

Poprawianie geometrii to rachunek, nie zgadywanie: siatka to jedna jednostka na klawisz, a
jednostka to `width`, którą mają zwykłe klawisze literowe.

### Potem kalibruj

Kalibracja zapala jeden klawisz naraz i go nazywa, żebyś mógł potwierdzić, że klawisz świecący na
biało jest tym, który profil deklaruje. Tylko tak da się mieć pewność; wszystko inne to wnioskowanie
z tabeli producenta.

```bash
keylegend-cli --profile devices/<twój-katalog>/device.json --calibrate
```

Przechodzi przypisane klawisze w kolejności czytania:

| Klawisz | Co robi |
|---|---|
| `Enter` albo `→` | ten się zgadza, dalej |
| `F` | zapalił się zły klawisz — zanotuj |
| `←` | jeden klawisz wstecz |
| `A` | zapalić wszystkie przypisane klawisze naraz |
| `S` | przeskoczyć do podsumowania |
| `Q` albo `Esc` | zakończyć |

Ponieważ identyfikatory klawiszy trzymają się układu amerykańskiego, komunikat pokazuje dodatkowo,
co dany klawisz naprawdę pisze na *twoim* komputerze — na polskiej klawiaturze usłyszysz więc o
„klawiszu ł", a nie o `Keyboard_SemicolonAndColon`.

Ustalenia są zapisywane do `calibration-findings.txt` na bieżąco, nie na końcu. Kalibracja to
cierpliwa praca i zamknięte okno nie może cię jej kosztować.

Podczas pracy pomaga drugi rysunek — ten opisuje każdy klawisz komórką, której się domaga, zamiast
jego nadrukiem:

```bash
python tools/preview-layout.py devices/<twój-katalog>/device.json --cells
```

### Zastosuj to, co znalazłeś

`tools/apply-calibration.ps1` zapisuje ustalenia z powrotem do profilu, zachowując kopię `.bak`:

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<twój-katalog>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` dotyczy klawiszy, przy których nic się nie zapaliło: macierz potrafi zaadresować komórkę,
ale ten konkretny model nie ma tam diody. Takie klawisze zachowują geometrię — klawisz przecież
istnieje, a podgląd powinien go rysować — i tracą `row`/`column`, żeby nic nie szło w próżnię.
`-Remap` dotyczy klawiszy przypisanych do złej komórki.

### Czego się spodziewać

Oto miejsca, w których wygenerowany profil myli się najczęściej:

| Gdzie | Co się dzieje |
|---|---|
| **Enter w układzie ISO** | Obejmuje dwie komórki. W wielu klawiaturach tylko dolna ma diodę, a górną połowę oświetla sąsiadka albo nic. |
| **Dolny rząd** | Liczba i szerokość modyfikatorów różnią się między modelami. Klawiatury do gier stawiają `Fn` tam, gdzie biurowe mają drugi klawisz Windows. |
| **Klawisze makro i multimedialne** | Często w kolumnie 0 albo w kolumnach zewnętrznych, i często w żadnej komórce. |
| **Klawiatury kompaktowe** | Macierz zachowuje pełne 6 × 22; klawiatura 60 % zostawia po prostu większość pustą. Komórki nie są przenumerowywane. |
| **Wysokie klawisze bloku numerycznego** | Plus i Enter zajmują dwa rzędy, ale odpowiadają na jedną komórkę — zwykle górną. |

Klawisz, który okaże się pozbawiony diody, zachowuje geometrię i traci komórkę:

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Nadal jest rysowany, więc podgląd pasuje do sprzętu; po prostu nigdy się nie zapala. Tak jest
poprawnie, to nie usterka.

### Oznacz jako sprawdzony

Kiedy każda komórka się zgadza, przekaż temu samemu skryptowi `-MarkVerified` albo wpisz ręcznie
`"verified": true` i usuń `note` mówiącą, że profil został wygenerowany. Ten znacznik mówi kolejnej
osobie z twoją klawiaturą, że może mu zaufać.

---

## 3. Przetestuj

```bash
dotnet test
```

Testy dostarczonych profili sprawdzają każdy profil w `devices/`, także twój. Wyłapują powtórzone
identyfikatory, dwa klawisze roszczące sobie prawo do tej samej diody, klawisze narysowane jeden na
drugim, komórki poza macierzą i geometrię, która zsunęła się poza płaszczyznę.

## 4. Otwórz pull request

Napisz, którą klawiaturę i który układ fizyczny sprawdziłeś oraz czy przeszedłeś kalibrację.
Zobacz [CONTRIBUTING.md](../../CONTRIBUTING.md).

Profile z `"verified": false` też są mile widziane — dają przewagę kolejnej osobie z taką
klawiaturą. Poprawka istniejącego profilu jest warta tyle samo co nowy profil.

### O obrazach

Pole `image` jest opcjonalne i obecnie nieużywane: podgląd jest rysowany z geometrii, dzięki czemu
pozostaje ostry w każdym rozmiarze i nie może przeczyć profilowi. Jeśli mimo to dołączasz obraz,
musi to być obraz, który **ty** sfotografowałeś albo narysowałeś. Rendera produktowego producenta
nie da się wydać na licencji MIT tego projektu, a pull request zawierający taki render dostanie
prośbę o jego usunięcie.

## Zobacz też

- [Format profilu urządzenia](device-profile-format.md) — każde pole, szczegółowo
- [Architektura](architecture.md) — dlaczego znaczenie klawiszy pochodzi z Windows, a nie z tabeli
