# ex02-durees-vie — Singleton vs Transient

## Objectif

Ce programme ne lit rien : il **démontre** la différence entre les durées de vie d'un service.

Tu as deux compteurs identiques (un champ `int`, une méthode `Incrementer()` qui pré-incrémente
et renvoie la valeur). Le `Main` et les deux compteurs te sont **fournis** : ils résolvent chaque
service deux fois et affichent le résultat. À toi de compléter **`Conteneur.Construire()`** en
enregistrant les deux services avec la **bonne durée de vie**.

- Enregistre `CompteurSingleton` en **singleton** : c'est la **même instance** à chaque résolution,
  donc `Incrementer()` renvoie `1` puis `2`.
- Enregistre `CompteurTransient` en **transient** : c'est une **instance neuve** à chaque
  résolution, donc `Incrementer()` renvoie `1` puis `1`.

Sortie attendue (4 lignes) :

```
Singleton: 1
Singleton: 2
Transient: 1
Transient: 1
```

## Livrable

- `DureesVie.cs`

## Indices

- `using Microsoft.Extensions.DependencyInjection;` est déjà en haut.
- Dans `Conteneur.Construire()` : `services.AddSingleton<CompteurSingleton>();` et
  `services.AddTransient<CompteurTransient>();` avant de renvoyer le fournisseur.
- La correction ne se contente pas de comparer la sortie : elle résout chaque service **deux fois**
  et vérifie que le singleton donne la **même instance** et le transient **deux instances distinctes**.
