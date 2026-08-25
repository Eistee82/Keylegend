# Keylegend

**Interaktywne podświetlenie klawiatury dla Razer Chroma — klawisze świecą według tego, co naprawdę robią.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Wersja 1.0.0.** Podświetlenie, interfejs, wykrywanie gier i profile aplikacji działają.
> [Pobierz instalator albo wersję przenośną](https://github.com/Eistee82/Keylegend/releases/latest),
> albo zbuduj ze źródeł. Zobacz [CHANGELOG.md](CHANGELOG.md).

![Keylegend koloruje klawisze według tego, co w danej chwili znaczą, i zmienia profil, gdy na pierwszy plan wychodzi inny program](docs/images/keylegend.png)

---

## Co robi

Większość oprogramowania RGB traktuje klawiaturę jak ozdobę. Keylegend traktuje ją jak
**wyświetlacz**.

Każdy klawisz ma kolor tego, co znaczy *w tej chwili* — a kolor zmienia się w momencie, w którym
zmienia się jego znaczenie:

- **Blokady na pierwszy rzut oka.** Num Lock, Caps Lock i Scroll Lock pokazują swój stan na samym
  klawiszu.
- **Kolor według klasy znaku.** Cyfry, małe litery, wielkie litery, symbole i klawisze sterujące
  mają każdy swój kolor.
- **Przytrzymaj modyfikator, zobacz warstwę.** Naciśnij `AltGr`, a zapalone zostaną tylko te
  klawisze, które faktycznie niosą znak AltGr. Naciśnij `Windows`, a zapalą się skróty Windows,
  pogrupowane funkcjami. Tak samo dla `Alt`, `Ctrl` i ich kombinacji.
- **Shift i Caps Lock działają same z siebie.** Ponieważ znak wytwarzany przez każdy klawisz jest
  pobierany z Windows na bieżąco, litery same przechodzą z koloru „mała litera" na kolor „wielka
  litera". Blok numeryczny przebarwia się na nawigację, gdy Num Lock jest wyłączony.
- **Gry mają własne traktowanie.** Są wykrywane automatycznie — także te w oknie bez ramki — a
  WASD, klawisze wokół i rząd cyfr dostają stałe kolory: podczas gry liczy się, gdzie trafiają
  ręce, a nie jaką literę pisze klawisz.
- **Profile dla poszczególnych aplikacji, około dziewięćdziesięciu w zestawie.** Photoshop,
  Visual Studio Code, Excel, Elden Ring i pozostałe wchodzą w życie, gdy tylko program zdobędzie
  fokus, a profil wskazujący program ma pierwszeństwo przed ogólnym profilem gry. Zmień jeden, a
  tylko zmieniona część przestanie podążać za wersją dostarczoną — reszta nadal będzie się
  poprawiać z kolejnymi wydaniami.
- **Oddaje podświetlenie.** Po konfigurowalnym czasie bezczynności (domyślnie 60 s) Keylegend
  zwalnia klawiaturę, a Twój efekt z Chroma Studio znów przejmuje ster.
- **Jedenaście języków.** Angielski, niemiecki, hiszpański, francuski, włoski, niderlandzki,
  polski, portugalski, rosyjski, ukraiński i chiński uproszczony. Interfejs podąża za językiem
  wyświetlania Windows i można go zmienić w ustawieniach. Opisów klawiszy to nie dotyczy: podążają
  za Twoją klawiaturą, nie za menu.

Ponieważ znaczenie klawiszy pochodzi z **aktywnego układu klawiatury Windows**, a nie ze
sztywnej tabeli, Keylegend działa z każdym układem — polskim, niemieckim, amerykańskim,
Dvorakiem — bez zmian.

## Jak to działa

Keylegend pyta Windows, jaki znak wytworzyłby każdy klawisz w bieżącym stanie klawiatury
(`ToUnicodeEx`), wyprowadza z tego kategorię i wysyła powstałą mapę kolorów do Razer Chroma SDK
przez jego lokalny interfejs REST.

Świadomie **nie** instaluje globalnego haka klawiatury. Odczytuje wyłącznie *stan* modyfikatorów
i blokad; nigdy nie przechwytuje, nie przekazuje ani nie zapisuje naciśnięć. Zobacz
[docs/pl/architecture.md](docs/pl/architecture.md).

## Wymagania

- Windows 10 lub 11
- Razer Synapse z działającą usługą Chroma SDK
- Podłączona klawiatura Razer Chroma (patrz niżej)
- Środowisko uruchomieniowe .NET 10

## Instalacja

```powershell
winget install Eistee82.Keylegend
```

To najkrótsza droga: winget pobiera środowisko .NET jako zadeklarowaną zależność, więc nie zostaje
nic do ręcznej instalacji. W przeciwnym razie weź plik:

[**Pobierz najnowszą wersję.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Plik | Co to jest |
|---|---|
| `Keylegend-1.0.0-setup.exe` | Instaluje dla bieżącego użytkownika — bez uprawnień administratora. Wpis w menu Start i odinstalowanie, które usuwa także wpis autostartu. |
| `Keylegend-1.0.0-portable.zip` | Ten sam program do rozpakowania. Katalog `devices` musi zostać obok pliku wykonywalnego. |

Żaden z nich nie jest podpisany, więc Windows uzna wydawcę za nieznanego — certyfikat kosztuje
rocznie więcej, niż ten projekt ma. Każde wydanie zawiera `SHA256SUMS.txt` do sprawdzenia pobranego
pliku, a dziennik kompilacji, który je wytworzył, jest jawny.

## Obsługiwane klawiatury

**Każda klawiatura Razer Chroma.** Nie ma listy ani pliku na model, bo Keylegend nie musi rozpoznawać
twojej klawiatury — pyta o nią. Razer Synapse opisuje podłączoną: model z nazwy, układ fizyczny jako
liczbę oraz klawisze, które sprzęt naprawdę ma. Rysunek tego modelu wykonany przez Razera dostarcza
resztę: prawdziwe wymiary klawiszy, obudowę z pokrętłem i klawiszami multimedialnymi oraz kontury
znaków nadrukowanych na nasadkach, we właściwym języku.

Jedyne, czego rysunek nie mówi, to do której komórki macierzy podświetlenia należy każdy klawisz. To
stała protokołu Chroma, identyczna w każdym modelu — dlatego również Synapse nie potrzebuje tabeli na
model. Sprawdzone wobec jedynej ręcznie skalibrowanej klawiatury: wszystkie 105 klawiszy się zgadza.

`physicalLayout` opisuje *kształt* klawiatury, nie język, w którym piszesz. O to, jaki znak daje
klawisz, pyta się Windows w trakcie działania, więc niemiecka klawiatura działa poprawnie także przy
Windows ustawionym na US lub Dvoraka.

**Wymaga Razer Synapse**, zainstalowanego i uruchomionego, z podłączoną klawiaturą. Tam klawiatura
jest opisana i tam znajduje się jej rysunek.
## Dokumentacja

| Temat | |
|---|---|
| Architektura | jak rozstrzygane jest kolorowanie i dlaczego nie ma żadnego haka klawiatury |
| Dodawanie profilu | kolorowanie dla poszczególnych aplikacji |
| Konfiguracja | ustawienia, plik ustawień, autostart |

Dostępna w jedenastu językach:

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

Angielski i niemiecki to utrzymywane oryginały; tam, gdzie tłumaczenie im przeczy, rację ma tekst
angielski. Poprawki są mile widziane, zobacz [CONTRIBUTING.md](CONTRIBUTING.md).

## Budowanie i uruchamianie

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) to cały program: okno, ikona w obszarze powiadomień,
ustawienia. Jedyny przełącznik wart poznania: `--verify` sprawdza, czy kopia niesie dostarczone
profile i wszystkie jedenaście języków, zapisuje ustalenia do podanej po nim ścieżki i odpowiada
kodem wyjścia. To właśnie skrypt wydania uruchamia na spakowanej kopii.

Ustawienia znajdują się w `%APPDATA%\Keylegend\settings.json` i są zapisywane przez aplikację.

## Współtworzenie

Zgłoszenia błędów, profile aplikacji i tłumaczenia są mile widziane — zobacz
[CONTRIBUTING.md](CONTRIBUTING.md) oraz [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licencja

[MIT](LICENSE). Wyjątkiem są dwa cudze przyciski wpłat; kodu, nagłówków, bibliotek ani grafik
producenta nie ma tu wcale — zobacz [NOTICE.md](NOTICE.md).

## Informacja o znakach towarowych

Ten projekt **nie jest powiązany z Razer Inc. ani przez nią wspierany czy sponsorowany.**

RAZER i RAZER CHROMA są znakami towarowymi lub zastrzeżonymi znakami towarowymi Razer Inc.
Używane są tu wyłącznie po to, by wskazać sprzęt i interfejs programowy, z którymi ten projekt
współpracuje — co dopuszcza użycie odnoszące. Keylegend jest niezależnym projektem utrzymywanym
przez społeczność.

To samo dotyczy każdej innej nazwy w tym repozytorium. Profile aplikacji i gier wymieniają około
dziewięćdziesięciu programów — Photoshop, Visual Studio Code, Excel, Elden Ring i inne — a
dokumentacja wymienia producentów i modele klawiatur. To znaki towarowe ich właścicieli i pojawiają
się wyłącznie po to, by powiedzieć, do jakiego programu lub jakiej klawiatury coś się odnosi.
Keylegend nie jest z żadnym z nich związany i nie zawiera ani ich kodu, ani ich materiałów. Zobacz
[NOTICE.md](NOTICE.md).