# Architektura

## Główna myśl

Cała logika decyzyjna to **czyste obliczenie**, bez dostępu do Windows, sieci ani systemu plików:

```
(stan klawiatury, profil urządzenia, profil aplikacji, ustawienia kolorów) → kolor każdego klawisza
```

Wynikają z tego dwie rzeczy i obie tłumaczą, dlaczego projekt ma taki kształt:

1. Podgląd na ekranie i prawdziwa klawiatura są wypełniane **tym samym kodem**. To, co widzisz w
   oknie, jest tym, co się zapala.
2. Logikę da się w całości przetestować bez podłączonej klawiatury i bez zainstalowanego Synapse.

Wszystko, co rozmawia ze światem zewnętrznym, siedzi w cienkich adapterach wokół tego rdzenia.

## Projekty

| Projekt | Zawiera | Może zależeć od |
|---|---|---|
| `Keylegend.Core` | profile urządzeń, kategorie, zestawy skrótów, kompozytor klatek, automat stanów sesji | niczego zależnego od platformy |
| `Keylegend.Windows` | stan klawiatury, rozstrzyganie znaków, okno pierwszoplanowe | API Windows |
| `Keylegend.Chroma` | klient REST dla Chroma SDK, bicie serca | sieci |
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
po prostu zwraca `A` zamiast `a` i sam trafia do kategorii „wielka litera". I dlatego też każdy
układ klawiatury działa bez zmian.

## Profile aplikacji

Profil wiąże reguły podświetlenia z programem. W zestawie jest ich około dziewięćdziesięciu, a
decyzje, które za nimi stoją, warto wyłożyć, bo każda była drugą, a nie pierwszą odpowiedzią.

### Profile to dane, nie kod

Ta sama zasada co przy obsłudze urządzeń: dodanie profilu to dodanie pliku JSON w `profiles/`, a
kompilacja podnosi go po masce. Nikt nie musi tykać C#, żeby nauczyć Keylegend jakiegoś programu, a
to znaczy, że profil może przygotować, przejrzeć i poprawić ktoś, kto zna wyłącznie ten program.
Gdyby obsługa nowej aplikacji kiedykolwiek wymagała kodu, format byłby zły.

### Osadzone w zestawie, a nie luzem na dysku

Profile urządzeń leżą obok pliku wykonywalnego, profile aplikacji nie. Trzy powody, a każdy
wystarczyłby sam. Wydanie w jednym pliku zabiera je ze sobą, bez katalogu, który da się zgubić. Nic
na dysku nie da się zmienić przypadkiem, a właśnie to nadaje sens „przywróceniu wersji
dostarczonej" — wersja dostarczona musi być poza zasięgiem, żeby warto było do niej wracać. A
profil, który się nie kompiluje, staje się błędem kompilacji, a nie programem, który po cichu nie
ma żadnych profili.

### Nadpisania idą sekcjami

Zmiana użytkownika nigdy nie jest zapisywana jako kopia profilu. Zapisuje się ją jako nadpisanie
zaczepione o identyfikator profilu, zawierające tylko dotknięte sekcje. Wynikają z tego dwie
rzeczy: przywracanie w ogóle jest możliwe, a nowsze wydanie wciąż może poprawić profil, który ktoś
częściowo zmienił. Identyfikator to nośna część i po opublikowaniu nie wolno go zmieniać — zmiana
nazwy osieroca czyjeś zmiany.

Ziarnistość wybrano wbrew obu oczywistym alternatywom:

- **Po polu** wygląda schludniej i tworzy stany, których nikt nie skonfigurował. Przemaluj `W`,
  potem przyjmij aktualizację dodającą `Q`, a wynikiem jest mieszanina, której użytkownik nigdy nie
  zbudował i nie potrafi wyjaśnić.
- **Po profilu** to porażka odwrotna. Zmień jedną rzecz, a profil zostaje zamrożony na zawsze; nigdy
  już nie zobaczy żadnej poprawki.

Sekcja to ziarnistość, przy której zmiana wciąż mieści się w jednym zdaniu: zmieniłeś wyróżnienia,
więc wyróżnienia są teraz twoje.

### Profil zastępuje tylko te warstwy, które wymienia

Skróty są indeksowane kombinacją modyfikatorów i nakładane na katalog ogólny, a nie stawiane w jego
miejsce. Photoshop wie, co `Ctrl` znaczy w Photoshopie; nie wie nic o `Win+E`, które Windows
przypisuje w skali systemu i które jest prawdziwe niezależnie od tego, co jest na wierzchu.
Zastąpienie całego katalogu czyniłoby profil odpowiedzialnym za fakty, w sprawie których nie ma
zdania. Profil, który nie wymienia żadnej warstwy, zwraca katalog ogólny bez zmian, więc przypadek
typowy nie alokuje niczego.

### Skróty i wyróżnienia niosą etykietę

Etykieta mówi, co robi polecenie — „Powiel warstwę", a nie „Ctrl+J". Sprzęt nigdy jej nie pokazuje:
diody niosą kolor i nic więcej, więc etykieta nic nie kosztuje w czasie działania. Zwraca się
trzykrotnie gdzie indziej. Podgląd w aplikacji może ją pokazać, test może znaleźć sprzeczności
między wpisami, a przy dziewięćdziesięciu profilach to jedyny sposób, by ktokolwiek sprawdził, czy
wpis jest poprawny. `"j": "Edycja"` nie da się z niczym zestawić; `"j": "Powiel warstwę"` owszem.

### Migracja pliku ustawień w formacie 1

Format 1 przechowywał profile w całości, bez identyfikatora i bez zapisu, skąd profil pochodzi.
Właśnie to naprawia nowy format: nadpisanie potrzebuje identyfikatora, o który się zaczepi, a
przywracanie musi wiedzieć, że istnieje wersja dostarczona, do której można wrócić.

Konsekwencją dla migracji jest to, że stary plik nie potrafi powiedzieć, które z jego wpisów były
kiedyś dostarczone. Wszystkie stają się więc profilami użytkownika. Zachowuje to każdą zmianę, którą
ktoś wprowadził, kosztem tego, że profil dostarczony pojawi się obok zmigrowanej kopii, dopóki jeden
z dwóch nie zostanie usunięty — i to jest właściwy kompromis, bo odczyt przeciwny po cichu
kasowałby czyjąś pracę.

## Rozmowa z klawiaturą

Do Chroma SDK zwracamy się przez jego lokalny interfejs REST. Kolory to liczby całkowite kodowane w
BGR; całą klawiaturę zapisuje się jako macierz 6 × 22. Sesję trzeba utrzymywać przy życiu biciem
serca.

Zmierzone na maszynie deweloperskiej: utworzenie sesji zajmuje 60–125 ms, pierwsza klatka po
przejęciu od działającego efektu Chroma Studio około 500 ms, a każda następna około 2 ms.

### Jak często wysyłane są klatki

Wygląda to na szczegół, a nim nie jest; obie oczywiste odpowiedzi są błędne i obu próbowano.

**Wysyłanie tylko przy zmianie** zagładza przejęcie. Zwykłe naciśnięcie nie zmienia stanu
klawiatury — robią to tylko modyfikatory i blokady — więc przejęcie dawało dokładnie jedną klatkę.
Chroma odrzuca klatki, dopóki wciąż przejmuje kontrolę, i zgłasza dla nich powodzenie, więc ta jedna
klatka mogła zniknąć i zostawić klawiaturę zamrożoną na poprzednim efekcie, dopóki użytkownik nie
nacisnął przypadkiem modyfikatora.

**Wysyłanie tak szybko, jak się da** rujnuje responsywność. Klatki ustawiają się w kolejce wewnątrz
interfejsu, a zmiana stanu czeka wtedy za wszystkim, co już wysłano — naciśnięcie Shift wyraźnie
potrzebowało sekundy lub dwóch, żeby się pokazać.

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
