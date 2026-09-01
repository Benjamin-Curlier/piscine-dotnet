# Module 50 — Tests d'intégration et de contrats

Un test unitaire isole une règle. Il ne prouve pas que routing, sérialisation, SQL, authentification
ou broker sont correctement branchés. Les tests d'intégration couvrent ces frontières avec un coût
supérieur : il faut donc choisir des scénarios à forte valeur.

## 1. Pyramide et responsabilités {#pyramide}

- beaucoup de tests unitaires rapides pour le métier ;
- des tests d'intégration pour chaque adaptateur important ;
- quelques scénarios de bout en bout pour les parcours critiques ;
- des essais manuels ou instrumentés pour ergonomie, charge et panne complexe.

Ne remplace pas la base ou le protocole que tu veux précisément vérifier par un mock. Inversement, un
test unitaire n'a pas besoin de Docker.

## 2. ASP.NET Core en mémoire {#webapplicationfactory}

`WebApplicationFactory<TEntryPoint>` démarre l'application avec son pipeline réel via `TestServer`.
Le test obtient un `HttpClient`, remplace éventuellement une dépendance et vérifie le contrat HTTP.
Sépare le projet de tests unitaires du projet d'intégration pour contrôler coût et environnement.

Teste succès, validation, absence d'identité, conflit et dépendance indisponible. Une fausse
authentification de test doit rester limitée à l'assembly de tests.

## 3. Dépendances conteneurisées {#testcontainers}

Une base PostgreSQL, SQL Server ou un broker lancé pour la suite valide le vrai provider. Le test
attend la readiness, applique les migrations, insère ses propres données et détruit l'environnement.
Un nom ou réseau unique évite les collisions en exécution parallèle.

Le test ne dépend ni d'un service partagé entre développeurs ni d'un ordre d'exécution. Capture logs
et artefacts en cas d'échec pour rendre la CI diagnostiquable.

## 4. Tests de contrats {#contracts}

Valide OpenAPI et Protobuf comme artefacts versionnés. Un contrôle de compatibilité refuse la
suppression d'une réponse HTTP attendue ou la réutilisation d'un numéro Protobuf. Pour le messaging,
rejoue d'anciens messages contre le nouveau consumer.

Les tests du ViewModel peuvent utiliser un dispatcher synchrone et un faux port applicatif ; un test
UI dédié vérifie ensuite binding, navigation et threading du framework.

### Atelier réel

Monte API, base et NATS dans un environnement isolé. Soumets une commande depuis un client HTTP,
attends son traitement, vérifie la base puis rejoue la même clé d'idempotence.

Références :

- [Tests d'intégration ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [Tests dans .NET](https://learn.microsoft.com/dotnet/core/testing/)
