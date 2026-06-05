# Premier hôte

Le **Generic Host** est le squelette d'une application .NET moderne : il assemble la configuration,
l'injection de dépendances, le logging, et fait tourner des **services hébergés**
(`IHostedService` / `BackgroundService`) avec un cycle de vie géré.

## Objectif

Construis un hôte avec `Host.CreateApplicationBuilder(args)`, enregistre un `BackgroundService`
nommé `Worker` qui, dans `ExecuteAsync`, journalise **une** ligne en Information puis demande
l'arrêt de l'application.

La configuration du logging (provider fourni `LogCapture.cs` + on tait les logs internes du host
via `AddFilter("Microsoft", LogLevel.None)`) est **déjà écrite** dans le starter.

## Marche à suivre

Dans `ExecuteAsync` :

```csharp
_logger.LogInformation("Hôte démarré, travail effectué");
_lifetime.StopApplication();   // sinon l'hôte tournerait indéfiniment
```

## Sortie attendue

```
Worker [Information] Hôte démarré, travail effectué
```

> La catégorie `Worker` vient de `ILogger<Worker>` : le type donne la catégorie.

## Livrables

- `PremierHote.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
