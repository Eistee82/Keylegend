# Configuration

Les paramètres résident dans `%APPDATA%\Keylegend\` et se modifient depuis l'interface. Une
configuration par défaut complète est écrite au premier démarrage.

## Couleurs

Une couleur par catégorie :

| Catégorie | S'applique à |
|---|---|
| Chiffre | `1`, `7`, et le pavé numérique quand Verr Num est actif |
| Minuscule | `a`, `é` |
| Majuscule | `A`, `É` |
| Symbole | `+`, `#`, `€`, `\|`, et les opérateurs du pavé numérique |
| Touche de commande | Échap, Tab, Entrée, Retour arrière, modificateurs, flèches, bloc de navigation, et le pavé numérique quand Verr Num est éteint |
| Touche de fonction | F1 à F12 |
| Touche morte | `^`, `´`, `` ` `` — touches qui demandent une seconde frappe pour produire un caractère |
| Non affectée | touches sans signification dans le contexte actuel ; éteintes par défaut. La touche centrale du pavé numérique avec Verr Num éteint en est l'exemple le plus net |

Les touches de verrouillage ont deux couleurs chacune — une pour l'état actif, une pour l'état
inactif.

## Jeux de raccourcis

Un jeu de raccourcis associe des touches à des **groupes de fonctions** et se choisit selon les
modificateurs maintenus. Jeux fournis : `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`,
`Ctrl+Shift`, `Ctrl+Alt`.

Chaque groupe a sa couleur, si bien que les commandes apparentées se lisent comme un bloc — par
exemple l'édition (`X`/`C`/`V`/`Z`/`Y`/`A`) dans une couleur et les opérations sur les fichiers
(`N`/`O`/`S`/`P`/`W`) dans une autre.

Les raccourcis Windows sont fixés à l'échelle du système et donc toujours exacts. Les raccourcis
Ctrl varient d'un programme à l'autre ; le jeu fourni couvre les conventions Windows courantes.

## Profils d'application

Un profil décrit ce que le clavier doit montrer pendant qu'un programme donné est au premier plan.
Une petite centaine accompagne l'application — des programmes comme Photoshop, Visual Studio Code
ou Excel, et des jeux comme Elden Ring ou Counter-Strike 2. Ils s'appliquent d'eux-mêmes : dès que
la fenêtre correspondante a le focus, le profil s'applique, et quand le focus passe ailleurs, les
jeux par défaut reviennent. Là où aucun profil ne correspond, rien ne change.

La reconnaissance se fait par nom d'exécutable. Quand plusieurs profils correspondent, celui qui
nomme le programme l'emporte — un jeu ayant son propre profil le garde donc même si la détection
de jeux se déclenche aussi. La priorité ne tranche que les égalités restantes.

Un profil se superpose à l'ensemble général, entrée par entrée. Photoshop dit ce que `Ctrl+J`
signifie là ; `Ctrl+C` copie toujours, car un profil qui nomme la couche Ctrl ne prétend pas que
Ctrl ne signifie rien d'autre. Et `Win+E` ouvre toujours l'Explorateur, parce que Windows attribue
cette combinaison à l'échelle du système et qu'elle tient quel que soit ce qui est devant.

### Ce que contient un profil

| Section | Contenu |
|---|---|
| Correspondance | À quels programmes le profil s'applique : noms d'exécutables, s'il couvre les jeux détectés en général, et la priorité |
| Mises en évidence | Touches fixées à une couleur quel que soit le caractère qu'elles produisent — ZQSD dans un jeu, les touches d'outil d'un éditeur d'images |
| Raccourcis | Remplacements de couches de modificateurs individuelles : quelle touche porte quelle commande sous `Ctrl`, colorée par groupe de fonctions |

Mises en évidence et raccourcis portent aussi une étiquette disant ce que fait la commande —
« Dupliquer le calque », « Sauter ». Rien de cela n'est visible sur le clavier ; les LED ne
montrent que la couleur. L'étiquette apparaît dans l'aperçu à l'intérieur de l'application, et à
quatre-vingt-dix profils c'est le seul moyen de vérifier qu'une entrée est juste.

### Modifier et réinitialiser

Les trois sections se remplacent séparément. Modifiez les mises en évidence d'un profil fourni et
elles sont vôtres à partir de là : elles sont figées et ne suivent plus la version fournie. La
correspondance et les raccourcis, eux, continuent de la suivre et bénéficient des améliorations
qu'apporte une nouvelle version.

Seule la section modifiée est enregistrée, sous l'identifiant du profil — jamais une copie du
profil entier. C'est précisément pour cela que la réinitialisation existe, et pour cela qu'une
mise à jour peut encore améliorer un profil que vous avez partiellement modifié.

La réinitialisation se fait donc aussi par section : rendre les raccourcis tout en gardant vos
propres mises en évidence est possible. Réinitialiser le profil entier reprend toutes les
sections, ainsi qu'un nom modifié et un état masqué.

Les profils fournis peuvent être **masqués mais pas supprimés**. Ils vivent à l'intérieur du
fichier programme ; en supprimer un ne durerait que jusqu'au démarrage suivant. Un profil masqué
est ignoré lors du choix d'un profil, mais reste dans la liste et peut être réaffiché.

### Vos propres profils

Un profil que vous créez vous-même est enregistré en entier dans `settings.json`, parce qu'il n'y
a rien à quoi le comparer. Il ne peut donc pas être réinitialisé, seulement supprimé. Pour le
reste il se comporte comme un profil fourni : les mêmes trois sections, la même règle de choix.

Si un profil devrait s'appliquer à tout le monde et pas seulement à vous, sa place est dans le
projet, sous forme de fichier — voir [Ajouter un profil](adding-a-profile.md).

### Format du fichier de paramètres

`settings.json` porte le `formatVersion` 3. Les fichiers plus anciens sont migrés au chargement.

Un fichier de version 1 ne connaît ni les identifiants ni la provenance d'un profil, et ne peut donc
pas dire lesquelles de ses entrées sont les entrées fournies. Toutes deviennent des profils
utilisateur. Rien n'est perdu, mais les profils fournis apparaissent à côté, il peut donc y avoir au
début deux entrées pour un même programme ; celle en trop peut être supprimée ou masquée.

Un fichier de version 2 énumère toutes les couleurs, y compris celles que personne n'a touchées, et
fige ainsi la palette : une couleur livrée améliorée n'atteint personne ayant déjà lancé le
programme. Une couleur égale à la palette de cette version est donc lue comme valeur par défaut et
abandonnée à la migration ; tout le reste est votre choix et est conservé.
## Comportement

| Réglage | Signification |
|---|---|
| Rendre l'éclairage en cas d'inactivité | S'il est rendu, tout simplement. Désactivé, Keylegend garde le clavier jusqu'à ce que vous mettiez en pause ou fermiez — et le prend au démarrage au lieu d'attendre une frappe. |
| Durée d'inactivité | Secondes sans activité clavier avant la restitution. 60 par défaut — le reprendre coûte une à deux secondes, une durée courte en fait donc une interruption permanente. La valeur est conservée pendant que la restitution est désactivée. |
| Luminosité | Facteur global de 0 à 100 %, appliqué à chaque couleur au moment de composer l'image. |
| Utiliser les profils d'application | Si les profils sont consultés du tout. Désactivé, les jeux par défaut s'appliquent partout, quoi qu'il y ait devant. |
| Démarrer avec Windows | Inscrit l'application dans la clé `Run`, avec l'option `--minimized`. Démarré ainsi, Keylegend apparaît dans la zone de notification : pas de fenêtre, pas de bulle. Démarré à la main, il montre toujours sa fenêtre. Une entrée écrite par une version antérieure est mise à jour au démarrage suivant. |

## Langue

L'interface suit la langue d'affichage de Windows et existe en onze langues : anglais, allemand,
espagnol, français, italien, néerlandais, polonais, portugais, russe, ukrainien et chinois
simplifié. **Paramètres → Langue** permet de passer outre ; le changement prend effet
immédiatement, sans redémarrage.

Chaque langue se nomme elle-même dans cette liste plutôt que d'être traduite. La traduire
voudrait dire que chacune des onze porte dix noms pour les autres, et quelqu'un dont l'interface
s'est ouverte dans une langue qu'il ne sait pas lire devrait chercher la sienne dans une langue
qu'il ne sait pas lire non plus.

Le choix est enregistré dans `settings.json` sous `language`, comme `Automatic`, `English`,
`German`, `Spanish`, `French`, `Italian`, `Dutch`, `Polish`, `Portuguese`, `Russian`, `Ukrainian`
ou `ChineseSimplified`. Une valeur inconnue retombe sur `Automatic` plutôt que de refuser de
démarrer, ce qu'un fichier modifié à la main veut de toute façon le plus probablement.

Ce qui est traduit, ce sont les menus et les explications. Deux choses ne le sont **pas**, toutes
deux délibérément :

- **Les légendes des touches** sur le clavier représenté. Elles viennent du dessin de Razer et doivent correspondre au clavier devant vous, pas à la langue des menus — un clavier ISO
  allemand affiche `strg` et `entf`, que l'interface tourne en anglais ou non.
- **Les noms des modificateurs** (Shift, Ctrl, Alt, Alt Gr, Verr Num …). Ces mêmes noms sont
  produits par la mécanique des raccourcis pour les listes de couches, qui se situe hors de la
  traduction ; une demi-traduction se lirait plus mal qu'aucune.

Tout ce qui n'a pas de traduction retombe sur l'anglais, si bien qu'un fichier de langue inachevé
coûte les lignes qui lui manquent et non l'interface entière.

## Si l'éclairage ne fonctionne pas

Le dialogue avec le service Chroma peut échouer : le service est arrêté, Synapse a été fermé, un
autre programme détient la session. Keylegend continue d'essayer, avec une pause croissante entre
les tentatives, et dit pendant ce temps ce qui ne va pas :

- la ligne d'état en bas de la fenêtre porte la raison, en ambre plutôt que dans le gris habituel
- la zone de notification le dit dans son infobulle, pour qu'une fenêtre fermée ne le cache pas
- une bulle l'annonce, une fois par panne et non une fois par tentative

Les trois disparaissent dès qu'une image passe à nouveau. Si rien n'apparaît et que le clavier ne
s'allume toujours pas, le programme ne tourne pas : cherchez son icône dans la zone de notification.

## Si les mauvaises touches s'allument

Le clavier dans la fenêtre est le clavier sur le bureau : les deux sont remplis par le même code, la
fenêtre montre donc à quoi le matériel devrait ressembler. La vérification consiste à tenir les deux
côte à côte.

À quelle cellule de la matrice d'éclairage appartient une touche est la seule chose que ni Synapse ni
le dessin n'indiquent : cela vient de la table du protocole Chroma lui-même. Si une touche s'allume
sur le matériel alors qu'une autre est allumée dans la fenêtre, cette table est fausse pour votre
modèle. Un ticket disant quel clavier et quelle touche vaut la peine.
