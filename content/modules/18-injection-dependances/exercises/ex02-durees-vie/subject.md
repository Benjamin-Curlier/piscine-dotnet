# ex02-durees-vie — Singleton vs Transient

## Objectif

Ce programme ne lit rien : il **démontre** la différence entre les durées de vie d'un service.

Tu as deux compteurs identiques (un champ `int`, une méthode `Incrementer()` qui pré-incrémente
et renvoie la valeur). Enregistre l'un en **singleton**, l'autre en **transient**.

- Résous le **singleton deux fois** : c'est la **même instance**, donc `Incrementer()` renvoie `1`
  puis `2`.
- Résous le **transient deux fois** : c'est une **instance neuve** à chaque fois, donc `Incrementer()`
  renvoie `1` puis `1`.

Affiche les **4 lignes** :

```
Singleton: 1
Singleton: 2
Transient: 1
Transient: 1
```

## Livrable

- `DureesVie.cs`

## Indices

- Ajoute `using Microsoft.Extensions.DependencyInjection;` en haut.
- Chaque classe : `private int _n; public int Incrementer() => ++_n;`.
- Enregistre : `services.AddSingleton<CompteurSingleton>(); services.AddTransient<CompteurTransient>();`.
- Construis le provider, puis appelle `provider.GetRequiredService<...>().Incrementer()` deux fois
  pour chaque compteur, en préfixant l'affichage par `"Singleton: "` ou `"Transient: "`.
- Pas de saisie : aucune lecture de l'entrée. Instructions d'abord, classes après.
