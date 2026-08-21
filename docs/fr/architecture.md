# Architecture

## L'idée centrale

Toute la logique de décision est un **calcul pur**, sans accès à Windows, au réseau ni au système
de fichiers :

```
(état du clavier, profil de périphérique, profil d'application, réglages de couleur) → couleur par touche
```

Deux conséquences en découlent, et toutes deux expliquent la forme de cette conception :

1. L'aperçu à l'écran et le vrai clavier sont remplis par **le même code**. Ce que vous voyez
   dans la fenêtre est ce qui s'allume.
2. La logique est entièrement testable sans clavier branché et sans Synapse installé.

Tout ce qui parle au monde extérieur se trouve dans de fins adaptateurs autour de ce noyau.

## Projets

| Projet | Contient | Peut dépendre de |
|---|---|---|
| `Keylegend.Core` | profils de périphérique, catégories, jeux de raccourcis, compositeur d'images, automate de session | rien de spécifique à une plateforme |
| `Keylegend.Windows` | état du clavier, résolution des caractères, fenêtre au premier plan | API Windows |
| `Keylegend.Chroma` | client REST pour le SDK Chroma, battement de cœur | réseau |
| `Keylegend.App` | interface WPF, icône de notification, stockage de la configuration | tout ce qui précède |

`Keylegend.Core` ne doit jamais référencer les autres. Si une modification semble l'exiger,
c'est l'abstraction qui n'est pas au bon endroit.

## Lire l'état du clavier

Keylegend n'installe **pas** de hook clavier global. Un tel hook est fonctionnellement un
enregistreur de frappe, se place dans la chaîne d'entrée et se fait régulièrement signaler par
les systèmes anti-triche.

À la place, l'état des touches qui nous intéressent est interrogé (`GetAsyncKeyState` pour les
modificateurs maintenus, `GetKeyState` pour les verrouillages) environ soixante fois par seconde,
et une nouvelle image n'est composée que si quelque chose a changé. Aucune frappe n'est jamais
interceptée, transmise, journalisée ni conservée.

### Modificateurs gauche et droit

Windows signale **Alt Gr comme Ctrl plus Alt droit**, et sur les dispositions allemandes
Ctrl + Alt gauche produit les mêmes caractères qu'Alt Gr. On les distingue par le côté :

- **Alt droit** → couche Alt Gr, montrant l'affectation des caractères
- **Ctrl + Alt gauche** → le jeu de raccourcis `Ctrl+Alt`

Les variantes gauche et droite doivent donc être évaluées séparément (`VK_LMENU`/`VK_RMENU`, et
ainsi de suite).

## Déterminer ce que signifie une touche

Plutôt que de livrer une table de dispositions, Keylegend demande à Windows quel caractère une
touche produirait dans l'état clavier actuel (`ToUnicodeEx`), et déduit la catégorie du caractère
obtenu.

C'est pourquoi Maj, Verr Maj et Verr Num ne demandent aucun traitement particulier : la même
touche renvoie simplement `A` au lieu de `a` et atterrit d'elle-même dans la catégorie
« majuscule ». C'est aussi pourquoi n'importe quelle disposition fonctionne sans modification.

## Profils d'application

Un profil lie des règles d'éclairage à un programme. Une petite centaine est fournie, et les
décisions qui les sous-tendent méritent d'être énoncées, car chacune fut la deuxième réponse et
non la première.

### Les profils sont des données, pas du code

La même règle que pour les périphériques : ajouter un profil, c'est ajouter un fichier JSON sous
`profiles/`, et la compilation le prend par joker. Personne n'a besoin de toucher au C# pour
apprendre un programme à Keylegend, ce qui veut dire qu'un profil peut être proposé, relu et
corrigé par quelqu'un qui ne connaît que le programme. Si prendre en charge une nouvelle
application demandait du code, le format serait mauvais.

### Intégrés à l'assembly plutôt que posés sur le disque

Les profils de périphérique sont à côté de l'exécutable ; ceux des applications non. Trois
raisons, dont chacune suffirait. Une version en fichier unique les emporte sans dossier à perdre.
Rien sur le disque ne peut être modifié par accident, et c'est précisément ce qui donne un sens à
« rétablir la version fournie » — la version fournie doit être hors d'atteinte pour valoir la
peine d'y revenir. Et un profil qui ne compile pas devient une erreur de compilation plutôt qu'un
programme silencieusement dépourvu de profils.

### Les remplacements se font par section

La modification d'un utilisateur n'est jamais enregistrée comme une copie du profil. Elle est
enregistrée comme un remplacement indexé sur l'identifiant du profil, ne contenant que les
sections touchées. Deux conséquences : la réinitialisation est possible, et une nouvelle version
peut encore améliorer un profil que quelqu'un a partiellement modifié. L'identifiant est porteur
pour cela et ne doit jamais changer une fois publié — le renommer orpheline les modifications de
quelqu'un.

La granularité a été choisie contre les deux autres possibilités évidentes :

- **Par champ** paraît plus propre et produit des états que personne n'a configurés. Recolorez
  `W`, prenez ensuite une mise à jour qui ajoute `Q`, et le résultat est un mélange que
  l'utilisateur n'a jamais construit et ne peut pas expliquer.
- **Par profil** est l'échec inverse. Renommez une chose et le profil est figé pour toujours ; il
  ne verra plus jamais une correction.

Une section est la granularité à laquelle le changement tient encore en une phrase : vous avez
modifié les mises en évidence, elles sont donc à vous désormais.

### Un profil ne remplace que les couches qu'il nomme

Les raccourcis sont indexés par combinaison de modificateurs et posés par-dessus le catalogue
général, non substitués à lui. Photoshop sait ce que `Ctrl` veut dire dans Photoshop ; il ne sait
rien de `Win+E`, que Windows attribue à l'échelle du système et qui reste vrai quoi qu'il y ait
devant. Remplacer tout le catalogue rendrait un profil responsable de faits sur lesquels il n'a
aucun avis. Un profil qui ne nomme aucune couche renvoie le catalogue général inchangé, si bien
que le cas courant n'alloue rien.

### Raccourcis et mises en évidence portent une étiquette

L'étiquette dit ce que fait la commande — « Dupliquer le calque », pas « Ctrl+J ». Le matériel ne
la montre jamais : les LED ne portent que de la couleur, l'étiquette ne coûte donc rien à
l'exécution. Elle se rembourse trois fois ailleurs. L'aperçu dans l'application peut l'afficher,
un test peut trouver des contradictions entre entrées, et à quatre-vingt-dix profils c'est le
seul moyen pour quiconque de vérifier qu'une entrée est correcte. `"j": "Modifier"` ne peut être
confronté à rien ; `"j": "Dupliquer le calque"` le peut.

### Migrer un fichier de paramètres au format 1

Le format 1 stockait les profils entiers, sans identifiant et sans trace de leur provenance.
C'est exactement ce que corrige le nouveau format : un remplacement a besoin d'un identifiant
auquel s'attacher, et la réinitialisation a besoin de savoir qu'il existe une version fournie à
laquelle revenir.

La conséquence pour la migration est qu'un ancien fichier ne peut pas dire lesquelles de ses
entrées étaient jadis fournies. Toutes deviennent donc des profils utilisateur. Cela conserve
chaque modification faite par quelqu'un, au prix de voir le profil fourni apparaître à côté de la
copie migrée jusqu'à ce que l'un des deux soit supprimé — et c'est le bon compromis, car l'autre
lecture supprimerait silencieusement du travail.

## Parler au clavier

Le SDK Chroma est adressé par son interface REST locale. Les couleurs sont des entiers encodés en
BGR ; le clavier entier s'écrit comme une matrice 6 × 22. Une session doit être maintenue en vie
par un battement de cœur.

Mesuré sur la machine de développement : créer une session prend 60 à 125 ms, la première image
après avoir repris la main sur un effet Chroma Studio en cours environ 500 ms, et chaque image
suivante autour de 2 ms.

### À quelle fréquence les images sont envoyées

Cela ressemble à un détail et n'en est pas un ; les deux réponses évidentes sont fausses, et
chacune a été essayée.

**N'envoyer qu'au changement** affame la reprise en main. Une frappe ordinaire ne change pas
l'état du clavier — seuls les modificateurs et les verrouillages le font —, si bien qu'une reprise
ne produisait qu'une seule image. Chroma jette les images tant qu'il prend encore le contrôle, et
signale un succès pour elles : cette unique image pouvait donc disparaître et laisser le clavier
figé sur l'effet précédent jusqu'à ce que l'utilisateur appuie par hasard sur un modificateur.

**Envoyer aussi vite que possible** ruine la réactivité. Les images s'accumulent dans l'interface,
et un changement d'état attend alors derrière tout ce qui a déjà été envoyé — appuyer sur Maj
mettait une seconde ou deux, visiblement, à s'afficher.

Ce qui marche, c'est d'envoyer pour trois raisons distinctes à trois cadences différentes :

| Raison | Cadence |
|---|---|
| L'état du clavier a changé | immédiatement — mesuré à 1 ms de bout en bout |
| Dans les trois secondes suivant une reprise en main | toutes les 120 ms, jusqu'à ce que la transition se stabilise |
| Sinon | toutes les 750 ms, purement comme assurance contre une image perdue |

## Gestion de la session

| État | Comportement |
|---|---|
| **Au repos** | Aucune session. Chroma Studio pilote l'éclairage. Seule la peu coûteuse interrogation d'activité tourne. |
| **Actif** | Session ouverte, battement de cœur en marche, une nouvelle image à chaque changement d'état. |
| **En pause** | Éclairage libéré jusqu'à reprise. |

Keylegend prend la main à la première frappe et libère le clavier après une durée d'inactivité
configurable, si bien que votre propre effet Chroma Studio revient. Le coût de réveil d'environ
500 ms n'est donc payé qu'après une vraie pause, jamais pendant la frappe.
