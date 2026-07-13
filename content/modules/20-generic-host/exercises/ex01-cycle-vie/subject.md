# Cycle de vie

Un service hébergé a deux temps : **`StartAsync`** (au démarrage de l'hôte) et **`StopAsync`**
(à l'arrêt). Implémenter `IHostedService` directement permet de voir cet ordre.

## Objectif

Écris une classe `Cycle : IHostedService` enregistrée dans l'hôte :

- `StartAsync` : journalise `Démarrage` (catégorie `Cycle`), puis demande l'arrêt
  (`_lifetime.StopApplication()`) ;
- `StopAsync` : journalise `Arrêt`.

`LogCapture.cs` et la configuration du logging te sont fournis.

## Sortie attendue

```
Cycle [Information] Démarrage
Cycle [Information] Arrêt
```

L'hôte appelle `StartAsync` au démarrage puis `StopAsync` à l'arrêt : l'ordre est garanti.

## Livrables

- `CycleVie.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
