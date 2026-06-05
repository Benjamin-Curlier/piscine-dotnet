# Module 20 — Generic Host & services hébergés

Le **Generic Host** est le squelette d'une application .NET moderne (services, workers, web). Il
réunit en un seul objet la **configuration**, l'**injection de dépendances** (module 18), le
**logging** (module 19) et la gestion du **cycle de vie** de services qui tournent en arrière-plan.

## 1. Construire un hôte

```csharp
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services : conteneur DI
// builder.Logging  : configuration des logs
// builder.Configuration : appsettings, variables d'env…

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();   // démarre les services hébergés et BLOQUE jusqu'à l'arrêt
```

> **Dans la piscine.** On configure le logging avec le provider fourni `LogCapture.cs` et on **tait
> les logs internes du host** avec `builder.Logging.AddFilter("Microsoft", LogLevel.None)` pour une
> sortie déterministe :
>
> ```csharp
> builder.Logging.ClearProviders();
> builder.Logging.AddProvider(new CaptureLoggerProvider());
> builder.Logging.SetMinimumLevel(LogLevel.Information);
> builder.Logging.AddFilter("Microsoft", LogLevel.None);
> ```

## 2. Un service hébergé : `BackgroundService` {#premier-hote}

Le plus simple est d'hériter de `BackgroundService` et de surcharger `ExecuteAsync` :

```csharp
sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Travail effectué");
        _lifetime.StopApplication();   // sinon l'hôte tourne indéfiniment
        return Task.CompletedTask;
    }
}
```

`IHostApplicationLifetime.StopApplication()` demande l'**arrêt propre** : indispensable pour un
travail « one-shot » (sinon `host.Run()` ne rend jamais la main).

## 3. Le cycle de vie : `StartAsync` / `StopAsync` {#cycle-vie}

`BackgroundService` repose sur `IHostedService`, qu'on peut aussi implémenter **directement** pour
maîtriser les deux temps :

```csharp
sealed class Cycle : IHostedService
{
    public Task StartAsync(CancellationToken ct) { /* au démarrage */ return Task.CompletedTask; }
    public Task StopAsync(CancellationToken ct)  { /* à l'arrêt    */ return Task.CompletedTask; }
}
```

L'hôte appelle `StartAsync` de **tous** les services au démarrage, puis `StopAsync` à l'arrêt
(dans l'ordre inverse d'enregistrement). L'ordre Start → Stop est garanti.

## 4. Injecter des dépendances dans un worker {#travail}

Un service hébergé est résolu par le conteneur : son constructeur reçoit tout ce qui est enregistré
(`ILogger<T>`, tes propres services, options…).

```csharp
builder.Services.AddSingleton(new Parametres(n));
builder.Services.AddHostedService<Travailleur>();
// Travailleur(ILogger<Travailleur>, IHostApplicationLifetime, Parametres) ← injectés
```

C'est la convergence des modules 18 (DI), 19 (logging) et 20 (host).

## 5. Producteur/consommateur dans l'hôte {#pipeline}

Un worker orchestre souvent un **pipeline** : un `Channel<T>` (module 21) découple la production de
la consommation. En un seul passage déterministe :

```csharp
var channel = Channel.CreateUnbounded<int>();
foreach (var v in valeurs) await channel.Writer.WriteAsync(v);
channel.Writer.Complete();

await foreach (var v in channel.Reader.ReadAllAsync())
{
    // traiter v…
}
```

C'est le cœur d'un **Worker Service** réel (lecture d'une file, traitement, journalisation).

## Références

- [.NET Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host)
- [BackgroundService](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.backgroundservice)
- [Worker Services](https://learn.microsoft.com/dotnet/core/extensions/workers)
