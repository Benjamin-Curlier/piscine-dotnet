# ex00-manhattan — Distance de Manhattan

## Objectif

Lis **deux points**, chacun donné par ses coordonnées `x y` sur sa propre ligne. Affiche leur
**distance de Manhattan** : le nombre de déplacements horizontaux/verticaux pour aller de l'un à
l'autre, sans diagonale.

Distance de Manhattan = `|x1 - x2| + |y1 - y2|`.

Exemple : `2 3` puis `5 7` → `7` (soit `|2-5| + |3-7|` = `3 + 4`).

## Livrable

- `Manhattan.cs`

## Contraintes

- Lis exactement deux lignes ; affiche un seul entier suivi d'un saut de ligne.

## Indices

- `System.Math.Abs(x)` donne la valeur absolue.
- Pour découper une ligne : `ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)`.
- Cette distance est l'**heuristique** que tu réutiliseras dans A* (ex03) : elle ne surestime
  jamais le vrai coût, ce qui la rend *admissible*.
