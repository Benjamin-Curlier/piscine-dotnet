# Module 43 — .NET Aspire 13

.NET Aspire aide à **décrire, lancer et diagnostiquer** une application distribuée pendant le
développement. Son AppHost est un modèle exécutable de ressources : projets .NET, conteneurs,
brokers, bases, paramètres et relations. Le dashboard agrège logs, traces, métriques, états et
commandes. Aspire ne remplace pas à lui seul l'orchestrateur de production.

Ce module cible Aspire 13 avec .NET 10. Aspire évolue indépendamment du SDK : conserve des versions
épinglées et consulte les notes de migration avant une mise à jour majeure.

## 1. Le modèle AppHost {#apphost}

Un AppHost traditionnel est un projet .NET dont le `Program.cs` décrit la topologie en C# :

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddContainer("cache", "redis", "8-alpine")
    .WithDataVolume();

builder.AddProject<Projects.Api>("api")
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
```

Aspire 13 permet aussi un **AppHost fichier** pour démarrer sans projet dédié. Dans les deux cas, le
code est une description : évite d'y placer du métier ou un script de déploiement impératif.

## 2. Ressources, références et attente {#ressources}

- `AddProject`, `AddContainer` et les intégrations ajoutent des ressources au modèle.
- `WithReference(resource)` exprime une relation et injecte la configuration nécessaire (par
  exemple une référence de service ou une chaîne de connexion selon le type de ressource).
- `WaitFor(resource)` retarde le démarrage jusqu'à ce que la dépendance soit prête selon son état et
  ses health checks.
- `WithEnvironment`, paramètres et secrets configurent sans coder une adresse de machine.

Une référence ne garantit pas, à elle seule, que la dépendance est prête. Une attente ne dispense pas
le client de timeout, de reconnexion et de gestion des pannes après le démarrage.

## 3. Service discovery {#discovery}

Le consommateur utilise un nom logique, par exemple `https+http://catalogue`, au lieu d'un port local
éphémère. Aspire fournit les informations de découverte selon l'environnement. Le code métier ne doit
pas lire la topologie de l'AppHost ni connaître Docker.

Les intégrations Aspire configurent souvent client, health checks et télémétrie, mais lis leur contrat
exact : une intégration n'ajoute pas forcément persistance, retry ou migration de schéma.

## 4. Service Defaults {#service-defaults}

Le projet ServiceDefaults centralise les préoccupations transverses :

```csharp
builder.AddServiceDefaults();
// ... endpoints métier
app.MapDefaultEndpoints();
```

Le modèle courant configure OpenTelemetry, health checks, service discovery et résilience HTTP. Ce
projet reste une bibliothèque partagée, pas un microservice. Expose séparément `/health` (readiness)
et `/alive` (liveness), et n'envoie pas de données sensibles dans la télémétrie.

## 5. Dashboard et diagnostic {#dashboard}

`aspire run` lance l'AppHost et ouvre le dashboard. Utilise la vue des ressources pour constater
l'état réel, les endpoints et les commandes ; suis ensuite une requête dans les traces et corrèle ses
logs. Le dashboard de développement reçoit OpenTelemetry sans obliger chaque équipe à installer
d'abord un backend complet.

Un écran vert n'est pas une preuve de résilience. Pour valider un scénario : coupe une ressource,
observe timeout et dégradation, redémarre-la, vérifie reconnexion **et** récupération des données ou de
la topologie attendue.

## 6. CLI et cycle de travail {#cli}

```text
aspire new
aspire add
aspire run
aspire publish
```

La CLI crée, enrichit et exécute des AppHosts ; `publish` produit des artefacts destinés à une cible
de déploiement. Le résultat doit encore passer par la stratégie de secrets, réseau, stockage,
sécurité et exploitation de cette cible.

## 7. Atelier Asteria {#atelier}

Compose une API, un worker et NATS avec JetStream. L'API publie une commande, le worker la traite de
façon idempotente et expose ses signaux OpenTelemetry. Prouve successivement : nominal, queue group,
arrêt worker, rejeu, arrêt broker, reconnexion et absence de double effet.

## Références

- [Vue d'ensemble .NET Aspire](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [AppHost](https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview)
- [Architecture Aspire](https://learn.microsoft.com/dotnet/aspire/architecture/overview)
- [CLI Aspire](https://learn.microsoft.com/dotnet/aspire/cli/overview)
