# ex02-record-with — Record immuable et expression `with`

## Objectif

Définis un record `Point(int X, int Y)`.

Lis **X** puis **Y** sur l'entrée standard, crée un record `p = new Point(X, Y)`.
Ensuite, crée `p2` comme une **copie de `p`** avec `X` incrémenté de 1 grâce à l'expression `with`.

Affiche `p2` au format `(X, Y)`.

Exemples :
- Entrée `1`, `2` → sortie `(2, 2)`
- Entrée `0`, `0` → sortie `(1, 0)`

## Livrable

- `RecordWith.cs`

## Indices

- Syntaxe du record : `record Point(int X, int Y);` — à déclarer **après** le code principal.
- Expression `with` : `var p2 = p with { X = p.X + 1 };`.
- Affichage : `System.Console.WriteLine($"({p2.X}, {p2.Y})")`.
- Le record original `p` reste **inchangé** : l'immutabilité est préservée.
