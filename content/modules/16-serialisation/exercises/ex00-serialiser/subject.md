# ex00-serialiser — Sérialiser une personne

## Objectif

Lis un **nom** (une ligne) puis un **âge** (un entier sur la ligne suivante).
Construis un objet `Personne { Nom, Age }`, **sérialise**-le en JSON avec
`JsonSerializer.Serialize(...)`, puis affiche le JSON obtenu.

Exemple : `Alice` puis `30` → `{"Nom":"Alice","Age":30}`.

## Livrable

- `Serialiser.cs`

## Indices

- `using System.Text.Json;` en haut du fichier.
- Lis le nom avec `System.Console.ReadLine()`, l'âge avec `int.Parse(System.Console.ReadLine())`.
- Crée l'objet : `var p = new Personne { Nom = nom, Age = age };`.
- Sérialise : `var json = JsonSerializer.Serialize(p);`.
- `System.Text.Json` sérialise les propriétés **dans leur ordre de déclaration** et en
  **PascalCase** : déclare `Nom` avant `Age` pour obtenir `{"Nom":...,"Age":...}`.
- La classe `Personne` se place **en bas** du fichier (après les instructions).
