# ex02-durees-vie — Singleton vs Transient

## Objectif

Lis un entier `N` sur l'entrée standard (le **nombre de résolutions** à effectuer). Le programme
**démontre** la différence entre les durées de vie d'un service.

Tu as deux compteurs identiques (un champ `int`, une méthode `Incrementer()` qui pré-incrémente
et renvoie la valeur). Enregistre l'un en **singleton**, l'autre en **transient**.

- Résous le **singleton N fois** : c'est la **même instance**, donc `Incrementer()` renvoie
  `1, 2, …, N` (l'état s'accumule).
- Résous le **transient N fois** : c'est une **instance neuve** à chaque fois, donc `Incrementer()`
  renvoie `1` à chaque résolution.

Affiche `2 × N` lignes. Exemple pour `N = 3` :

```
Singleton: 1
Singleton: 2
Singleton: 3
Transient: 1
Transient: 1
Transient: 1
```

## Livrable

- `DureesVie.cs`

## Indices

- Ajoute `using Microsoft.Extensions.DependencyInjection;` en haut.
- Lis `N` avec `int.Parse(System.Console.ReadLine())`.
- Chaque classe : `private int _n; public int Incrementer() => ++_n;`.
- Enregistre : `services.AddSingleton<CompteurSingleton>(); services.AddTransient<CompteurTransient>();`.
- Construis le provider, puis, dans une **boucle** de `N` tours, appelle
  `provider.GetRequiredService<...>().Incrementer()` pour chaque compteur, en préfixant l'affichage
  par `"Singleton: "` ou `"Transient: "`.
- Instructions d'abord (y compris la lecture de `N`), classes après.
