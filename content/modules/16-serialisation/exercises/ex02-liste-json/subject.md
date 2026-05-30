# ex02-liste-json — Une liste en tableau JSON

## Objectif

Lis un entier **N**, puis **N** noms (un par ligne). Range-les dans une `List<string>`,
**sérialise** la liste en tableau JSON et affiche le résultat.

Exemple : `3` puis `a`, `b`, `c` → `["a","b","c"]`.

## Livrable

- `ListeJson.cs`

## Indices

- `using System.Text.Json;` **et** `using System.Collections.Generic;` (pour `List<>`).
- Lis `N` avec `int.Parse(System.Console.ReadLine())`.
- Crée `var liste = new List<string>();` puis remplis-la dans une boucle `for` avec
  `liste.Add(System.Console.ReadLine());`.
- Sérialise : `var json = JsonSerializer.Serialize(liste);`. Une liste devient un **tableau JSON**
  entre crochets : `["a","b","c"]`.
- Affiche le JSON avec `System.Console.WriteLine(json)`.
