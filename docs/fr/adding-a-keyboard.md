# Ajouter ou corriger un clavier

La prise en charge d'un clavier est **une donnée, pas du code**. Vous n'avez besoin ni de C# ni
d'outils de compilation — un éditeur de texte et votre propre clavier suffisent.

La plupart de ceux qui arrivent ici n'ont rien à ajouter : un profil existe déjà pour leur
disposition. Ce qui manque à ces profils est la seule chose qui ne se génère pas : quelqu'un qui,
matériel en main, confirme que chaque touche s'allume là où le profil le prétend. **C'est le
travail décrit en [partie 2](#2-corriger-un-profil), et il prend une dizaine de minutes.**

---

## Ce qu'un profil sait, et à quel point il en est sûr

Un profil répond à deux questions distinctes, et elles ne sont pas également fiables :

| Question | D'où vient la réponse | Fiabilité |
|---|---|---|
| Où se trouve chaque touche, et quelle taille fait-elle ? | Le pas normalisé de 19,05 mm, que tout clavier suit depuis l'IBM Model M | **Certaine.** La géométrie découle de la disposition. |
| Quelle cellule de la matrice LED allume cette touche ? | La matrice publiée par le fabricant, en supposant un clavier standard | **Une supposition.** Les modèles déplacent des touches, laissent des cellules non équipées et en ajoutent. |

Cette séparation est toute la raison d'être de l'indicateur `verified`. Un profil marqué
`"verified": false` a presque certainement raison sur l'image et peut fort bien se tromper sur la
touche qui s'allume.

---

## 1. Ajouter une disposition manquante

Vérifiez d'abord qu'elle manque vraiment : `devices/` contient déjà des profils format complet
pour ANSI-US, ISO-DE, ISO-UK, ISO-FR, ISO-ES, ISO-IT, ISO-NORDIC, ISO-PT, ISO-CH, ISO-RU, ISO-PL,
JIS-JP et ABNT2-BR, plus des variantes tenkeyless, 75 %, 65 % et 60 %. Si la vôtre en fait partie,
passez à la partie 2.

### La voie générée

`tools/make-layout.py` construit un profil à partir des dimensions normalisées. Y ajouter un
clavier tient en une entrée de la liste `PROFILES`, en bas du fichier :

```python
("generic-fullsize-iso-tr", dict(
    name="Full-size keyboard (Turkish)", vendor="Generic", model="Full-size 105-key",
    physical_layout="ISO-TR", form_factor="fullsize", variant="iso", legends="en")),
```

| Argument | Ce qu'il décide |
|---|---|
| `form_factor` | `fullsize`, `tkl`, `75`, `65`, `60`, `fullsize-macro` |
| `variant` | `ansi`, `iso`, `jis` ou `abnt2` — la forme de la touche Entrée et les touches supplémentaires |
| `legends` | Quel jeu de légendes imprimées utiliser : `en`, `de`, `fr`, `es`, `it` |
| `right` | `win` ou `fn` — ce qui se trouve entre l'Alt droit et la touche menu |

Puis lancez-le :

```bash
python tools/make-layout.py --only iso-tr
```

Si les légendes de votre clavier ne figurent pas parmi les cinq jeux, ajoutez-en un : copiez
`LEGENDS_EN` dans le même fichier, traduisez les entrées, et inscrivez-le dans `LEGEND_SETS`.
Seules les touches qui n'écrivent *rien* ont besoin d'une légende — les autres sont demandées à
Windows à l'exécution, et c'est ce qui permet à un profil de servir toutes les dispositions
logicielles sur le même matériel.

### La voie manuscrite

Pour un clavier qui n'est pas une variation d'une disposition standard — orthogonal, scindé, doté
d'une rangée de touches macro que personne d'autre n'a — écrivez `device.json` directement. La
[description du format](device-profile-format.md) liste chaque champ, et
`devices/device-profile.schema.json` donne à la plupart des éditeurs la complétion et les erreurs
en ligne.

Le premier jet n'a pas besoin d'être exact. Placez les touches approximativement, laissez `row` et
`column` à `null` partout où vous doutez, et laissez le calibrage faire le reste.

---

## 2. Corriger un profil

C'est la partie qui demande le matériel, et celle qui compte vraiment.

### Regarder d'abord

Avant de toucher au clavier, examinez l'image :

```bash
python tools/preview-layout.py devices/generic-fullsize-iso-fr/device.json
```

Cela écrit `preview.svg` à côté du profil ; ouvrez-le dans n'importe quel navigateur. Comparez-le
au clavier devant vous et cherchez :

- des touches manquantes, ou des touches dessinées que votre clavier n'a pas
- une touche Entrée de la mauvaise forme — haute et en L sur ISO, large et plate sur ANSI
- une rangée du bas avec le mauvais nombre de modificateurs, ce qui varie plus que tout le reste
- des **contours rouges**, qui marquent les touches sans cellule de matrice. Celles-là ne
  s'allumeront jamais.

Corriger la géométrie relève du calcul, pas de la devinette : la grille fait une unité par touche,
et une unité est la `width` qu'ont les touches de lettres ordinaires.

### Puis calibrer

Le calibrage allume une touche à la fois et la nomme, pour que vous puissiez confirmer que la
touche qui brille en blanc est bien celle que le profil annonce. C'est le seul moyen d'en être
certain : tout le reste est déduit d'une table de fabricant.

```bash
keylegend-cli --profile devices/<votre-dossier>/device.json --calibrate
```

Il parcourt les touches associées dans le sens de la lecture :

| Touche | Effet |
|---|---|
| `Entrée` ou `→` | celle-ci est correcte, on passe à la suivante |
| `F` | la mauvaise touche s'est allumée — le noter |
| `←` | une touche en arrière |
| `A` | allumer toutes les touches associées en même temps |
| `S` | passer directement au récapitulatif |
| `Q` ou `Échap` | arrêter |

Comme les identifiants suivent la disposition américaine, l'affichage indique aussi ce que chaque
touche écrit réellement sur *votre* machine — sur un clavier français on vous parle donc de « la
touche ù » et non de `Keyboard_ApostropheAndDoubleQuote`.

Les constats sont écrits dans `calibration-findings.txt` au fil de l'eau, pas à la fin. Le
calibrage est un travail patient et une fenêtre fermée ne doit pas vous le coûter.

Une seconde image aide pendant le travail — celle-ci étiquette chaque touche avec la cellule
qu'elle revendique au lieu de sa légende :

```bash
python tools/preview-layout.py devices/<votre-dossier>/device.json --cells
```

### Appliquer ce que vous avez trouvé

`tools/apply-calibration.ps1` réécrit les constats dans le profil, en gardant une copie `.bak` :

```powershell
tools/apply-calibration.ps1 `
  -ProfilePath devices/<votre-dossier>/device.json `
  -Unlit Keyboard_Backslash,Keyboard_PauseBreak `
  -Remap "Keyboard_Enter=3,14"
```

`-Unlit` concerne les touches qui n'ont rien allumé du tout : la matrice peut adresser la cellule,
mais ce modèle-là n'y a pas de LED. Ces touches gardent leur géométrie — la touche existe, et
l'aperçu doit la dessiner — et perdent leur `row`/`column`, pour que rien ne parte dans le vide.
`-Remap` concerne les touches associées à la mauvaise cellule.

### À quoi s'attendre

Voici les endroits où un profil généré se trompe le plus souvent :

| Où | Ce qui se passe |
|---|---|
| **La touche Entrée ISO** | Elle couvre deux cellules. Sur beaucoup de claviers, seule celle du bas est équipée d'une LED, et la moitié haute est éclairée par sa voisine ou pas du tout. |
| **La rangée du bas** | Le nombre et la largeur des modificateurs diffèrent d'un modèle à l'autre. Les claviers de jeu mettent `Fn` là où les claviers de bureau ont une seconde touche Windows. |
| **Touches macro et multimédia** | Souvent sur la colonne 0 ou sur les colonnes extérieures, et souvent sur aucune cellule. |
| **Claviers compacts** | La matrice conserve ses 6 × 22 complets ; un clavier 60 % en laisse simplement la plus grande partie vide. Les cellules ne sont pas renumérotées. |
| **Les touches hautes du pavé numérique** | Plus et Entrée couvrent deux rangées mais répondent à une seule cellule — en général celle du haut. |

Une touche qui s'avère dépourvue de LED garde sa géométrie et perd sa cellule :

```jsonc
{ "id": "Keyboard_Function", "x": 234, "y": 120, "width": 24, "height": 19,
  "row": null, "column": null }
```

Elle reste dessinée, l'aperçu correspond donc au matériel ; elle ne s'allume simplement jamais.
C'est correct, ce n'est pas un défaut.

### Marquer comme vérifié

Quand chaque cellule concorde, passez `-MarkVerified` au même script, ou mettez
`"verified": true` à la main, et supprimez la `note` disant que le profil a été généré. Cet
indicateur dit à la prochaine personne ayant votre clavier qu'elle peut s'y fier.

---

## 3. Tester

```bash
dotnet test
```

Les tests des profils fournis valident chaque profil sous `devices/`, y compris le vôtre. Ils
attrapent les identifiants en double, deux touches revendiquant la même LED, des touches dessinées
l'une sur l'autre, des cellules hors de la matrice et une géométrie qui a glissé hors du plan.

## 4. Ouvrir une pull request

Indiquez quel clavier et quelle disposition physique vous avez vérifiés, et si vous avez parcouru
le calibrage. Voir [CONTRIBUTING.md](../../CONTRIBUTING.md).

Les profils en `"verified": false` sont bienvenus aussi — ils donnent une avance à la prochaine
personne ayant ce clavier. Une correction sur un profil existant vaut tout autant qu'un nouveau.

### À propos des images

Le champ `image` est facultatif et actuellement inutilisé : l'aperçu est dessiné à partir de la
géométrie, ce qui le garde net à toute taille et l'empêche de contredire le profil. Si vous en
joignez tout de même une, ce doit être une image que **vous** avez photographiée ou dessinée. Un
rendu produit par un fabricant ne peut pas être publié sous la licence MIT de ce projet, et une
pull request en contenant un se verra demander de le retirer.

## Voir aussi

- [Format de profil de périphérique](device-profile-format.md) — chaque champ, en détail
- [Architecture](architecture.md) — pourquoi le sens des touches vient de Windows et non d'une table
