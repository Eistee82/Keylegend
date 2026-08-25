# Keylegend

**Éclairage de clavier interactif pour Razer Chroma — vos touches s'allument selon ce qu'elles font réellement.**

[English](README.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) ·
[Polski](README.pl.md) · [Português](README.pt.md) · [Русский](README.ru.md) · [Українська](README.uk.md) · [简体中文](README.zh-cn.md)

> **Version 1.0.0.** L'éclairage, l'interface, la détection des jeux et les profils d'application
> fonctionnent. [Téléchargez l'installateur ou la version portable](https://github.com/Eistee82/Keylegend/releases/latest),
> ou compilez depuis les sources. Voir [CHANGELOG.md](CHANGELOG.md).

![Keylegend colore les touches selon leur signification du moment et change de profil quand une autre application passe au premier plan](docs/images/keylegend.png)

---

## Ce qu'il fait

La plupart des logiciels RGB traitent le clavier comme une décoration. Keylegend le traite comme
un **afficheur**.

Chaque touche est colorée selon ce qu'elle signifie *à cet instant* — et cette couleur change dès
que sa signification change :

- **Les verrouillages d'un coup d'œil.** Verr Num, Verr Maj et Arrêt Défil montrent leur état sur
  la touche elle-même.
- **Une couleur par classe de caractère.** Chiffres, minuscules, majuscules, symboles et touches
  de commande ont chacun leur couleur.
- **Maintenez un modificateur, voyez sa couche.** Appuyez sur `Alt Gr` et seules les touches
  portant réellement un caractère Alt Gr restent allumées. Appuyez sur `Windows` et les
  raccourcis Windows s'allument, groupés par fonction. Idem pour `Alt`, `Ctrl` et leurs
  combinaisons.
- **Maj et Verr Maj fonctionnent d'eux-mêmes.** Comme le caractère produit par chaque touche est
  demandé en direct à Windows, les lettres passent toutes seules de la couleur « minuscule » à la
  couleur « majuscule ». Le pavé numérique se recolore en navigation quand Verr Num est éteint.
- **Les jeux ont leur propre traitement.** Détectés automatiquement — y compris en fenêtre sans
  bordure — ZQSD, les touches autour et la rangée de chiffres prennent des couleurs fixes : en
  jouant, ce qui compte est où vont les mains, pas quelle lettre une touche écrit.
- **Des profils par application, une petite centaine fournis.** Photoshop, Visual Studio Code,
  Excel, Elden Ring et les autres s'appliquent dès que le programme a le focus, et un profil qui
  nomme un programme l'emporte sur le profil de jeu général. Modifiez-en un et seule la partie
  modifiée cesse de suivre la version fournie — le reste continue de s'améliorer avec les
  versions suivantes.
- **Il rend l'éclairage.** Après une durée d'inactivité configurable (60 s par défaut), Keylegend
  libère le clavier et votre effet Chroma Studio reprend la main.
- **Onze langues.** Anglais, allemand, espagnol, français, italien, néerlandais, polonais,
  portugais, russe, ukrainien et chinois simplifié. L'interface suit la langue d'affichage de
  Windows et se change dans les paramètres. Les légendes des touches ne sont pas concernées :
  elles suivent votre clavier, pas les menus.

Parce que la signification des touches vient de la **disposition clavier active de Windows** et
non d'une table figée, Keylegend fonctionne avec n'importe quelle disposition — française,
allemande, américaine, Dvorak — sans modification.

## Comment ça marche

Keylegend demande à Windows quel caractère chaque touche produirait dans l'état clavier actuel
(`ToUnicodeEx`), en déduit une catégorie, et envoie la carte de couleurs obtenue au SDK Razer
Chroma via son interface REST locale.

Il n'installe délibérément **aucun hook clavier global**. Il ne lit que l'*état* des
modificateurs et des verrouillages ; il n'intercepte, ne transmet ni n'enregistre jamais de
frappe. Voir [docs/fr/architecture.md](docs/fr/architecture.md).

## Prérequis

- Windows 10 ou 11
- Razer Synapse avec le service Chroma SDK en fonctionnement
- Un clavier Razer Chroma, branché (voir ci-dessous)
- Le runtime .NET 10

## Installation

```powershell
winget install Eistee82.Keylegend
```

C'est le chemin le plus court : winget récupère le runtime .NET en tant que dépendance déclarée,
il n'y a donc aucun prérequis à installer soi-même. Sinon, prenez un fichier :

[**Télécharger la dernière version.**](https://github.com/Eistee82/Keylegend/releases/latest)

| Fichier | Ce que c'est |
|---|---|
| `Keylegend-1.0.0-setup.exe` | S'installe pour l'utilisateur courant — aucun droit d'administrateur. Entrée dans le menu Démarrer, et une désinstallation qui retire aussi l'entrée de démarrage automatique. |
| `Keylegend-1.0.0-portable.zip` | Le même programme, à décompresser. Gardez le dossier `devices` à côté de l'exécutable. |

Les deux ne sont pas signés : Windows annoncera donc un éditeur inconnu — un certificat coûte plus
par an que ce projet ne dispose. Chaque version fournit `SHA256SUMS.txt` pour vérifier le
téléchargement, et le journal de compilation qui l'a produit est public.

## Claviers pris en charge

**Tout clavier Razer Chroma.** Il n’y a pas de liste, ni de fichier par modèle, car Keylegend
n’a pas besoin de reconnaître votre clavier : il le demande. Razer Synapse décrit celui qui est
branché — le modèle par son nom, la disposition physique sous forme de numéro, et les touches que le
matériel possède réellement. Le dessin que Razer fait de ce modèle fournit le reste : les vraies
dimensions des touches, le boîtier avec sa molette et ses touches multimédia, et les contours des
caractères imprimés sur les capuchons, dans la bonne langue.

La seule chose que le dessin ne dit pas, c’est à quelle cellule de la matrice d’éclairage
chaque touche appartient. C’est une constante du protocole Chroma, identique sur tous les
modèles — raison pour laquelle Synapse non plus n’a pas besoin d’une table par modèle.
Vérifié contre le seul clavier calibré à la main : les 105 touches concordent.

`physicalLayout` décrit la *forme* du clavier, non la langue dans laquelle vous écrivez. Le caractère
que produit une touche est demandé à Windows au moment de l’exécution : un clavier allemand est
donc servi correctement même si Windows est réglé sur US ou Dvorak.

**Nécessite Razer Synapse**, installé et en cours d’exécution, clavier branché. C’est là
que le clavier est décrit et là que se trouve son dessin.
## Documentation

| Sujet | |
|---|---|
| Architecture | comment la coloration est décidée, et pourquoi il n'y a pas de hook clavier |
| Ajouter un profil | coloration par application |
| Configuration | paramètres, fichier de paramètres, démarrage automatique |

Disponible en onze langues :

[English](docs/en/) · [Deutsch](docs/de/) · [Español](docs/es/) · [Français](docs/fr/) ·
[Italiano](docs/it/) · [Nederlands](docs/nl/) · [Polski](docs/pl/) · [Português](docs/pt/) ·
[Русский](docs/ru/) · [Українська](docs/uk/) · [简体中文](docs/zh-cn/)

L'anglais et l'allemand sont les originaux maintenus ; là où une traduction les contredit, c'est
le texte anglais qui fait foi. Les corrections sont bienvenues, voir
[CONTRIBUTING.md](CONTRIBUTING.md).

## Compiler et lancer

```bash
git clone https://github.com/Eistee82/Keylegend.git
cd keylegend
dotnet build
dotnet test
```

`Keylegend.exe` (`src/Keylegend.App`) est tout le programme : fenêtre, icône dans la zone de
notification, paramètres. Le seul commutateur à connaître : `--verify` vérifie qu'une copie porte
les profils fournis et les onze langues, écrit ce qu'il trouve dans le chemin indiqué à la suite et
répond par son code de sortie. C'est ce que le script de publication exécute contre une copie
empaquetée.

Les paramètres résident dans `%APPDATA%\Keylegend\settings.json` et sont écrits par
l'application.

## Contribuer

Les rapports de bogue, les profils d'application et les traductions sont tous bienvenus — voir
[CONTRIBUTING.md](CONTRIBUTING.md) et [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licence

[MIT](LICENSE). Deux boutons de don tiers font exception, et aucun code, en-tête, bibliothèque ni
visuel de fabricant ne figure ici — voir [NOTICE.md](NOTICE.md).

## Mention de marques

Ce projet **n'est ni affilié à Razer Inc., ni approuvé ou parrainé par elle.**

RAZER et RAZER CHROMA sont des marques, déposées ou non, de Razer Inc. Elles sont employées ici
uniquement pour désigner le matériel et l'interface logicielle avec lesquels ce projet
fonctionne, comme le permet l'usage référentiel. Keylegend est un projet indépendant, maintenu
par la communauté.

Il en va de même pour tous les autres noms présents dans ce dépôt. Les profils d'application et de
jeu nomment une petite centaine de programmes — Photoshop, Visual Studio Code, Excel, Elden Ring et
d'autres — et la documentation nomme des fabricants et des modèles de clavier. Ce sont les marques
de leurs détenteurs respectifs et elles n'apparaissent que pour dire à quel programme ou à quel
clavier une chose se rapporte. Keylegend n'est associé à aucun d'eux et ne contient ni leur code ni
leurs visuels. Voir [NOTICE.md](NOTICE.md).