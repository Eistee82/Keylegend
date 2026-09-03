# Architektura

## Główna myśl

Cała logika decyzyjna to **czyste obliczenie**, bez dostępu do Windows, sieci ani systemu plików:

```
(stan klawiatury, podłączona klawiatura, profil aplikacji, ustawienia kolorów) → kolor każdego klawisza
```

Wynikają z tego dwie rzeczy i obie tłumaczą, dlaczego projekt ma taki kształt:

1. Podgląd na ekranie i prawdziwa klawiatura są wypełniane **tym samym kodem**. To, co widzisz w
   oknie, jest tym, co się zapala.
2. Logikę da się w całości przetestować bez podłączonej klawiatury i bez zainstalowanego Synapse.

Wszystko, co rozmawia ze światem zewnętrznym, siedzi w cienkich adapterach wokół tego rdzenia.

## Projekty

| Projekt | Zawiera | Może zależeć od |
|---|---|---|
| `Keylegend.Core` | podłączona klawiatura, kategorie, zestawy skrótów, kompozytor klatek, automat stanów sesji | niczego zależnego od platformy |
| `Keylegend.Windows` | stan klawiatury, rozstrzyganie znaków, okno pierwszoplanowe | API Windows |
| `Keylegend.Chroma` | klient REST dla Chroma SDK, bicie serca | sieci |
| `Keylegend.Engine` | pętla, która czyta klawiaturę, składa klatkę i ją wysyła | Core, Chroma, Windows |
| `Keylegend.App` | interfejs WPF, ikona w obszarze powiadomień, przechowywanie konfiguracji | wszystkiego powyższego |

`Keylegend.Core` nigdy nie może odwoływać się do pozostałych. Jeśli zmiana zdaje się tego wymagać,
to abstrakcja jest w złym miejscu.

## Odczytywanie stanu klawiatury

Keylegend **nie** instaluje globalnego haka klawiatury. Taki hak jest funkcjonalnie keyloggerem,
siedzi w łańcuchu wejścia i bywa regularnie oznaczany przez systemy antycheatowe.

Zamiast tego stan interesujących klawiszy jest odpytywany (`GetAsyncKeyState` dla przytrzymanych
modyfikatorów, `GetKeyState` dla blokad) około sześćdziesiąt razy na sekundę, a nowa klatka
powstaje tylko wtedy, gdy coś się zmieniło. Żadne naciśnięcie nigdy nie jest przechwytywane,
przekazywane, zapisywane w dzienniku ani przechowywane.

Przy wybranym efekcie pisania to samo odpytywanie sięga aż do klawiszy, które zgłasza podłączona
klawiatura, zamiast kończyć się na modyfikatorach. To to samo pytanie zadane większej liczbie
klawiszy — czy ten jest w tej chwili wciśnięty — i zadaje się je tylko wtedy, gdy efekt jest
wybrany; bez efektu na poszczególne klawisze nigdy się nie patrzy. To, co z tego zostaje, jest
niewielkie i nietrwałe: `KeyActivity` pamięta, kiedy każdy klawisz opadł i wrócił, i zapomina
klawisz, którego nikt nie tknął od kilku sekund. Jedynym wyjątkiem jest efekt ciepła, który trzyma
na klawisz gasnącą liczbę, dopóki ten nie ostygnie — ślad pisania w pamięci, nigdzie niezapisany i
przemijający z procesem.

### Modyfikatory lewe i prawe

Windows zgłasza **AltGr jako Ctrl plus prawy Alt**, a w układach niemieckich Ctrl + lewy Alt daje
te same znaki co AltGr. Rozróżnia się je po stronie:

- **prawy Alt** → warstwa AltGr, pokazująca przypisanie znaków
- **Ctrl + lewy Alt** → zestaw skrótów `Ctrl+Alt`

Warianty lewy i prawy trzeba więc oceniać osobno (`VK_LMENU`/`VK_RMENU` i tak dalej).

## Ustalanie, co znaczy klawisz

Zamiast wozić ze sobą tabelę układów, Keylegend pyta Windows, jaki znak dałby klawisz w bieżącym
stanie klawiatury (`ToUnicodeEx`), i wyprowadza kategorię z otrzymanego znaku.

Dlatego Shift, Caps Lock i Num Lock nie wymagają żadnego szczególnego traktowania: ten sam klawisz
po prostu zwraca `A` zamiast `a` i sam trafia do kategorii „wielka litera”. I dlatego też każdy
układ klawiatury działa bez zmian.

### Jaka klawiatura jest podłączona

Pyta się o to Razer Synapse, bo już to wie. Zapisuje opis każdego podłączonego urządzenia do
`…\Razer Chroma SDK\Devices\<guid>.json`: model z nazwy, układ fizyczny jako liczbę, rozmiar
macierzy i skan-kod każdego klawisza, który sprzęt naprawdę ma. `SdkDeviceDescription` to czyta, a o klawiaturze nic nie jest wywnioskowane.

Ten opis powstaje, gdy podnosi się oprogramowanie Razera, a wcześniej go nie ma — przy logowaniu
jest to wyścig, który Keylegend może przegrać: na maszynie, na której to powstawało, plik pojawił
się dziewięćdziesiąt pięć sekund po starcie systemu, a własny wpis autostartu Keylegend zadziałał
osiem sekund później. Szukanie go nie jest więc pojedynczą próbą, której niepowodzenie kończy
program. `AttachedKeyboardSearch` szuka dalej — żwawo, dopóki żadne urządzenie nie jest wymienione,
z rosnącą przerwą, dopóki brakuje tylko rysunku — ikona w obszarze powiadomień powstaje przed
pierwszym spojrzeniem, a silnik jest budowany, gdy tylko klawiatura się pojawi.

Jak klawiatura wygląda, pochodzi z tej samej instalacji. Interfejs Synapse to aplikacja
internetowa, a rysunki, które wczytuje dla urządzenia, zostają w jej pamięci podręcznej:
prostokąty klawiszy z nazwami, kształt obudowy z pokrętłem głośności i paskiem multimedialnym oraz
obrysy znaków nadrukowanych na klawiszach. `SvgLayoutSource` znajduje rysunek podłączonego modelu i
układu dokładnie, a nie po kształcie, bo każdy rysunek dostarczany jest obok obiektu konfiguracji
nazywającego jedno i drugie.

Pobierane są tylko wymiary i obrysy; kolory i stylistyka Razera są pomijane, a nic z tego materiału
nie jest kopiowane do tego repozytorium.

Jedyne, czego nie mówi żadne z nich, to do której komórki macierzy podświetlenia należy klawisz. To
`StandardKeyMatrix`, własna tablica `RZKEY` protokołu, identyczna w każdym modelu.

## Profile aplikacji

Profil wiąże reguły podświetlenia z programem. W zestawie jest ich około dziewięćdziesięciu, a
decyzje, które za nimi stoją, warto wyłożyć, bo żadna z nich nie jest odpowiedzią oczywistą.

### Profile to dane, nie kod

Ta sama zasada co przy obsłudze urządzeń: dodanie profilu to dodanie pliku JSON w `profiles/`, a
kompilacja podnosi go po masce. Nikt nie musi tykać C#, żeby nauczyć Keylegend jakiegoś programu, a
to znaczy, że profil może przygotować, przejrzeć i poprawić ktoś, kto zna wyłącznie ten program.
Gdyby obsługa nowej aplikacji kiedykolwiek wymagała kodu, format byłby zły.

### Osadzone w zestawie, a nie luzem na dysku

Profile aplikacji są skompilowane w zestawie, a nie leżą jako pliki obok pliku wykonywalnego. Trzy
powody, a każdy wystarczyłby sam. Wydanie w jednym pliku zabiera je ze sobą, bez katalogu, który da
się zgubić. Nic na dysku nie da się zmienić przypadkiem, a właśnie to nadaje sens „przywróceniu
wersji dostarczonej” — wersja dostarczona musi być poza zasięgiem, żeby warto było do niej wracać.
A profil, który się nie kompiluje, staje się błędem kompilacji, a nie programem, który po cichu nie
ma żadnych profili.

### Nadpisania idą sekcjami

Zmiana użytkownika nigdy nie jest zapisywana jako kopia profilu. Zapisuje się ją jako nadpisanie
zaczepione o identyfikator profilu, zawierające tylko dotknięte sekcje. Wynikają z tego dwie
rzeczy: przywracanie w ogóle jest możliwe, a nowsze wydanie wciąż może poprawić profil, który ktoś
częściowo zmienił. Identyfikator to nośna część i po opublikowaniu nie wolno go zmieniać — zmiana
nazwy osieroca czyjeś zmiany.

Ziarnistość broni się wobec obu oczywistych alternatyw:

- **Po polu** wygląda schludniej i tworzy stany, których nikt nie skonfigurował. Przemaluj `W`,
  potem przyjmij aktualizację dodającą `Q`, a wynikiem jest mieszanina, której użytkownik nigdy nie
  zbudował i nie potrafi wyjaśnić.
- **Po profilu** to porażka odwrotna. Zmień jedną rzecz, a profil zostaje zamrożony na zawsze; nigdy
  już nie zobaczy żadnej poprawki.

Sekcja to ziarnistość, przy której zmiana wciąż mieści się w jednym zdaniu: zmieniłeś wyróżnienia,
więc wyróżnienia są teraz twoje.

### Profil nakłada się na zestaw ogólny, wpis po wpisie

Skróty są indeksowane według połączenia modyfikatorów, a wpisy profilu kładą się na ogólnych, a nie
na ich miejsce — wpis po wpisie, nie warstwa po warstwie. Photoshop wie, co znaczy `Ctrl+J` w
Photoshopie; nie wie nic o `Win+E`, które Windows przypisuje w całym systemie, ani o `Ctrl+C`, które
obowiązuje wszędzie, gdzie stoi kursor tekstu.

Warstwami znaczyłoby, że profil wymieniający `Ctrl` dla własnych poleceń zabiera całą warstwę, a
schowek jest tego ceną: kopiowanie, wklejanie, wycinanie, cofanie i zaznaczanie wszystkiego gasną w
przeglądarce, w komunikatorze, w terminalu — w programach, w których niemal nic innego się nie robi
poza pisaniem i wklejaniem. Wpisami wygrywa ten, kto wymienia klawisz, dla tego klawisza, i nic
więcej się nie rusza. Opróżnienie całej warstwy jest celowo niemożliwe.

Profil, który nie wymienia żadnej warstwy, zwraca ogólny katalog bez zmian; częsty przypadek nie
zajmuje więc pamięci.

### Skróty i wyróżnienia niosą etykietę

Etykieta mówi, co robi polecenie — „Powiel warstwę”, a nie „Ctrl+J”. Sprzęt nigdy jej nie pokazuje:
diody niosą kolor i nic więcej, więc etykieta nic nie kosztuje w czasie działania. Zwraca się
trzykrotnie gdzie indziej. Podgląd w aplikacji może ją pokazać, test może znaleźć sprzeczności
między wpisami, a przy dziewięćdziesięciu profilach to jedyny sposób, by ktokolwiek sprawdził, czy
wpis jest poprawny. `"j": "Edycja"` nie da się z niczym zestawić; `"j": "Powiel warstwę"` owszem.

### Migracja pliku ustawień w formacie 1

Plik w formacie 1 przechowuje profile w całości, bez identyfikatora i bez zapisu, skąd profil
pochodzi. Nadpisanie potrzebuje identyfikatora, o który się zaczepi, a przywracanie musi wiedzieć,
że istnieje wersja dostarczona, do której można wrócić — taki plik nie potrafi więc powiedzieć,
które z jego wpisów są dostarczone.

Dlatego wszystkie stają się profilami użytkownika. Zachowuje to każdą zmianę, którą ktoś wprowadził,
kosztem tego, że profil dostarczony pojawi się obok zmigrowanej kopii, dopóki jeden z dwóch nie
zostanie usunięty — to właściwy kompromis, bo odczyt przeciwny po cichu kasuje czyjąś pracę.

### Migracja pliku ustawień w formacie 2

Plik w formacie 2 wymienia wszystkie kolory, także nietknięte, i nie potrafi więc powiedzieć, które
z jego wpisów są decyzjami, a które odbitymi wartościami domyślnymi. Uszanowanie ich wszystkich
przypina paletę: poprawiony kolor dostarczony nie dociera wtedy do nikogo, kto kiedykolwiek
uruchomił program.

Format 3 zapisuje tylko to, co odbiega od dostarczonej palety, więc wpis w pliku znaczy, że ktoś go
wybrał. Migracja starszego pliku wymusza odgadnięcie tej różnicy, a założenie jest takie: wpis równy
palecie tamtej wersji jest wartością domyślną, wszystko inne jest wyborem. `PaletteBeforeFormat3`
przechowuje tę paletę jako zamrożoną kopię, zamiast czytać obecną — takie porównanie traci sens w
chwili, gdy paleta znów się zmieni, czyli dokładnie wtedy, gdy jest potrzebne.

Ceną jest to, że kto świadomie wybrał jeden z tych kolorów, traci go. To właściwy kierunek: jedna
osoba wybiera kolor ponownie, wobec wszystkich użytkowników trzymających paletę, której nikt nie
wybrał.

## Rozmowa z klawiaturą

Do Chroma SDK zwracamy się przez jego lokalny interfejs REST. Kolory to liczby całkowite kodowane w
BGR; całą klawiaturę zapisuje się jako macierz 6 × 22. Sesję trzeba utrzymywać przy życiu biciem
serca.

Zmierzone na maszynie deweloperskiej: utworzenie sesji zajmuje 60–125 ms, pierwsza klatka po
przejęciu od działającego efektu Chroma Studio około 500 ms, a każda następna około 2 ms.

### Każda odpowiedź brzmi 200, więc decyduje treść

Usługa odpowiada **na wszystko** kodem HTTP 200, także na żądania, które odrzuciła. Klatka o
błędnym rozmiarze macierzy wraca tak:

```json
{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
```

ze statusem 200. Kto sprawdza sam kod statusu, zgłasza więc powodzenie dla klatek, których
klawiatura nigdy nie pokazała: ciche niepowodzenie, nie do odróżnienia od podświetlenia, które po
prostu się nie zmienia.

Dlatego decyduje `result` w treści: zero oznacza powodzenie, wszystko inne — odrzucenie. Tam, gdzie
usługa podaje `error` otwartym tekstem, jest on przejmowany bez zmian, bo nazywa rzeczywistą
usterkę lepiej niż jakiekolwiek sformułowanie wymyślone tutaj. Kody, z którymi użytkownik może coś
zrobić, są tłumaczone:

| Kod | Znaczenie |
|---|---|
| 4309 | Chroma jest wyłączona dla tego urządzenia w Synapse |
| 1152 | sesję trzyma inna aplikacja |
| 1167 | nie podłączono żadnego urządzenia Chroma |
| 5 | odmówiono dostępu |
| 87 | żądanie było błędne |
| 50 | żądanie nie jest obsługiwane |

Udane nawiązanie sesji nie niesie `result` w ogóle — zwraca zamiast tego dane sesji — więc jego
brak liczy się jako powodzenie.

### Jak często wysyłane są klatki

Wygląda to na szczegół, a nim nie jest: obie oczywiste odpowiedzi są błędne.

**Wysyłanie tylko przy zmianie** zagładza przejęcie. Zwykłe naciśnięcie nie zmienia stanu
klawiatury — robią to tylko modyfikatory i blokady — więc przejęcie daje dokładnie jedną klatkę.
Chroma odrzuca klatki, dopóki wciąż przejmuje kontrolę, i zgłasza dla nich powodzenie, więc ta jedna
klatka może zniknąć i zostawić klawiaturę zamrożoną na poprzednim efekcie, dopóki użytkownik nie
naciśnie przypadkiem modyfikatora.

**Wysyłanie tak szybko, jak się da** rujnuje responsywność. Klatki ustawiają się w kolejce wewnątrz
interfejsu, a zmiana stanu czeka wtedy za wszystkim, co już wysłano — naciśnięcie Shift wyraźnie
potrzebuje sekundy lub dwóch, żeby się pokazać.

Sprawdza się wysyłanie z trzech odrębnych powodów w trzech różnych tempach:

| Powód | Tempo |
|---|---|
| Stan klawiatury się zmienił | natychmiast — zmierzone na 1 ms od końca do końca |
| W ciągu trzech sekund od przejęcia | co 120 ms, dopóki przekazanie się nie ustabilizuje |
| W przeciwnym razie | co 750 ms, wyłącznie jako ubezpieczenie od zgubionej klatki |

## Obsługa sesji

| Stan | Zachowanie |
|---|---|
| **Bezczynny** | Brak sesji. Podświetleniem steruje Chroma Studio. Działa tylko tanie odpytywanie aktywności. |
| **Aktywny** | Sesja otwarta, bicie serca trwa, nowa klatka przy każdej zmianie stanu. |
| **Wstrzymany** | Podświetlenie zwolnione do czasu wznowienia. |

Keylegend przejmuje przy pierwszym naciśnięciu i zwalnia klawiaturę po konfigurowalnym czasie
bezczynności, dzięki czemu wraca twój własny efekt z Chroma Studio. Koszt wybudzenia rzędu 500 ms
płaci się więc dopiero po prawdziwej przerwie, nigdy podczas pisania.

Klawiaturą steruje tylko jedna kopia Keylegend. Dwie otwierałyby dwie sesje dla tego samego
urządzenia; usługa oddaje je jednej z nich, a druga niczego nie podświetla, wciąż zgłaszając
powodzenie — co wygląda dokładnie jak program, który po cichu przestał działać. To, co robi drugie
uruchomienie, zależy od tego, co już działa. Ten sam program z tego samego miejsca oznacza, że ktoś
kliknął ikonę, gdy siedziała w obszarze powiadomień: pojawia się jej okno, a drugie uruchomienie
ustępuje — nic nie zostaje zamknięte i podświetlenie nie mruga. Wszystko inne — starsza wersja albo
ta sama z innego katalogu — zostaje zastąpione: prosi się ją o zakończenie, oddaje swoją sesję, a
zamykana jest bez pytania dopiero wtedy, gdy nie odpowie w ciągu dwóch sekund.
