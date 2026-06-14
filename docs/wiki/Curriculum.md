# Curriculum

Le parcours va des fondamentaux C# débutant jusqu'à un palier avancé proche de la stack de
production. **Git est tissé dans tout le parcours** (pratiqué à chaque rendu) + deux modules git
dédiés. Le contenu est généré **progressivement** au fil des itérations.

> **Release `v3.0.0`** : la piscine couvre **M00–M39 + Rushes 0/1/2/3** (contenu cumulé depuis v1.0 :
> tronc commun + palier avancé, **approfondissement C#/.NET** M24–M35, **plateformes & architecture**
> M36–M39) et ajoute l'**app de bureau** + les **installeurs** Windows/Linux. Voir
> [CHANGELOG.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/CHANGELOG.md).
>
> **UX recrue** : le **contenu ci-dessous est inchangé**, accessible via une **app de bureau**
> (**tableau de bord** · cours · **plan de travail + bouton Ouvrir** · vérification avec diff coloré ·
> progression · **rapport exportable** · **réglages** · **terminal + coaching git** · résultat **riche**)
> **ou** le **CLI** `piscine`. Le rendu reste un `git push` (cf.
> [docs/mise-en-oeuvre.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/mise-en-oeuvre.md)).

## Tronc commun

| # | Module | Notions clés | Git |
|---|---|---|---|
| 00 | Setup & Git | installer, lancer `piscine`, hello world | clone/add/commit/push, 1er rendu |
| 01 | Bases C# | types, variables, I/O console, opérateurs, conditions | commits atomiques |
| 02 | Boucles | `for`/`while`/`foreach`, itération | messages de commit clairs |
| 03 | Méthodes | paramètres, portée, retour, récursion | — |
| 04 | Tableaux & chaînes | `array`, manipulation de `string` | `.gitignore` |
| 05 | ★ Git intermédiaire | (dédié) | branches, merge, conflits, historique |
| 06 | Collections | `List`, `Dictionary`, intro LINQ | — |
| 07 | POO 1 | classes, objets, encapsulation, propriétés | — |
| 08 | POO 2 | héritage, interfaces, polymorphisme, abstrait | — |
| 09 | Exceptions | `try/catch`, gestion d'erreurs, `Result` | — |
| 10 | Génériques & lambdas | `T`, délégués, `Func`/`Action` | — |
| 11 | LINQ | requêtes, projection, agrégation | — |
| 12 | Async/await | `Task`, asynchrone, annulation | — |
| 13 | Tests unitaires | xUnit, écrire ses propres tests ; `ex03-mutation` (`mutation`) : écrire des tests qui tuent des mutants | — |
| 14 | ★ Git avancé / collab | (dédié) | rebase, workflow MR GitLab, revue de code |

## Palier avancé

| # | Module | Notions clés |
|---|---|---|
| 15 | Regex | motifs, groupes, validation, `Regex` performant |
| 16 | Sérialisation | `System.Text.Json`, (dé)sérialisation, converters |
| 17 | Réflexion & attributs | `Type`, introspection, attributs custom |
| 18 | Injection de dépendances | `Microsoft.Extensions.DependencyInjection`, durées de vie |
| 19 | Logging | `Microsoft.Extensions.Logging`, niveaux, scopes, providers |
| 20 | Generic Host & Worker | `HostBuilder`, `BackgroundService`, config & options |
| 21 | Threading avancé | `Channel<T>`, producteur/consommateur, `Parallel`, synchro |
| 22 | Réseau | sockets TCP/UDP, `HttpClient` |
| 23 | Design patterns | GoF essentiels en C# (Strategy, Factory, Observer, Decorator…) |

## Rushes (solo, projets de synthèse)

- **Rush 0** (après ~M04) : programme console ludique (ASCII-art / mini-calculatrice / FizzBuzz avancé).
- **Rush 1** (après POO, ~M08) : appli métier console (gestionnaire d'inventaire / bibliothèque).
- **Rush 2** (après LINQ/async, ~M12) : CLI de traitement de données (parser, agréger, rapport).
- **Rush 3** (`r3-traitement`, après palier avancé) : **Worker Service déterministe auto-noté** —
  un `BackgroundService` single-shot consomme une file `Channel<Commande>` (réseau *simulé* en
  mémoire) injectée par DI, journalise via `LogCapture`, dresse un bilan, puis `StopApplication()`.
  La partie réseau réelle reste une pratique locale non notée.
- **Rush 4** (`r4-clean-arch`, après M36) : **Clean Architecture auto-notée `projet`** — un catalogue
  de produits découpé en couches `Domain` / `Application` / `Infrastructure` + composition root.
  La moulinette compile tous les fichiers, compare la sortie **et** vérifie la **règle de dépendance**
  par assertions Roslyn (`requires_types` + `forbidden_dependencies` namespace→namespace).

## Cours & références externes

Chaque `cours.md` : explications progressives en français + exemples + **références externes**
(Microsoft Learn, freeCodeCamp, chaînes YouTube type Nick Chapsas / Tim Corey, docs officielles
.NET). Ton pédagogique, jargon expliqué.

## État de la notation par module

- **Modules auto-notés (`io`)** : M00, M01, M02, M03, M04, M06, M07, M08, M09, M10, M11, M12, M13,
  M15, M16, M17, M18, **M19 (Logging), M20 (Generic Host)**, M21, M23.
- **Exos bonus du cœur débutant** (non bloquants, `difficulty: difficile`) : **M01** `ex03-fizzbuzz`,
  **M02** `ex03-fibonacci`, **M03** `ex03-puissance`, **M04** `ex03-anagramme`, **M06** `ex03-top-k`,
  **M07** `ex03-pile`, **M08** `ex03-vehicule`. (M05 = module git dédié, pas d'exo `io`.)
- **Exos bonus intermédiaires** (non bloquants, `difficulty: difficile`) : **M09** `ex03-calculatrice-robuste`,
  **M10** `ex03-reduce`, **M11** `ex03-mot-frequent`, **M12** `ex03-somme-carres`,
  **M15** `ex03-compter-mot`, **M16** `ex03-stats-json`, **M17** `ex03-decrire-objet`,
  **M18** `ex03-pipeline-di`, **M21** `ex03-somme-parallele-carres`, **M23** `ex03-decorator`.
  (M05 = git, M14/M22 = lecture ; M13 a déjà un 4ᵉ exo `ex03-mutation`.)
- **Modules de lecture/pratique guidée** (cours + checklist, sans auto-notation pour l'instant) :
  M14 (git avancé), M22 (réseau). *(M05 est désormais auto-noté via le grader `git` — voir V3.)*
- **Rushes auto-notés** : Rush 0, Rush 1, Rush 2, **Rush 3** (post-v1.0, worker déterministe),
  **Rush 4** (`r4-clean-arch`, Clean Architecture, grader `projet` : io + assertions d'architecture).
- **Module V3 (grader `git`)** : **M05 Git intermédiaire** — exo `ex00-branche-merge` (branches +
  fusion) auto-noté : la moulinette inspecte l'état du dépôt (corrigé décrit par une *fixture*).
- **Modules V3 (grader `projet`)** : **M36 Clean Architecture** (`ex00-couches` : couches
  Domain/Application/Infrastructure + composition root ; noté io + assertions d'architecture).
- **Modules V3 (lecture)** : **M37 Docker** (conteneurisation .NET 10 : Dockerfile multi-étapes,
  `dotnet publish -t:PublishContainer` sans Dockerfile, images chiseled) ; **M38 Silk.NET**
  (fenêtrage & rendu GPU : boucle Load/Update/Render, OpenGL, entrées ; guidé, non déterministe) ;
  **M39 Blazor** (interfaces web en C# : composants, paramètres, `@bind`, cycle de vie, DI, modèles
  de rendu .NET 10 ; guidé, web/DOM non déterministe).
- **M19/M20 débloqués** (post-v1.0, en `io`) : un `ILoggerProvider` synchrone fourni rend la sortie
  des logs déterministe (captée par le grader) — solution **contenu pur**, sans changement moteur.
- **Hors périmètre v1.0** (drafts + design sur la branche `v1.0-blockers`, à traiter ensuite) :
  Rush 3, et les graders dédiés git/réseau/élève-écrit-tests.
  Détail : [docs/superpowers/BLOCKERS-v1.0.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/v1.0-blockers/docs/superpowers/BLOCKERS-v1.0.md).

## Palier v2 — approfondissement C#/.NET (en cours)

Modules au-delà du tronc spec §6 (numérotation M24+ ; M00–M23 figés depuis v1.0). Introduisent le
**niveau de difficulté** (`difficulty: facile|moyen|difficile`) et des exercices **bonus** non bloquants.

| # | Module | État |
|---|---|---|
| 24 | Switch & pattern matching (C# 14) | ✅ io |
| 25 | Enums | ✅ io |
| 26 | Static, const, readonly & immutabilité | ✅ io |
| 27 | Opérations binaires | ✅ io |
| 28 | Complexité (Big O) & tris | ✅ io |
| 29 | Recherche de chemin (BFS, Dijkstra, A*) | ✅ io (+bonus) |
| 30 | Design patterns (suite) : Singleton, Adapter, Decorator, Builder, Command | ✅ io (+bonus) |
| 31 | Smelly code & refactoring | ✅ io (+bonus) |
| 32 | Garbage collection & gestion des ressources | ✅ io (+bonus) |
| 33 | Discriminated unions (hiérarchies scellées) | ✅ io (+bonus) |
| 34 | Interopérabilité (P/Invoke & code natif) | ✅ lecture |
| 35 | Entity Framework Core (SQLite in-memory) | ✅ io (+bonus) |

## Palier v3 — plateformes & architecture

Modules « plateforme » et architecture multi-fichiers, adossés aux nouveaux graders **`git`**,
**`projet`**, **`reseau`** et **`mutation`**. Les modules dont la sortie n'est pas déterministe en
console (web/DOM, GPU, conteneurs) sont livrés en **lecture guidée**, cohérent avec M14/M22/M34.

| # | Module | État |
|---|---|---|
| 05 | Git intermédiaire (branches, merge) | ✅ auto-noté `git` (fixture) |
| 36 | Clean Architecture (couches + composition root) | ✅ auto-noté `projet` (io + assertions d'archi) |
| 37 | Docker (conteneurisation .NET 10) | ✅ lecture |
| 38 | Silk.NET (fenêtrage & rendu GPU) | ✅ lecture |
| 39 | Blazor (interfaces web en C#) | ✅ lecture |

Graders introduits au palier v3 : **`git`** (état attendu du dépôt rendu, via fixture LibGit2Sharp),
**`projet`** (compilation multi-fichiers + assertions d'architecture Roslyn), **`reseau`** (harnais
d'écho TCP loopback), **`mutation`** (l'élève écrit des tests qui doivent tuer des mutants — pilote M13).

Roadmap v2/v3 complète (dont v3 : Blazor, Docker, Silk.NET, clean architecture, graders projet) :
[docs/superpowers/plans/2026-05-31-roadmap-v2-v3.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/superpowers/plans/2026-05-31-roadmap-v2-v3.md).

> Historique d'avancement : voir les itérations dans
> [docs/superpowers/plans/](https://github.com/Benjamin-Curlier/piscine-dotnet/tree/main/docs/superpowers/plans).
