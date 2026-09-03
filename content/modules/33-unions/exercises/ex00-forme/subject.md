# ex00-forme — Union de formes & aire

## Objectif

Une **discriminated union** (type somme) représente une valeur qui est **exactement l'une** des
variantes prévues. En C# 14, on l'approche par convention avec un `abstract record` de base et des
`sealed record` pour chaque cas connu.

Lis une forme : `cercle R`, `rectangle L H` ou `carre C`. Construis la bonne variante et affiche
son **aire** (entière). Pour le cercle, prends π = `3` (aire = `3·R·R`).

Exemples : `cercle 5` → `75` ; `rectangle 4 6` → `24` ; `carre 3` → `9`.

## Livrable

- `Forme.cs`

## Contraintes

- `Forme` est un `abstract record` ; chaque variante est un `sealed record`.
- Calcule l'aire par **pattern matching** sur le type (`forme switch { Cercle c => ..., ... }`).

## Indices

- `abstract record Forme;` puis `sealed record Cercle(int Rayon) : Forme;` etc.
- Le `record` te donne gratuitement le constructeur positionnel et la déconstruction.
- `sealed` empêche de dériver les variantes, mais ne ferme pas la base `Forme` : termine donc le
  `switch` par un cas de repli qui lève une exception pour toute variante inattendue.
