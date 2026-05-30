# ex02-voyelles — Compter les voyelles

## Objectif

Lis **un mot** sur l'entrée standard, puis affiche le **nombre de voyelles** qu'il contient. Les
voyelles sont `a`, `e`, `i`, `o`, `u`, **majuscules comprises** (`y` n'est pas comptée).

Exemples : `bonjour` → `3` · `Piscine` → `3` · `xyz` → `0` · `AEIOU` → `5`.

## Livrable

- `Voyelles.cs`

## Indices

- Parcours chaque caractère du mot avec `foreach (char c in mot)`.
- Ramène le caractère en minuscule avec `char.ToLower(c)`, puis teste s'il est dans `"aeiou"`
  (`"aeiou".Contains(...)`).
- Incrémente un compteur à chaque voyelle trouvée.
