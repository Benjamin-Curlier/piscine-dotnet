# ex01-extraire-methode — Duplication → méthode

## Objectif

Le code fourni calcule **deux fois** la même chose (une valeur absolue, écrite à la main) avec un
bloc copié-collé. Supprime cette **duplication** en extrayant une méthode, sans changer le résultat.

Le programme lit quatre entiers `a b c d` et affiche `|a-b| + |c-d|`.

Exemple : `2 5 10 3` → `10` (`3 + 7`).

## Livrable

- `Distances.cs`

## Contraintes

- Comportement identique (tests `io` = filet de régression).
- Une seule logique de distance, factorisée dans une méthode appelée deux fois.

## Indices

- `static int Distance(int x, int y) => System.Math.Abs(x - y);` placée en bas du fichier.
- La duplication est le « smell » le plus courant : dès qu'un bloc se répète, demande-toi s'il ne
  mérite pas un nom (une méthode).
