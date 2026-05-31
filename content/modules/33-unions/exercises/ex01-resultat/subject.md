# ex01-resultat — Succès ou erreur

## Objectif

Plutôt que de signaler un échec par une exception ou un `null`, on peut le rendre **explicite dans
le type** : un `Resultat` est *soit* un `Succes` (avec une valeur), *soit* une `Erreur` (avec un
message). L'appelant est obligé de traiter les deux cas.

Lis `a/b`. Si `b ≠ 0`, produis `Succes(a/b)` ; sinon `Erreur("division par zero")`. Affiche
`OK <valeur>` ou `ERR <message>`.

Exemples : `10/2` → `OK 5` ; `9/0` → `ERR division par zero`.

## Livrable

- `Resultat.cs`

## Contraintes

- Union `Succes | Erreur` via hiérarchie scellée.
- Aucune exception pour gérer la division par zéro : c'est une **variante** du résultat.

## Indices

- `sealed record Succes(int Valeur) : Resultat;` et `sealed record Erreur(string Message) : Resultat;`.
- Découpe la ligne sur `/`. Choisis la variante, puis formate via un `switch`.
- C'est l'idée des types `Result`/`Either` de F#, Rust, etc. : rendre l'échec impossible à ignorer.
