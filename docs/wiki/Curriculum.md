# Curriculum

Le parcours courant contient **53 modules (M00–M52), 185 exercices auto-corrigés et 8 Rushes**.
Git est pratiqué à chaque rendu et possède deux modules dédiés. Le fil rouge complet, le rythme et
les règles d'XP sont décrits dans [`content/parcours.md`](../../content/parcours.md).

Chaque exercice affiche une difficulté, une durée active indicative, des XP, des compétences et son
rôle dans la mission Asteria. Ces informations orientent l'apprenant sans verrouiller la navigation.

## Acte I — Prise de quart (M00–M05)

| # | Module | Notions clés |
|---|---|---|
| 00 | Mise en place & Git | SDK/CLI, fichier C# .NET 10, premier rendu |
| 01 | Bases C# | types, variables, I/O, opérateurs, conditions |
| 02 | Boucles | `for`, `while`, `foreach`, invariants |
| 03 | Méthodes | paramètres, retours, portée, récursion |
| 04 | Tableaux & chaînes | indexation, transformations, parsing |
| 05 | Git intermédiaire | branches, merge, conflits, historique réel auto-noté |

## Acte II — Cœur métier (M06–M14)

| # | Module | Notions clés |
|---|---|---|
| 06 | Collections | `List`, `Dictionary`, ensembles, agrégation |
| 07–08 | POO 1 & 2 | encapsulation, héritage, interfaces, polymorphisme |
| 09 | Exceptions | erreurs attendues, exceptions, `Result` |
| 10 | Génériques & lambdas | `T`, délégués, `Func`/`Action` |
| 11 | LINQ | filtrage, projection, groupement, agrégation |
| 12 | Async/await | `Task`, annulation, concurrence asynchrone |
| 13 | Tests unitaires | vrais tests xUnit et mutation testing sur quatre exercices |
| 14 | Git avancé | rebase, revue, MR/PR et hotfix intégré sur deux branches |

## Acte III — Services en production (M15–M23)

| # | Module | Notions clés |
|---|---|---|
| 15 | Regex | motifs, groupes, validation |
| 16 | Sérialisation | `System.Text.Json`, contrats et converters |
| 17 | Réflexion & attributs | métadonnées, découverte de types |
| 18 | Injection de dépendances | ports, composition et durées de vie |
| 19 | Logging | logs structurés, niveaux, scopes, providers |
| 20 | Generic Host & Worker | `BackgroundService`, config, options, arrêt |
| 21 | Threading avancé | `Channel<T>`, parallélisme, synchronisation |
| 22 | Réseau | TCP/UDP, HTTP et harnais loopback |
| 23 | Design patterns | Strategy, Factory, Observer, Decorator |

## Acte IV — Algorithmes et langage avancé (M24–M33)

| # | Module | Notions clés |
|---|---|---|
| 24 | Switch & patterns | expressions switch, propriétés et listes |
| 25 | Enums | flags, parsing et domaine fermé |
| 26 | Static/const/readonly | invariants et immutabilité |
| 27 | Binaire | masques, bits, encodage |
| 28 | Complexité & tris | Big O, tris et choix de structure |
| 29 | Recherche de chemin | BFS, Dijkstra, A* |
| 30 | Patterns (suite) | Singleton, Adapter, Chain, Builder, Command |
| 31 | Refactoring | constantes, extractions, gardes, remplacement conditionnel |
| 32 | Mémoire & GC | `IDisposable`, finalisation, pression mémoire |
| 33 | Unions discriminées | hiérarchies scellées et exhaustivité |

## Acte V — Frontières et interfaces (M34–M39)

| # | Module | Validation déterministe |
|---|---|---|
| 34 | Interopérabilité | contrat `LibraryImport` inspecté sans charger de bibliothèque native |
| 35 | EF Core | SQLite in-memory, requêtes et persistance |
| 36 | Clean Architecture | compilation multi-fichiers et règle de dépendance Roslyn |
| 37 | Docker | Dockerfile multi-stage et `.dockerignore` inspectés |
| 38 | Silk.NET | vertex/fragment shaders inspectés sans GPU |
| 39 | Blazor | composants `.razor`, état et chargement async inspectés sans navigateur |

Les exercices d'inspection ne prétendent pas remplacer le test cible : le cours demande ensuite de
construire l'image, compiler le shader ou ouvrir l'interface dans l'environnement réel.

## Acte VI — Asteria distribué (M40–M43)

| # | Module | Notions clés |
|---|---|---|
| 40 | Brokers & messaging | NATS/RabbitMQ/ZeroMQ, topologies, livraison, idempotence |
| 41 | NATS .NET | Core NATS, queue groups, request/reply, JetStream et ACK |
| 42 | Observabilité & résilience | OTel, corrélation, retry, circuit breaker, health |
| 43 | .NET Aspire 13 | AppHost, références, `WaitFor`, Service Defaults et dashboard |

## Acte VII — Applications d'entreprise hybrides (M44–M52)

Le poste lourd reste le produit principal. Les technologies web servent ici de façades et de contrats
pour les échanges avec les services, l'identité et l'exploitation du système distribué.

| # | Module | Notions clés |
|---|---|---|
| 44 | Socle .NET d'entreprise | `.slnx`, projets, MSBuild, nullable, analyseurs, Options et configuration |
| 45 | Desktop & MVVM | état observable, thread UI, dispatcher, annulation et résultat obsolète |
| 46 | ASP.NET Core services | Minimal API, ProblemDetails, OpenAPI et intégration du client lourd |
| 47 | Sécurité & identité | JWT/OIDC, policies, secrets, mTLS et modèle de menaces |
| 48 | Données de production | SQL, index, migrations, concurrence, transactions et pagination |
| 49 | gRPC & Protobuf | RPC typé, streaming, deadlines et compatibilité de schéma |
| 50 | Tests d'intégration | WebApplicationFactory, dépendances conteneurisées et tests de contrats |
| 51 | CI/CD & livraison | gates, builds déterministes, desktop signé, images et rollback |
| 52 | Diagnostic & performance | counters, traces, dumps, cache distribué et budgets système |

## Rushes — projets de synthèse

| Rush | Synthèse |
|---|---|
| R0 — FizzBuzz | fondamentaux console et décomposition |
| R1 — Inventaire | collections et modèle objet |
| R2 — Rapport | parsing, LINQ, agrégation, async |
| R3 — Traitement | Worker, DI, Channel, logging et arrêt propre |
| R4 — Clean Architecture | domaine, application, infrastructure et composition root |
| R5 — Event processor | JSON, async, inbox idempotente, audit et architecture |
| R6 — Asteria distribué | AppHost Aspire, NATS, Service Defaults et runbook de reprise |
| R7 — Poste d'entreprise | client lourd, façade sécurisée, gRPC, EF/outbox, tests, CI et runbook hybride |

L'application expose ces missions dans la page **Rushes**, les recommande après leur module jalon et
affiche leur progression séparément des 185 exercices. Les jalons conseillés restent non bloquants.

## Graders et preuve pédagogique

- `io` : comportement observable et cas limites ;
- `unit` / `mutation` : tests écrits par l'apprenant et mutants réellement tués ;
- `source` : technique requise ou interdite, commentaires exclus, occurrences possibles ;
- `projet` : compilation multi-fichiers et dépendances entre couches ;
- `git` : branches, commits, fusions et contenu du dépôt ;
- `reseau` : serveur TCP/HTTP loopback contrôlé ;
- `fichier` : contrat de livrables texte non compilables dans la moulinette ;
- `norme` : diagnostics de forme, bloquants ou consultatifs selon l'exercice.

`piscine validate-content` charge strictement tous les manifests et confronte chaque corrigé à ses
graders. `piscine audit-content` signale en plus les modules vides, fuites de solution dans cours,
indices ou starters, calibrages atypiques et objectifs trop proches.
