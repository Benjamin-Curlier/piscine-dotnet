# Pipeline producteur/consommateur (bonus)

> Exercice **bonus** (difficulté : difficile).

Un `Channel<T>` (module 21) est une file thread-safe : un **producteur** y écrit, un
**consommateur** y lit. Ici, le tout dans un service hébergé, en un seul passage déterministe.

## Objectif

Lis une ligne d'entiers séparés par des espaces (enregistrée par DI dans le starter). Dans
`Pipeline : BackgroundService`, `ExecuteAsync` doit :

1. créer un `Channel.CreateUnbounded<int>()` ;
2. **produire** : écrire chaque valeur, puis `Writer.Complete()` ;
3. **consommer** via `Reader.ReadAllAsync` (ordre FIFO) : pour chaque valeur, journaliser
   `Traité {Valeur} -> {Double}` (le double), en accumulant le total ;
4. journaliser `Total = {Total}`, puis arrêter l'hôte.

## Exemple

Entrée :

```
1 2 3
```

Sortie :

```
Pipeline [Information] Traité 1 -> 2
Pipeline [Information] Traité 2 -> 4
Pipeline [Information] Traité 3 -> 6
Pipeline [Information] Total = 12
```

## Livrables

- `Pipeline.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
