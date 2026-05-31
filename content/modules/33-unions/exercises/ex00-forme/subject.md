# ex00-forme — Union de formes & aire

## Objectif

Une **discriminated union** (type somme) représente une valeur qui est **exactement l'une** d'un
ensemble **fermé** de variantes. En C#, on l'exprime avec une hiérarchie **scellée** : un
`abstract record` de base et des `sealed record` pour chaque cas.

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
- `sealed` ferme la hiérarchie : aucune autre forme ne peut exister, ce qui rend le `switch`
  réellement exhaustif.
