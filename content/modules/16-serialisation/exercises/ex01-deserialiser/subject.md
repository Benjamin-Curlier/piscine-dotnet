# ex01-deserialiser — Désérialiser une personne

## Objectif

Lis une **ligne** contenant un objet JSON, **désérialise**-la en `Personne` avec
`JsonSerializer.Deserialize<Personne>(...)`, puis affiche `Nom: X, Age: Y`.

Exemple : `{"Nom":"Alice","Age":30}` → `Nom: Alice, Age: 30`.

## Livrable

- `Deserialiser.cs`

## Indices

- `using System.Text.Json;` en haut du fichier.
- Lis la ligne : `var ligne = System.Console.ReadLine();`.
- Désérialise : `var p = JsonSerializer.Deserialize<Personne>(ligne);`.
- Les noms du JSON (`Nom`, `Age`) doivent correspondre **exactement** aux noms des propriétés.
- Affiche : `System.Console.WriteLine($"Nom: {p!.Nom}, Age: {p.Age}");`. L'opérateur `!` indique
  au compilateur que `p` n'est pas `null` (évite un avertissement).
- La classe `Personne` se place **en bas** du fichier.
