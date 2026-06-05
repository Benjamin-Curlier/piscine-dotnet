# Une unité de travail

L'intérêt de l'hôte : un service hébergé reçoit ses dépendances par **injection** (module 18) et
son **logger** (module 19), puis fait son travail.

## Objectif

Lis un entier `n` sur l'entrée **avant** de construire l'hôte, enregistre-le par DI
(`AddSingleton(new Parametres(n))`). Dans un `BackgroundService` `Travailleur` :

1. récupère `Parametres` (injecté) ;
2. calcule la somme `1 + 2 + … + n` ;
3. journalise `Somme 1..{N} = {Somme}` (log structuré) ;
4. arrête l'hôte.

Le starter fournit déjà l'ossature (lecture, enregistrement, configuration du logging).

## Exemple

Entrée :

```
5
```

Sortie :

```
Travailleur [Information] Somme 1..5 = 15
```

## Livrables

- `Travail.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
