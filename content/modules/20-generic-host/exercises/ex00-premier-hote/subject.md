# Premier hôte

Le **Generic Host** est le squelette d'une application .NET moderne : il assemble la configuration,
l'injection de dépendances, le logging, et fait tourner des **services hébergés**
(`IHostedService` / `BackgroundService`) avec un cycle de vie géré.

## Objectif

Construis un hôte avec `Host.CreateApplicationBuilder(args)`, enregistre un `BackgroundService`
nommé `Worker` qui, dans `ExecuteAsync`, journalise **une** ligne en Information — incluant la
**consigne lue sur l'entrée standard** — puis demande l'arrêt de l'application.

L'entrée standard contient **une ligne** : la consigne (la tâche à effectuer). Elle est lue dans
le starter, puis **enregistrée dans le conteneur DI** (`AddSingleton(new Consigne(...))`) et
**injectée** dans le `Worker` — c'est la façon idiomatique de fournir une donnée à un service hébergé.

La configuration du logging (provider fourni `LogCapture.cs` + on tait les logs internes du host
via `AddFilter("Microsoft", LogLevel.None)`) et le câblage de la consigne sont **déjà écrits** dans
le starter.

## Marche à suivre

Dans `ExecuteAsync` :

```csharp
_logger.LogInformation("Hôte démarré, travail effectué : {Tache}", _consigne.Tache);
_lifetime.StopApplication();   // sinon l'hôte tournerait indéfiniment
```

## Sortie attendue

Pour l'entrée `Indexer le catalogue` :

```
Worker [Information] Hôte démarré, travail effectué : Indexer le catalogue
```

> La catégorie `Worker` vient de `ILogger<Worker>` : le type donne la catégorie.

## Livrables

- `PremierHote.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
