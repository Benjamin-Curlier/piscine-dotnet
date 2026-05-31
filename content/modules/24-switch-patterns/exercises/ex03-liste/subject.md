# ex03-liste — Premier et dernier (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis une ligne d'entiers séparés par des espaces, transforme-la en `int[]`, puis analyse
le tableau avec un **list pattern** :

- un seul élément → `un seul`
- deux éléments ou plus → `premier={f} dernier={l}` (premier et dernier élément)

Exemple : `5` → `un seul` ; `1 2 3` → `premier=1 dernier=3` ; `10 20` → `premier=10 dernier=20`.

## Livrable

- `Liste.cs`

## Indices

- Découpe et parse : `ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)` puis
  `.Select(int.Parse).ToArray()` (nécessite `using System.Linq;`).
- Un **list pattern** décrit la forme du tableau : `[var x]` (exactement un élément),
  `[var f, .., var l]` (un premier `f`, une tranche `..` au milieu, un dernier `l`).
- La **tranche `..`** absorbe zéro, un ou plusieurs éléments : `[var f, .., var l]` accepte donc
  aussi un tableau de 2 éléments.
- `t switch { [var x] => "un seul", [var f, .., var l] => $"premier={f} dernier={l}", _ => "?" }`.
