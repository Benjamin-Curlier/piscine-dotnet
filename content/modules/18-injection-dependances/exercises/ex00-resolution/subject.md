# ex00-resolution — Résolution d'un service

## Objectif

Lis un **nom** sur une ligne. Au lieu de fabriquer toi-même le service qui salue, **enregistre-le**
dans le conteneur de DI, **construis le provider**, puis **résous** le service et appelle-le.

Le service expose une méthode `Saluer(nom)` qui renvoie `"Bonjour, {nom}!"`. Affiche le résultat.

Exemple : `Alice` → `Bonjour, Alice!`.

## Livrable

- `Resolution.cs`

## Indices

- Ajoute `using Microsoft.Extensions.DependencyInjection;` en haut.
- Déclare une interface `IGreeter` avec `string Saluer(string nom);` et une classe `Greeter` qui
  l'implémente : `public string Saluer(string nom) => $"Bonjour, {nom}!";`.
- Enregistre la correspondance puis construis le provider :
  `var services = new ServiceCollection(); services.AddSingleton<IGreeter, Greeter>(); var p = services.BuildServiceProvider();`.
- Résous avec `var g = p.GetRequiredService<IGreeter>();` puis affiche `g.Saluer(nom)`.
- Rappel grader : les interfaces et classes se déclarent **après** les instructions ; `System.Console`
  reste pleinement qualifié.
