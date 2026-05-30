# ex00-tri-liste — Trier une liste

## Objectif

Lis **une ligne** d'entiers séparés par des espaces, puis affiche-les **triés par ordre croissant**,
séparés par des espaces.

Exemples : `3 1 2` → `1 2 3` · `10 -3 7 -3` → `-3 -3 7 10` · `5` → `5`.

## Livrable

- `TriListe.cs`

## Indices

- Découpe la ligne (`Split`), remplis une `List<int>` en convertissant chaque morceau.
- `liste.Sort()` trie **sur place** par ordre croissant.
- `string.Join(' ', liste)` recompose la sortie séparée par des espaces.
