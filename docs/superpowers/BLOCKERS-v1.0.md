# Blocages v1.0 — à traiter après le tag

> Branche dédiée `v1.0-blockers`. Ces points ne se gradent PAS proprement en `io` avec le moteur
> actuel, ou demandent un travail moteur/packaging additionnel. Ils ont été **sortis du périmètre du
> tag v1.0** (décision proprio : « curriculum complet, blocages branchés »). Chacun est décrit avec le
> problème, la cause, une résolution proposée et un ordre de grandeur d'effort.

## Contexte moteur utile
- **Grader `io`** (`Piscine.Grading`) : compile le `solution/<F>.cs` via Roslyn (refs = DLLs du
  `TRUSTED_PLATFORM_ASSEMBLIES` du binaire `piscine`), l'exécute dans un `AssemblyLoadContext`
  collectible en **redirigeant `Console.SetOut`** vers un `StringWriter`, puis compare stdout/exit.
- Depuis It.20, le binaire embarque `Microsoft.Extensions.DependencyInjection/Logging/Logging.Console/Hosting`
  → résolus par le grader (prouvé par M18 DI).

---

## 1. M19 — Logging (exercice io)  🟠 moteur
**Problème.** Un exercice « configure un logger, logge des messages, on compare la sortie » ne capture
pas de façon fiable le stdout avec le grader actuel.
**Cause.**
- Le provider console de `Microsoft.Extensions.Logging.Console` écrit via un **thread d'arrière-plan**
  (`ConsoleLoggerProcessor`) sur le handle console natif — il ne respecte pas forcément le
  `Console.SetOut(StringWriter)` posé par l'`IoGrader` → sortie non capturée.
- Écriture **asynchrone** → ordre non garanti vis-à-vis des `Console.WriteLine` directs ; flush au
  `Dispose` du `LoggerFactory` seulement.
**Résolutions possibles.**
1. **Provider de test maison** fourni dans le starter : un `ILoggerProvider` minimal qui écrit via
   `Console.WriteLine` (donc capté par la redirection). L'élève apprend l'API `ILogger`/niveaux/scopes,
   la sortie est déterministe. *(le plus simple, recommandé)*
2. Capturer le **vrai stdout OS** dans le grader (rediriger le file descriptor, pas seulement
   `Console.Out`) — plus invasif, profite à d'autres cas.
3. Configurer `AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = null; })` et matcher
   le format `niveau: Catégorie[0] message` — fragile, à éviter pour des débutants.
**Effort.** Option 1 : ~½ journée (contenu + 1 mini-affordance starter). 

## 2. M20 — Generic Host & Worker (exercice io)  🟠 moteur
**Problème.** Un `IHost`/`BackgroundService` est long-running : pas d'arrêt déterministe naturel, et le
host logge via le provider console (même souci que M19).
**Résolution proposée.** Exercice « single-shot » : `Host.CreateApplicationBuilder`, enregistrer un
service, dans un `BackgroundService` faire UNE unité de travail déterministe puis
`IHostApplicationLifetime.StopApplication()` ; logger via le provider maison du point 1. Documenter le
cycle `StartAsync`/`StopAsync`. **Dépend de la résolution du point 1.**
**Effort.** ~1 journée après le point 1.

## 3. Rush 3 — Worker Service complet  🔴 moteur + réseau
**Problème.** Synthèse `Channel<T>` + **I/O réseau** + `ILogger` + DI sous `HostBuilder` : entrelace
host-lifetime (point 2), capture du logging (point 1) et **réseau non déterministe** (point 5).
**Résolution proposée.** Le découper : version notée déterministe (Channel + DI + worker single-shot,
sans réseau réel → réseau simulé par une file en mémoire) une fois 1/2 résolus ; la partie réseau réelle
reste un livrable « pratique locale » non noté.
**Effort.** ~1–2 jours après 1/2.

## 4. Grader git dédié (M05, M14)  🔴 moteur (nouveau type de grading)
**Problème.** M05/M14 sont aujourd'hui des modules de **lecture** (groupe vide). On aimerait noter de
vraies opérations git (créer une branche, fusionner, résoudre un conflit, rebaser).
**Résolution proposée.** Nouveau type de grading `git` : le manifest décrit un **état attendu du dépôt**
(branches présentes, nombre de commits, contenu d'un fichier après merge, absence de marqueurs de
conflit, forme de l'historique). Le grader inspecte le dépôt rendu via **LibGit2Sharp** (déjà au repo,
cf. `Piscine.Git`). S'appuyer sur le pipeline de rendu git existant (`GradeReceivedCommand`).
**Effort.** ~2–3 jours (modèle manifest + `GitGrader` + tests).

## 5. Harnais réseau (M22)  🔴 moteur
**Problème.** Sockets/`HttpClient` ne sont pas déterministes hors d'un environnement contrôlé.
**Résolution proposée.** Fournir au grader un **serveur de test embarqué** (écho TCP + petit serveur
HTTP `HttpListener` sur port éphémère, injecté via variables d'env/host:port) ; l'exercice cible ce
serveur ; comparaison déterministe de la sortie. Timeouts stricts.
**Effort.** ~2–3 jours.

## 6. Grader « élève écrit les tests » (M13)  🟠 moteur
**Problème.** M13 enseigne xUnit mais ne note pas (pour l'instant) des tests écrits PAR l'élève.
**Résolution proposée.** Le `UnitGrader` existe déjà (exécute des `[Fact]` par réflexion dans un ALC).
Inverser le schéma : exécuter les tests de l'élève contre (a) une **implémentation correcte** fournie
→ doivent PASSER, et (b) une ou plusieurs **implémentations boguées** (mutants) → doivent ÉCHOUER. Un
jeu de tests qui ne tue pas les mutants est rejeté. Réutilise `CompilationService` + `UnitGrader`.
**Effort.** ~2 jours (orchestration mutants + format manifest).

---

## Ordre de traitement conseillé (post-1.0)
1. **Point 1 (provider logging de test)** — débloque M19, puis M20 (point 2), puis Rush 3 (point 3).
2. **Point 6 (élève-écrit-tests)** — valorise M13, base réutilisable.
3. **Point 4 (grader git)** — fort intérêt pédagogique, s'appuie sur l'existant `Piscine.Git`.
4. **Point 5 (harnais réseau)** — le plus lourd, le moins prioritaire.

> Chaque point fera l'objet d'une itération dédiée avec plan TDD (skills brainstorming → writing-plans →
> executing-plans), comme le reste du moteur.
