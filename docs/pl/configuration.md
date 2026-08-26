# Konfiguracja

Ustawienia znajdują się w `%APPDATA%\Keylegend\` i zmienia się je przez interfejs. Przy pierwszym
uruchomieniu zapisywana jest pełna konfiguracja domyślna.

## Kolory

Jeden kolor na kategorię:

| Kategoria | Dotyczy |
|---|---|
| Cyfra | `1`, `7` oraz bloku numerycznego przy włączonym Num Locku |
| Mała litera | `a`, `ą` |
| Wielka litera | `A`, `Ą` |
| Symbol | `+`, `#`, `€`, `\|` oraz operatorów bloku numerycznego |
| Klawisz sterujący | Esc, Tab, Enter, Backspace, modyfikatory, strzałki, blok nawigacyjny oraz blok numeryczny przy wyłączonym Num Locku |
| Klawisz funkcyjny | F1 do F12 |
| Klawisz martwy | `^`, `´`, `` ` `` — klawisze, które do wytworzenia znaku wymagają drugiego naciśnięcia |
| Nieprzypisane | klawisze bez znaczenia w bieżącym kontekście; domyślnie ciemne. Najjaśniejszym przykładem jest środkowy klawisz bloku numerycznego przy wyłączonym Num Locku |

Klawisze blokad mają po dwa kolory — jeden dla włączonego, drugi dla wyłączonego.

## Zestawy skrótów

Zestaw skrótów przypisuje klawisze do **grup funkcji** i jest wybierany na podstawie
przytrzymywanych modyfikatorów. Zestawy w komplecie: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`,
`Ctrl+Shift`, `Ctrl+Alt`.

Każda grupa ma własny kolor, dzięki czemu pokrewne polecenia czytają się jako blok — na przykład
edycja (`X`/`C`/`V`/`Z`/`Y`/`A`) w jednym kolorze, a operacje na plikach (`N`/`O`/`S`/`P`/`W`) w
innym.

Skróty Windows są ustalone w skali systemu i dlatego zawsze trafne. Skróty z Ctrl różnią się między
programami; dostarczony zestaw obejmuje typowe konwencje Windows.

## Profile aplikacji

Profil opisuje, co klawiatura ma pokazywać, gdy określony program jest na wierzchu. Z aplikacją
przychodzi ich około dziewięćdziesięciu — programy takie jak Photoshop, Visual Studio Code czy
Excel oraz gry takie jak Elden Ring czy Counter-Strike 2. Działają same z siebie: gdy tylko
odpowiednie okno ma fokus, profil obowiązuje, a gdy fokus wędruje dalej, wracają zestawy domyślne.
Tam, gdzie nie pasuje żaden profil, nic się nie zmienia.

Rozpoznawanie odbywa się po nazwie pliku wykonywalnego. Gdy pasuje więcej niż jeden profil, wygrywa
ten, który wskazuje program — gra z własnym profilem zachowuje go więc, choć wykrywanie gier też się
odzywa. Priorytet rozstrzyga tylko pozostałe remisy.

Profil nakłada się na zestaw ogólny, wpis po wpisie. Photoshop mówi, co znaczy tam `Ctrl+J`;
`Ctrl+C` nadal kopiuje, bo profil wymieniający warstwę Ctrl nie twierdzi, że Ctrl nie znaczy nic
innego. A `Win+E` nadal otwiera Eksploratora, bo Windows przypisuje to połączenie w całym systemie i
obowiązuje ono niezależnie od tego, co jest na wierzchu.

### Co zawiera profil

| Sekcja | Zawartość |
|---|---|
| Dopasowanie | Do jakich programów profil się stosuje: nazwy plików wykonywalnych, czy obejmuje wykryte gry w ogóle, oraz priorytet |
| Wyróżnienia | Klawisze przypięte do stałego koloru niezależnie od wytwarzanego znaku — WASD w grze, klawisze narzędzi w edytorze obrazu |
| Skróty | Zamienniki poszczególnych warstw modyfikatorów: który klawisz niesie które polecenie pod `Ctrl`, kolorowany według grupy funkcji |

Wyróżnienia i skróty niosą też etykietę mówiącą, co robi polecenie — „Powiel warstwę”, „Skok”. Nic
z tego nie jest widoczne na klawiaturze; diody pokazują wyłącznie kolor. Etykieta pojawia się w
podglądzie wewnątrz aplikacji, a przy dziewięćdziesięciu profilach to jedyny sposób, by w ogóle
sprawdzić, czy wpis jest poprawny.

### Zmiany i przywracanie

Trzy sekcje są nadpisywane osobno. Zmień wyróżnienia dostarczonego profilu, a od tej chwili
wyróżnienia są twoje: zostają zamrożone i nie podążają już za wersją dostarczoną. Dopasowanie i
skróty nadal za nią podążają i podchwytują ulepszenia, które przynosi nowe wydanie.

Zapisywana jest tylko zmieniona sekcja, pod identyfikatorem profilu — nigdy kopia całego profilu.
Właśnie dlatego istnieje przywracanie i dlatego aktualizacja wciąż może poprawić profil, który
częściowo zmieniłeś.

Przywracanie działa więc również sekcjami: da się oddać skróty, zachowując własne wyróżnienia.
Przywrócenie całego profilu odbiera każdą sekcję, a także zmienioną nazwę i stan ukrycia.

Dostarczone profile można **ukryć, ale nie usunąć**. Żyją wewnątrz pliku programu; usunięcie
jednego przetrwałoby tylko do następnego uruchomienia. Ukryty profil jest pomijany przy wyborze
profilu, ale zostaje na liście i można go znów pokazać.

### Twoje własne profile

Profil, który tworzysz sam, zapisywany jest w całości w `settings.json`, bo nie ma nic, z czym
można by go porównać. Nie da się go zatem przywrócić, tylko usunąć. Poza tym zachowuje się jak
dostarczony: te same trzy sekcje, ta sama zasada wyboru.

Jeśli profil powinien obowiązywać wszystkich, a nie tylko ciebie, jego miejsce jest w projekcie
jako plik — zobacz [Dodawanie profilu](adding-a-profile.md).

### Format pliku ustawień

`settings.json` niesie `formatVersion` 3. Starsze pliki są migrowane przy wczytaniu.

Plik wersji 1 nie zna ani identyfikatorów, ani pochodzenia profilu, więc nie potrafi powiedzieć,
które z jego wpisów są dostarczone. Wszystkie stają się profilami użytkownika. Nic nie ginie, ale
dostarczone profile pojawiają się obok, więc na początku mogą być dwa wpisy dla tego samego
programu; zbędny można usunąć albo ukryć.

Plik wersji 2 wymienia wszystkie kolory, także nietknięte, i tym samym przypina paletę: poprawiony
dostarczony kolor nie dociera do nikogo, kto uruchomił program wcześniej. Kolor równy palecie tamtej
wersji jest więc przy migracji czytany jako wartość domyślna i porzucany; wszystko inne jest
Państwa wyborem i zostaje.

## Zachowanie

| Ustawienie | Znaczenie |
|---|---|
| Oddawaj podświetlenie przy bezczynności | Czy w ogóle jest oddawane. Wyłączone — Keylegend zatrzymuje klawiaturę, dopóki go nie wstrzymasz albo nie zamkniesz, i przejmuje ją przy starcie zamiast czekać na naciśnięcie. |
| Czas bezczynności | Sekundy bez aktywności klawiatury przed oddaniem. Domyślnie 60 — odzyskanie kosztuje sekundę lub dwie, więc krótki czas zamienia to w ciągłe przerywanie. Wartość zachowuje się, gdy oddawanie jest wyłączone. |
| Jasność | Globalny współczynnik od 0 do 100 %, stosowany do każdego koloru przy składaniu klatki. |
| Używaj profili aplikacji | Czy profile są w ogóle brane pod uwagę. Wyłączone — zestawy domyślne obowiązują wszędzie, cokolwiek jest na wierzchu. |
| Uruchamiaj z systemem Windows | Rejestruje aplikację w kluczu `Run`, z przełącznikiem `--minimized`. Uruchomiony w ten sposób Keylegend pojawia się w obszarze powiadomień: bez okna i bez dymka. Uruchomiony ręcznie zawsze pokazuje okno. Wpis zapisany przez wcześniejszą wersję zostaje uaktualniony przy następnym starcie. |

## Język

Interfejs podąża za językiem wyświetlania Windows i jest dostępny w jedenastu językach: angielskim,
niemieckim, hiszpańskim, francuskim, włoskim, niderlandzkim, polskim, portugalskim, rosyjskim,
ukraińskim i chińskim uproszczonym. **Ustawienia → Język** to nadpisuje; przełączenie działa
natychmiast, bez ponownego uruchamiania.

Każdy język nazywa się na tej liście sam, zamiast być tłumaczonym. Tłumaczenie oznaczałoby, że
każdy z jedenastu niesie dziesięć nazw dla pozostałych, a ktoś, komu interfejs otworzył się w
języku, którego nie umie czytać, musiałby szukać własnego w języku, którego też nie umie czytać.

Wybór zapisywany jest w `settings.json` pod `language` jako `Automatic`, `English`, `German`,
`Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian` albo
`ChineseSimplified`. Nieznana wartość cofa się do `Automatic`, zamiast odmawiać uruchomienia — a
tego ręcznie zmieniony plik najpewniej i tak chce.

Przetłumaczone są menu i objaśnienia. Dwie rzeczy **nie** są, obie celowo:

- **Opisy klawiszy** na rysowanej klawiaturze. Pochodzą z rysunku Razera i muszą pasować do
  klawiatury przed tobą, a nie do języka menu — niemiecka klawiatura ISO pokazuje `strg` i `entf`
  niezależnie od tego, czy interfejs chodzi po angielsku.
- **Nazwy modyfikatorów** (Shift, Ctrl, Alt, AltGr, Num Lock …). Te same nazwy wytwarza mechanizm
  skrótów na potrzeby list warstw, a ten stoi poza tłumaczeniem; połowiczne tłumaczenie czytałoby
  się gorzej niż żadne.

Wszystko bez tłumaczenia cofa się do angielskiego, więc niedokończony plik językowy kosztuje
brakujące wiersze, a nie cały interfejs.

## Gdy podświetlenie nie działa

Rozmowa z usługą Chroma może się nie udać: usługa jest zatrzymana, Synapse zamknięto, inny program
trzyma sesję. Keylegend próbuje dalej, z rosnącą przerwą między próbami, i mówi przy tym, co jest
nie tak:

- wiersz stanu na dole okna niesie powód, w barwie amber zamiast zwykłej szarości
- obszar powiadomień mówi to w swojej podpowiedzi, żeby zamknięte okno tego nie ukryło
- jedna chmurka to ogłasza — raz na awarię, a nie raz na próbę

Wszystkie trzy znikają, gdy tylko znów przejdzie klatka. Jeśli nie pojawia się nic, a klawiatura
nadal nie świeci, program nie działa — poszukaj jego ikony w obszarze powiadomień.

## Gdy świecą nie te klawisze

Klawiatura w oknie to klawiatura na biurku: obie wypełnia ten sam kod, więc okno pokazuje, jak sprzęt
powinien wyglądać. Sprawdzeniem jest przystawienie jednej do drugiej.

Do której komórki macierzy podświetlenia należy klawisz, to jedyna rzecz, której nie mówi ani
Synapse, ani rysunek — pochodzi z tabeli samego protokołu Chroma. Jeśli więc na sprzęcie świeci inny
klawisz niż w oknie, ta tabela jest błędna dla twojego modelu. Wtedy warto założyć zgłoszenie z
nazwą klawiatury i klawisza.
