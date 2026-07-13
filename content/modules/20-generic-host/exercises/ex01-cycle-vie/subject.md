# Cycle de vie

Un service hébergé a deux temps : **`StartAsync`** (au démarrage de l'hôte) et **`StopAsync`**
(à l'arrêt). Implémenter `IHostedService` directement permet de voir cet ordre.

## Objectif

L'entrée standard contient **une ligne** : le **nom du service**. Il est lu, enregistré dans le
conteneur DI et **injecté** dans le service (déjà câblé dans le starter).

Écris une classe `Cycle : IHostedService` enregistrée dans l'hôte :

- `StartAsync` : journalise `Démarrage {Nom}` (catégorie `Cycle`), puis demande l'arrêt
  (`_lifetime.StopApplication()`) ;
- `StopAsync` : journalise `Arrêt {Nom}`.

`LogCapture.cs` et la configuration du logging te sont fournis.

## Sortie attendue

Pour l'entrée `Indexeur` :

```
Cycle [Information] Démarrage Indexeur
Cycle [Information] Arrêt Indexeur
```

L'hôte appelle `StartAsync` au démarrage puis `StopAsync` à l'arrêt : l'ordre est garanti.

## Livrables

- `CycleVie.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
