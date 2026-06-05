# Module 19 — Logging

`Console.WriteLine` est pratique pour bricoler, mais une vraie application **journalise** : elle
émet des messages porteurs d'un **niveau** (gravité) et d'une **catégorie** (origine), qu'on peut
ensuite **filtrer**, formater et rediriger (console, fichier, service distant) **sans toucher au
code métier**. C'est le rôle de `Microsoft.Extensions.Logging`.

## 1. Les briques : `ILogger`, `ILoggerFactory`, provider

- **`ILogger`** : ce qu'utilise ton code pour émettre (`logger.LogInformation(...)`).
- **`ILoggerProvider`** : ce qui *écrit* réellement les logs (console, fichier…).
- **`ILoggerFactory`** : assemble providers + filtres et fabrique des `ILogger` par catégorie.

```csharp
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new MonProvider());      // où écrire
    builder.SetMinimumLevel(LogLevel.Information); // à partir de quel niveau
});

var logger = factory.CreateLogger("App");        // "App" = la catégorie
logger.LogInformation("Tout va bien");
```

> **Capturer la sortie dans la piscine.** Le provider console standard
> (`builder.AddConsole()`) écrit sur un **thread d'arrière-plan** : l'ordre n'est pas garanti et
> la moulinette ne peut pas comparer la sortie de façon fiable. Les exercices te **fournissent**
> donc un petit provider synchrone, `LogCapture.cs`, qui écrit immédiatement au format
> `Catégorie [Niveau] message`. En production, tu utiliserais `AddConsole()` ou une lib comme
> Serilog.

## 2. Premier log {#premier-log}

Obtenir un logger et émettre un message tient en trois lignes (voir l'extrait ci-dessus). Le
**niveau** par défaut des helpers :

| Méthode | Niveau |
| --- | --- |
| `logger.LogTrace(...)` | `Trace` |
| `logger.LogDebug(...)` | `Debug` |
| `logger.LogInformation(...)` | `Information` |
| `logger.LogWarning(...)` | `Warning` |
| `logger.LogError(...)` | `Error` |
| `logger.LogCritical(...)` | `Critical` |

## 3. Niveaux & filtrage {#niveaux}

`SetMinimumLevel(LogLevel.Information)` : tout message **strictement en dessous** d'`Information`
(donc `Trace` et `Debug`) est **ignoré**. C'est ainsi qu'on garde des logs verbeux dans le code
mais silencieux en production.

```csharp
logger.LogDebug("Détail interne");   // ignoré si minimum = Information
logger.LogInformation("Démarré");    // émis
logger.LogWarning("Disque presque plein");
```

## 4. Log structuré {#structure}

Ne **concatène pas** tes valeurs dans la chaîne. Donne un **modèle à trous nommés** et les valeurs
à part : les outils peuvent alors filtrer par `OrderId`, `UserId`, etc.

```csharp
// ✗ à éviter
logger.LogInformation("Commande " + id + " pour " + client);

// ✓ structuré
logger.LogInformation("Commande {Id} validée pour {Client}", id, client);
```

Les trous sont remplis **dans l'ordre des arguments** (`{Id}` ← `id`, `{Client}` ← `client`).

## 5. Catégories {#categories}

La **catégorie** identifie l'origine d'un log (souvent le nom complet de la classe). Avec
`ILogger<T>`, la catégorie est déduite de `T` :

```csharp
var logger = factory.CreateLogger("Paiement");     // catégorie explicite
ILogger<MonService> typed = factory.CreateLogger<MonService>(); // catégorie = MonService
```

On peut régler un niveau **par catégorie** avec `AddFilter` :

```csharp
builder.SetMinimumLevel(LogLevel.Information);   // défaut
builder.AddFilter("Db", LogLevel.Warning);       // mais "Db" : Warning et plus seulement
```

Ainsi `dbLogger.LogInformation(...)` est filtré, alors que `dbLogger.LogWarning(...)` passe.

## 6. Et après ?

`ILogger` s'injecte naturellement par **DI** (module 18) : un service reçoit
`ILogger<MonService>` dans son constructeur. Le **Generic Host** (module 20) configure le logging
pour toute l'application en un point unique.

## Références

- [Logging in .NET](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Log levels](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel)
- [Logging providers](https://learn.microsoft.com/dotnet/core/extensions/logging-providers)
