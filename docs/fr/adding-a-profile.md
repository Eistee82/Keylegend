# Ajouter un profil

Un profil d'application est **une donnée, pas du code**. Vous n'avez besoin ni de C# ni d'outils
de compilation — un éditeur de texte et une véritable connaissance du programme suffisent, et
c'est la seconde partie la plus difficile.

Si vous ne voulez un profil que pour vous, faites-le dans l'interface : il est enregistré dans
`settings.json` et n'a besoin de rien de tout ceci. Un fichier sous `profiles/` est la façon dont
un profil est livré avec l'application pour tout le monde.

## 1. Créer le fichier

```
profiles/apps/<id>.json      programmes
profiles/games/<id>.json     jeux
```

Le nom du fichier doit être égal à l'`id` qu'il contient. Minuscules, `a-z0-9-`. La compilation
intègre tous les fichiers de ces deux dossiers par joker, il n'y a donc aucun fichier projet à
modifier.

Un identifiant est définitif. Les remplacements utilisateur et les entrées de profils masqués s'y
rattachent : en renommer un dans une version ultérieure orpheline les modifications de quelqu'un.
Choisissez un nom qui sera encore juste après un changement de marque du programme —
`adobe-photoshop`, pas `photoshop-2026`.

## 2. Le remplir

Les champs, les trois sections, les groupes de fonctions, les combinaisons de modificateurs et les
conventions de couleur sont décrits dans [profiles/FORMAT.md](../../profiles/FORMAT.md). Lisez-le
d'abord ; c'est la référence et cette page ne la répète pas.

Ce qui suit est la partie qui déraille même quand le format a été lu.

## 3. Positions et caractères ne sont pas la même chose

Les identifiants de touche viennent de la table du protocole d'éclairage et nomment des **positions
américaines**. `Keyboard_Y` est la touche physique qui écrit `Y` sur un clavier américain — sur un
clavier allemand, cette touche écrit `Z`. Le format offre donc deux façons de nommer une touche, et
choisir la mauvaise produit un profil visiblement faux sur toute disposition non américaine tout en
paraissant parfait sur la machine où il a été écrit.

La question à se poser pour chaque entrée est de savoir de quoi il s'agit vraiment :

- **Où se trouve la main → position.** Une mise en évidence pour ZQSD porte sur la forme que
  prennent vos doigts, pas sur les lettres. `Keyboard_W`, `Keyboard_A`, `Keyboard_S`,
  `Keyboard_D` sont les bonnes touches partout.
- **Quelle est la commande → caractère.** `Ctrl+Z` veut dire « la touche qui écrit z ». Écrit
  comme une position, annuler et rétablir paraissent inversés sur un clavier allemand.
- **Touches qui n'écrivent rien → position à nouveau.** Échap, Tab, Entrée, Retour arrière, les
  flèches et les touches de fonction n'ont pas de caractère : `shortcuts.keys` les nomme par
  identifiant sans ambiguïté.

### Pour les mises en évidence, cela dépend de la façon dont le programme lit le clavier

QWERTZ et QWERTY ne diffèrent qu'à deux endroits, donc `Keyboard_Y` et `Keyboard_Z` sont les seuls
identifiants où cela peut mal tourner. Et cela tourne mal en silence.

L'identifiant d'une mise en évidence est toujours une **position physique**. La question est de
savoir quelle touche physique le programme désigne, et cela découle de sa façon de lire le
clavier :

| Le programme se lie | Exemples | `Z` dans sa documentation signifie |
|---|---|---|
| au **caractère** (codes de touche virtuels Windows, qui suivent la disposition) | Photoshop, Blender, GIMP, Krita — les applications en général | `Keyboard_Y` — la touche de la rangée du haut, qui écrit `Z` sur un clavier allemand |
| à la **position** (codes de balayage, comme la plupart des moteurs de jeu, pour que ZQSD ne bouge pas) | les jeux en général | `Keyboard_Z` — la touche de la rangée du bas |

Si vous ne parvenez pas à établir de quelle manière un programme donné lit le clavier, laissez de
côté les entrées `Y` et `Z`. Toutes les autres lettres sont indifférentes.

## 4. Laissez de côté ce dont vous n'êtes pas sûr

Un raccourci faux est pire qu'un raccourci manquant. Une entrée manquante laisse une touche
éteinte et ne coûte rien ; une entrée fausse fait dire au clavier quelque chose de faux, et
l'utilisateur n'a aucun moyen de savoir que c'est faux. L'étiquette rend l'affirmation explicite —
elle ne la rend pas correcte.

Donc :

- N'écrivez que ce dont vous êtes sûr qu'il s'agit du raccourci **par défaut** du programme, tel
  qu'il sort de l'installation. Votre propre installation n'est pas une source ; vous avez
  probablement changé des choses et les avez oubliées.
- Vérifiez dans la documentation du programme, ou dans le programme lui-même avec des réglages
  intacts.
- Là où les valeurs par défaut diffèrent d'une version à l'autre, suivez la version actuelle.
- N'inventez pas. Si un programme n'a pas de raccourci bien connu pour quelque chose, il n'y a pas
  d'entrée.

Douze raccourcis corrects valent mieux que trente dont quatre sont faux. Cela vaut aussi pour les
étiquettes des mises en évidence : si vous ne pouvez pas dire ce que fait une touche, c'est le
signe que l'entrée n'a pas encore sa place dans le profil.

## 5. Tester

```bash
dotnet test
```

Les tests de profils vérifient chaque fichier sous `profiles/` : l'identifiant est unique et
correspond au nom du fichier, `kind` correspond au dossier, chaque identifiant de touche existe
dans la table de matrice, les couleurs se lisent, les groupes et les combinaisons de modificateurs
sont valides et écrits sous leur forme canonique, chaque raccourci porte une étiquette, aucune
touche de lettre ne figure sous `shortcuts.keys` (sa place est sous `characters`), aucun profil
n'est vide, et deux profils ne revendiquent pas un même exécutable sans se distinguer par
`titleContains`.

Une chose n'est délibérément **pas** vérifiée : la même étiquette apparaissant deux fois sous un
même modificateur. Cela ressemblait à un moyen d'attraper les copier-coller et attrapait en fait
de vrais alias — les navigateurs ferment un onglet avec `Ctrl+W` comme avec `Ctrl+F4`. Une
vérification qui se déclenche sur des données correctes est pire qu'aucune.

Ce qu'aucun test ne peut vérifier, c'est si un raccourci est *vrai*. C'est à cela que sert la
relecture, et la raison pour laquelle chaque entrée porte une étiquette à relire.

## 6. L'essayer contre le programme

Lancez Keylegend, mettez le programme au premier plan et maintenez les modificateurs que définit
votre profil. L'aperçu montre la même chose que le clavier, un portable sans matériel Chroma
suffit donc pour cela. Comparez avec les menus du programme — une commande dont vous ne trouvez
pas l'étiquette dans le programme est la première à supprimer.

## 7. Ouvrir une pull request

Merci d'indiquer quel programme et quelle version vous avez vérifiés, et comment vous avez
contrôlé les affectations : la documentation du programme, le programme lui-même, ou les deux.
Voir [CONTRIBUTING.md](../../CONTRIBUTING.md).

Un petit profil sûr est une bonne contribution. Un grand profil à moitié deviné ne l'est pas.
