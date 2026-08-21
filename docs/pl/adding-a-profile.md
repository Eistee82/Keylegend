# Dodawanie profilu

Profil aplikacji to **dane, nie kod**. Nie potrzebujesz C# ani narzędzi budowania — wystarczą
edytor tekstu i rzeczywista znajomość programu, a ta druga część jest trudniejsza.

Jeśli chcesz profil tylko dla siebie, zrób go w interfejsie: zapisuje się w `settings.json` i nic z
tego nie potrzebuje. Plik w `profiles/` to sposób, w jaki profil trafia z aplikacją do wszystkich.

## 1. Utwórz plik

```
profiles/apps/<id>.json      programy
profiles/games/<id>.json     gry
```

Nazwa pliku musi być taka sama jak `id` w środku. Małe litery, `a-z0-9-`. Kompilacja osadza po
masce każdy plik z tych dwóch katalogów, więc nie ma żadnego pliku projektu do edycji.

Identyfikator jest trwały. Zaczepiają się o niego nadpisania użytkownika i wpisy ukrytych profili,
więc zmiana nazwy w późniejszym wydaniu osieroca czyjeś zmiany. Wybierz nazwę, która będzie
poprawna także po zmianie marki programu — `adobe-photoshop`, a nie `photoshop-2026`.

## 2. Wypełnij go

Pola, trzy sekcje, grupy funkcji, kombinacje modyfikatorów i konwencje kolorów opisano w
[profiles/FORMAT.md](../../profiles/FORMAT.md). Przeczytaj to najpierw; to jest dokumentacja
odniesienia, a ta strona jej nie powtarza.

Poniżej to część, która psuje się nawet wtedy, gdy format został przeczytany.

## 3. Pozycje i znaki to nie to samo

Identyfikatory klawiszy pochodzą z profilu urządzenia i nazywają **pozycje amerykańskie**.
`Keyboard_Y` to fizyczny klawisz, który na klawiaturze amerykańskiej pisze `Y` — na niemieckiej ten
klawisz pisze `Z`. Format ma więc dwa sposoby nazywania klawisza, a wybranie złego daje profil
widocznie błędny na każdym układzie nieamerykańskim, choć wyglądający idealnie na maszynie, na
której powstał.

Przy każdym wpisie trzeba zapytać, o co tak naprawdę chodzi:

- **Gdzie leży ręka → pozycja.** Wyróżnienie dla WASD dotyczy kształtu, jaki układają palce, a nie
  liter. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`, `Keyboard_D` są wszędzie właściwymi klawiszami.
- **Jakie jest polecenie → znak.** `Ctrl+Z` znaczy „klawisz, który pisze z". Zapisane jako pozycja,
  cofnij i ponów wyglądają na niemieckiej klawiaturze na zamienione.
- **Klawisze, które nic nie piszą → znowu pozycja.** Esc, Tab, Enter, Backspace, strzałki i
  klawisze funkcyjne nie mają znaku, więc `shortcuts.keys` nazywa je po identyfikatorze bez
  dwuznaczności.

### Przy wyróżnieniach zależy to od tego, jak program czyta klawiaturę

QWERTZ i QWERTY różnią się dokładnie w dwóch miejscach, więc `Keyboard_Y` i `Keyboard_Z` to jedyne
identyfikatory, przy których może pójść źle. I idzie źle po cichu.

Identyfikator wyróżnienia to zawsze **pozycja fizyczna**. Pytanie brzmi, o który fizyczny klawisz
programowi chodzi, a to wynika z tego, jak czyta on klawiaturę:

| Program wiąże się ze | Przykłady | `Z` w jego dokumentacji znaczy |
|---|---|---|
| **znakiem** (wirtualne kody klawiszy Windows, które podążają za układem) | Photoshop, Blender, GIMP, Krita — aplikacje ogólnie | `Keyboard_Y` — klawisz z górnego rzędu, który na niemieckiej klawiaturze pisze `Z` |
| **pozycją** (skankody, jak w większości silników gier, żeby WASD nie wędrowało) | gry ogólnie | `Keyboard_Z` — klawisz z dolnego rzędu |

Jeśli nie potrafisz ustalić, w jaki sposób dany program czyta klawiaturę, pomiń wpisy `Y` i `Z`.
Każdej innej litery to nie dotyczy.

## 4. Pomiń to, czego nie jesteś pewien

Zły skrót jest gorszy od brakującego. Brakujący wpis zostawia klawisz ciemny i nic nie kosztuje;
zły sprawia, że klawiatura twierdzi coś nieprawdziwego, a użytkownik nie ma jak się dowiedzieć, że
to nieprawda. Etykieta czyni to twierdzenie wyraźnym — nie czyni go poprawnym.

Zatem:

- Zapisuj tylko to, co na pewno jest **domyślnym** przypisaniem programu, prosto po instalacji.
  Twoja własna instalacja nie jest źródłem; prawdopodobnie coś pozmieniałeś i o tym zapomniałeś.
- Sprawdzaj w dokumentacji programu albo w samym programie z nietkniętymi ustawieniami.
- Tam, gdzie domyślne wartości różnią się między wersjami, trzymaj się bieżącej.
- Nie wymyślaj. Jeśli program nie ma powszechnie znanego skrótu do czegoś, nie dostaje wpisu.

Dwanaście poprawnych skrótów jest warte więcej niż trzydzieści, z których cztery są błędne. To samo
dotyczy etykiet wyróżnień: jeśli nie potrafisz powiedzieć, co robi klawisz, to znak, że wpis
jeszcze nie należy do profilu.

## 5. Przetestuj

```bash
dotnet test
```

Testy profili sprawdzają każdy plik w `profiles/`: identyfikator jest unikatowy i zgodny z nazwą
pliku, `kind` zgadza się z katalogiem, każdy identyfikator klawisza istnieje w dostarczonym profilu
urządzenia, kolory dają się odczytać, grupy i kombinacje modyfikatorów są poprawne i zapisane w
postaci kanonicznej, każdy skrót niesie etykietę, żaden klawisz literowy nie stoi pod
`shortcuts.keys` (jego miejsce jest pod `characters`), żaden profil nie jest pusty i żadne dwa
profile nie roszczą sobie prawa do jednego pliku wykonywalnego, nie odróżniając się przez
`titleContains`.

Jedna rzecz celowo **nie** jest sprawdzana: ta sama etykieta pojawiająca się dwa razy pod jednym
modyfikatorem. Wyglądało to na sposób łapania pomyłek kopiuj-wklej, a łapało prawdziwe warianty —
przeglądarki zamykają kartę zarówno `Ctrl+W`, jak i `Ctrl+F4`. Kontrola, która odpala się na
poprawnych danych, jest gorsza niż jej brak.

Czego żaden test nie sprawdzi, to czy skrót jest *prawdziwy*. Po to jest przegląd i po to każdy wpis
ma etykietę do przejrzenia.

## 6. Wypróbuj z programem

Uruchom Keylegend, przenieś program na pierwszy plan i przytrzymaj modyfikatory, które definiuje
twój profil. Podgląd pokazuje to samo co klawiatura, więc do tego wystarczy laptop bez sprzętu
Chroma. Porównaj z menu samego programu — polecenie, którego etykiety nie znajdziesz w programie,
usuń jako pierwsze.

## 7. Otwórz pull request

Napisz, z jakim programem i którą wersją to sprawdziłeś oraz jak zweryfikowałeś przypisania:
dokumentacją programu, samym programem czy jednym i drugim. Zobacz
[CONTRIBUTING.md](../../CONTRIBUTING.md).

Mały, pewny profil to dobry wkład. Duży, w połowie zapamiętany — nie.
