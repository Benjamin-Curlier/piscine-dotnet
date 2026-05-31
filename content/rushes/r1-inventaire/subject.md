# Rush 1 — Gestionnaire d'inventaire

> **Un Rush est un projet de synthèse solo.** Pas de nouveau cours : tu réunis ce que tu as appris
> sur les **collections** (modules 05/06) et les **classes/objets** (modules 07/08) pour construire
> un petit **interpréteur de commandes**.

## Le contexte

Tu gères le **stock d'un magasin**. Un programme reçoit une suite de commandes (ajouter du stock,
en retirer, consulter une quantité, faire le total) et y répond. À toi d'écrire le moteur qui lit
ces commandes et tient l'inventaire à jour.

## Le problème

Lis d'abord **un entier** `N` : le nombre de commandes qui suivent. Puis lis **`N` lignes**, chacune
étant **une commande** parmi les quatre suivantes :

| Commande              | Effet                                                                          |
| --------------------- | ------------------------------------------------------------------------------ |
| `ajouter NOM QTE`     | Ajoute `QTE` unités à l'article `NOM` (le crée s'il n'existe pas encore).       |
| `retirer NOM QTE`     | Retire `QTE` unités de `NOM`, **sans jamais descendre sous 0** (borné à 0).     |
| `afficher NOM`        | Imprime `NOM: QTE` (la quantité courante de `NOM`, ou `0` s'il est inconnu).     |
| `total`               | Imprime `Total: X` où `X` est la **somme** des quantités de tous les articles.  |

**Important :** le programme **n'imprime rien** pour `ajouter` et `retirer`. Il n'imprime que pour
les commandes `afficher` et `total`.

## Format d'entrée / sortie

- **Entrée** : la 1re ligne est `N`. Les `N` lignes suivantes sont les commandes, dont les champs
  sont séparés par des **espaces**.
- **Sortie** :
  - `afficher pomme` → `pomme: 7` (un espace après les deux-points) ;
  - `total` → `Total: 12`.
  - Chaque ligne affichée se termine par un retour à la ligne.

### Exemple

Entrée :

```
5
ajouter pomme 10
ajouter poire 5
retirer pomme 3
afficher pomme
total
```

Sortie :

```
pomme: 7
Total: 12
```

Déroulé : `pomme` passe à 10 puis redescend à 7 ; `poire` vaut 5 ; `afficher pomme` imprime `7` ;
`total` fait `7 + 5 = 12`.

## Livrable

- `Inventaire.cs`

## Conseils

- Stocke l'inventaire dans un **`Dictionary<string, int>`** : la clé est le nom de l'article, la
  valeur sa quantité. Pense à `using System.Collections.Generic;`.
- **Découpe** chaque ligne en morceaux avec un `Split(' ')` : le 1er mot est la commande, les
  suivants sont les arguments.
- Pense aux **cas limites** :
  - `afficher` d'un **article inconnu** doit donner `0` (et non planter) ;
  - `retirer` plus que le stock disponible doit **borner à 0**, jamais une quantité négative ;
  - `ajouter` sur un article déjà présent **cumule** les quantités.
- Pour le `total`, additionne toutes les valeurs du dictionnaire (une boucle, ou `Sum()` avec
  `using System.Linq;`).

## Rendu

Comme pour un exercice : travaille dans ton workspace, puis `git add` / `commit` /
`push origin main`. La moulinette corrige le Rush comme un livrable autonome.
