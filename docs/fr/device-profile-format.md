# Format de profil de périphérique

Un profil de périphérique décrit un modèle de clavier dans une disposition physique. C'est un
fichier unique dans un dossier de `devices/`, nommé `<fabricant>-<modèle>-<disposition>` :

```
devices/razer-deathstalker-v2-de/
└── device.json     géométrie et correspondance des LED
```

`devices/device-profile.schema.json` décrit la même chose sous forme lisible par machine. Le
nommer dans une ligne `$schema`, comme le font les profils fournis, donne à la plupart des
éditeurs la complétion et les erreurs en ligne pendant que vous tapez.

## device.json

```jsonc
{
  "$schema": "../device-profile.schema.json",
  "formatVersion": 1,
  "name": "Razer DeathStalker V2",
  "vendor": "Razer",
  "model": "DeathStalker V2",
  "physicalLayout": "ISO-DE",
  "canvas":  { "width": 439.5, "height": 135.5 },
  "matrix":  { "rows": 6, "columns": 22 },
  "verified": true,
  "keys": [
    { "id": "Keyboard_Escape", "x": 6, "y": 6, "width": 19, "height": 19,
      "row": 0, "column": 1, "label": "esc" }
  ]
}
```

| Champ | Signification |
|---|---|
| `formatVersion` | Révision du format. Actuellement `1`. Une compilation refuse un profil numéroté plus haut qu'elle ne comprend. |
| `name` | Ce qu'affiche l'interface. |
| `vendor`, `model` | Qui le fabrique et quel modèle. `"Generic"` pour un profil décrivant une disposition plutôt qu'un produit. |
| `physicalLayout` | `ANSI-US`, `ISO-DE`, `JIS-JP`, `ABNT2-BR` … — l'*agencement* physique des touches, pas la disposition logicielle. |
| `canvas` | Le système de coordonnées auquel se rapportent toutes les positions. Seuls les rapports comptent ; les profils fournis raisonnent en millimètres. |
| `matrix` | Taille de la matrice LED du fabricant. Les claviers Razer font 6 × 22, quelle que soit leur taille. |
| `verified` | `true` une fois que quelqu'un a confirmé la correspondance sur du matériel réel. |
| `note` | Texte libre facultatif pour la personne qui ouvrira le fichier ensuite. |
| `image` | Facultatif, et actuellement inutilisé — voir [Images](#images) plus bas. |
| `keys[]` | Une entrée par touche. |

### Disposition physique, pas disposition logicielle

`physicalLayout` décide de la *forme* du clavier : si la touche Entrée est haute et en L, s'il y a
une touche supplémentaire à gauche du `Z`, si la rangée du bas porte les touches de conversion
japonaises.

Il ne dit rien des caractères que ces touches produisent. Keylegend le demande à Windows à
l'exécution, pour la disposition active. Un profil ISO-FR sert donc un clavier français que
Windows soit réglé en français, en américain, en Dvorak ou en Bépo — d'où un profil par
disposition *physique* et non un par langue.

### Entrées de touche

| Champ | Signification |
|---|---|
| `id` | Identifiant unique. Suivez la nomenclature existante : `Keyboard_A`, `Keyboard_Enter`, `Keyboard_NonUsBackslash`, `Keyboard_Num7`. |
| `x`, `y` | Position du coin supérieur gauche sur le plan. |
| `width`, `height` | Taille de la touche sur le plan. |
| `row`, `column` | Cellule dans la matrice LED du fabricant. Les deux à `null` tant que c'est inconnu — état valide, et c'est à cela que sert le calibrage. |
| `scanCode` | Remplace le code de balayage standard. Nécessaire seulement là où la disposition physique contredit la nomenclature américaine. |
| `parts` | Rectangles supplémentaires appartenant à la même touche, pour les touches non rectangulaires. |
| `label` | Ce qui est imprimé sur la touche, pour celles qui n'écrivent rien. |
| `labelSecondary` | Une seconde ligne imprimée, sous la première. |

### Les légendes appartiennent au clavier

`label` est ce qui est *imprimé sur la touche*, pas une traduction de ce qu'elle fait. Un clavier
allemand dit `strg`, un français `ctrl`, un italien `bloc maiusc` — et chacun le dit quelle que
soit la langue des menus de Keylegend. Changer la langue de l'interface ne change jamais les
légendes.

Les touches qui produisent un caractère ne portent aucun `label`. Leur légende vient de la
disposition Windows active, et suit donc d'elle-même Maj, Verr Maj et Alt Gr.

### Touches à plus d'un rectangle

La touche Entrée ISO est le cas type : une touche couvrant deux rangées.

```jsonc
{
  "id": "Keyboard_Enter",
  "x": 267.25, "y": 72.5, "width": 23.75, "height": 19,
  "row": 3, "column": 14,
  "scanCode": 28,
  "parts": [ { "x": 262.5, "y": 53.5, "width": 28.5, "height": 19 } ],
  "label": "enter"
}
```

Le rectangle principal porte la cellule ; `parts` ajoute le reste de la forme. Le `scanCode`
explicite est là parce que la moitié haute occupe la position qu'ANSI réserve au backslash : sans
lui, le haut de la touche Entrée serait coloré comme s'il écrivait `\`.

### Codes de balayage des touches propres à une seule disposition

La table standard de `Keylegend.Core` couvre ce qu'a un clavier américain. Les touches qui
n'existent qu'ailleurs indiquent leur code dans le profil, pour qu'aucun C# n'ait à changer :

| Identifiant | Touche | `scanCode` |
|---|---|---|
| `Keyboard_JpYen` | `¥`, à gauche du Retour arrière sur JIS | `0x7D` |
| `Keyboard_JpRo` | `ろ`, à droite du Maj droit sur JIS | `0x73` |
| `Keyboard_JpMuhenkan` | `無変換`, à gauche de la barre d'espace | `0x7B` |
| `Keyboard_JpHenkan` | `変換`, à droite de la barre d'espace | `0x79` |
| `Keyboard_JpKana` | `かな` | `0x70` |
| `Keyboard_AbntC1` | la touche `/?` à droite du Maj droit sur ABNT-2 | `0x73` |

## Règles imposées par le validateur

Elles sont vérifiées en intégration continue, un profil qui les enfreint ne peut donc pas être
fusionné :

- Les identifiants de touche sont uniques
- Deux touches ne revendiquent pas la même cellule de matrice
- Deux touches ne se chevauchent pas sur le plan
- `row` et `column` sont soit tous deux renseignés, soit tous deux `null`
- Les cellules sont dans la matrice déclarée
- Les touches sont dans le plan
- Chaque touche a une taille positive
- Une image nommée par `image` existe réellement

## Nomenclature et la différence ISO/ANSI

Les identifiants suivent la disposition américaine, parce que c'est ce que fait la matrice du
fabricant. Sur un clavier allemand, le `Z` physique se trouve donc sur `Keyboard_Y` et
inversement. Cela ne concerne que le nom : ni la position ni le comportement n'en dépendent, car
le caractère réel est demandé à Windows à l'exécution.

Deux identifiants n'existent que sur les claviers ISO :

| Identifiant | Touche | Cellule Razer |
|---|---|---|
| `Keyboard_NonUsBackslash` | la touche supplémentaire à gauche du `Y`/`Z` (`<`, `>`, `\|`) | `RZKEY_EUR_2`, rangée 4 colonne 2 |
| `Keyboard_NonUsTilde` | la touche voisine de l'Entrée dans la rangée de repos (`#`, `'`) | `RZKEY_EUR_1`, rangée 3 colonne 13 |

Sur les claviers ISO, l'Entrée haute couvre deux positions de matrice : la moitié haute là où ANSI
a le backslash (rangée 2, colonne 14), la moitié basse sur `Keyboard_Enter` (rangée 3,
colonne 14).

**Que les deux s'allument réellement dépend du modèle.** La table du fabricant décrit ce que la
matrice peut *adresser*, pas ce qu'un clavier donné a *équipé*. Sur la DeathStalker V2, le
calibrage a montré que la cellule haute ne pilote aucune LED — toute la touche Entrée est éclairée
par celle du bas, et c'est pourquoi le profil fourni modélise l'Entrée comme une touche à deux
rectangles plutôt que comme deux touches.

C'est exactement le genre de chose qu'aucune documentation ne permet de déduire, et la raison pour
laquelle un profil ne devrait pas être marqué `verified` avant que quelqu'un l'ait parcouru sur du
matériel.

## Images

`image` est facultatif et actuellement inutilisé : l'aperçu à l'écran est dessiné à partir de la
géométrie ci-dessus. Le dessiner garde l'aperçu net à toute taille de fenêtre et rend impossible
que l'image et le profil se contredisent.

Si vous en joignez tout de même une, ce doit être une image que **vous** avez prise ou réalisée.
Tout ce dépôt paraît sous la licence MIT, qui accorde à chacun le droit de modifier et de
redistribuer ce qu'il contient — un droit que personne ne peut accorder sur la photographie
produit d'un fabricant de claviers. Voir [NOTICE.md](../../NOTICE.md).

## Voir aussi

- [Ajouter ou corriger un clavier](adding-a-keyboard.md) — la marche à suivre concrète
