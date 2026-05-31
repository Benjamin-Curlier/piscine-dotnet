# ex02-json — Valeur JSON

## Objectif

Une valeur JSON simple est *soit* un nombre, *soit* un texte, *soit* un booléen : un cas d'école de
**type somme**. Modélise-la et rends-la selon sa variante.

Lis `nombre N`, `texte MOT` ou `booleen vrai|faux`. Affiche :
- le nombre tel quel ;
- le texte **entre guillemets** ;
- le booléen en `true` / `false`.

Exemples : `nombre 42` → `42` ; `texte bonjour` → `"bonjour"` ; `booleen vrai` → `true`.

## Livrable

- `Json.cs`

## Contraintes

- Union `Nombre | Texte | Booleen` via hiérarchie scellée.
- Utilise la **déconstruction** dans le pattern matching.

## Indices

- `Nombre(var n) => n.ToString()`, `Texte(var t) => "\"" + t + "\""`, `Booleen(var b) => b ? "true" : "false"`.
- La déconstruction `Variante(var x)` extrait directement la donnée du record dans le `switch`.
