# Rush 2 — Rapport de données

> **Un Rush est un projet de synthèse solo.** Pas de nouveau cours : tu réutilises ce que tu as
> appris sur les chaînes (`Split`, `int.Parse`) et les collections (LINQ : `GroupBy`, `Sum`,
> `OrderBy`).

## Le contexte

Tu reçois une liste d'opérations — des **dépenses** ou des **ventes** — chacune rattachée à une
**catégorie**. Ton programme doit **analyser** ces données et produire un **rapport** : combien a
été dépensé (ou vendu) **par catégorie**, et le **total général**.

## Le problème

Lis **un entier** `N` sur l'entrée standard, puis lis `N` lignes au format :

```
CATEGORIE MONTANT
```

`CATEGORIE` et `MONTANT` sont séparés par **un espace**. `MONTANT` est un **entier**.

Produis ensuite le rapport :

- une ligne `CATEGORIE: SOMME` par catégorie, **triées par ordre alphabétique** de catégorie ;
- puis une **dernière ligne** `Total: X`, où `X` est la **somme de tous les montants**.

### Format de sortie

Chaque ligne suit exactement la forme `cle: valeur` (deux-points, **un espace**, valeur), et la
ligne finale est `Total: valeur`.

### Exemple

Entrée :

```
4
alim 10
alim 5
transport 20
alim 2
```

Sortie :

```
alim: 17
transport: 20
Total: 37
```

Les trois opérations `alim` (10 + 5 + 2 = 17) sont regroupées sur une seule ligne, `transport`
donne 20, et le total général vaut 37.

## Livrable

- `Rapport.cs`

## Conseils

- **Découpe** chaque ligne avec `Split(' ')` : tu obtiens deux morceaux, la catégorie et le montant.
- **Convertis** le montant avec `int.Parse`.
- **Regroupe** par catégorie avec `GroupBy`, puis calcule la `Sum` des montants de chaque groupe.
- **Trie** les groupes par clé (la catégorie) avec `OrderBy` avant de les afficher.
- Le **total** est simplement la `Sum` de tous les montants.
- Pas besoin de `string.Join` ici : tu affiches **une ligne par catégorie** (une par groupe).

## Rendu

Comme pour un exercice : travaille dans ton workspace, puis `git add` / `commit` / `push origin main`.
La moulinette corrige le Rush comme un livrable autonome.
