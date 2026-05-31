# Itération 20 — Vers la release v1.0 (curriculum complet)

> Décisions proprio (AskUserQuestion, 2026-05-31) : **v1.0 = curriculum complet M00–M23 + Rushes 0–3**,
> blocages isolés sur une branche dédiée ; **packaging : oui** (ajout `Microsoft.Extensions.*` au binaire) ;
> **git (M05/M14) + réseau (M22) = modules guidés** (lecture + checklist), grader dédié branché ;
> **M13 = contenu io simple** maintenant, grader « élève-écrit-tests » branché.

## État de départ
17 modules vivants (M00→M04, M06→M12, M15, M16, M17, M21, M23) + Rush 0. CI verte, 77 tests.

## Découverte moteur (vérifiée)
- `CompilationService.LoadReferences` : refs du grader = DLLs du `TRUSTED_PLATFORM_ASSEMBLIES` du binaire `piscine`.
  → ajouter des `PackageReference` au binaire les rend résolvables par Roslyn ET par l'ALC d'exécution (repli ALC Default, même mécanisme que xUnit).
- `ContentValidator.Validate` ne note que `module.Groups.SelectMany(g => g.Exercises)`.
  → un module avec `groups: []` (lecture seule) passe la gate **sans changement moteur**.

## Phase 1 — Packaging moteur (sur `main`, TDD léger)
Ajouter au `src/Piscine.Cli/Piscine.Cli.csproj` : `Microsoft.Extensions.DependencyInjection`,
`Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Console`, `Microsoft.Extensions.Hosting`.
- `dotnet build` + `dotnet test Piscine.slnx -c Release` verts.
- `validate-content` reste vert (pas de régression).
- Commit `feat(cli): embarque Microsoft.Extensions.* pour exercices DI/Logging/Host`. Push. CI verte.

## Phase 2 — Contenu auto-gradable propre (io), agents opus parallèles
| Élément | Type | Notes |
|---|---|---|
| **M18 DI** | io | `ServiceCollection`, enregistrement/résolution, durées de vie → sortie déterministe via `Console.WriteLine` (pas de logging). Dépend de Phase 1. |
| **M13 Tests** | io | Enseigne xUnit/assertions via exos io « logiques » (le grader élève-écrit-tests est branché). |
| **Rush 1** | io | Appli métier console (gestionnaire d'inventaire) — synthèse POO. |
| **Rush 2** | io | CLI de traitement de données (parser/agréger/rapport) — LINQ. |
| **M05 Git interm.** | lecture | `cours.md` (branches/merge/conflits/historique) + `module.yaml` groupe vide. Checklist pratiquée sur le vrai dépôt. |
| **M14 Git avancé** | lecture | `cours.md` (rebase, workflow MR GitLab, revue) + groupe vide. |
| **M22 Réseau** | lecture | `cours.md` (sockets TCP/UDP, `HttpClient`) + groupe vide. |

`validate-content` après chaque vague ; commit séparé par module/rush ; push ; CI verte.

## Phase 3 — Branche `v1.0-blockers` (drafts + design, NON mergée)
Items qui ne se gradent pas proprement en `io` avec le moteur actuel — drafts de contenu conservés + problème documenté :
- **M19 Logging** : le provider console de `M.E.Logging` est asynchrone et écrit hors `Console.SetOut`
  (redirection du `IoGrader` contournée) → stdout non capturé / ordre non garanti. À résoudre :
  provider de test maison, ou capture stdout réelle dans le grader.
- **M20 Generic Host & Worker** : cycle de vie du host (long-running) + même souci de capture du logging.
- **Rush 3** (Worker complet) : `Channel<T>` + I/O réseau + `ILogger` + DI sous `HostBuilder` — entrelace
  host-lifetime, logging et réseau non déterministe.
- **Grader git dédié** (M05/M14) : noter des opérations git (branches/merge/rebase/conflits).
- **Harnais réseau** (M22) : serveur de test embarqué pour noter sockets/HttpClient de façon déterministe.
- **Grader « élève-écrit-tests »** (M13) : exécuter les tests de l'élève contre une impl correcte ET une
  impl boguée pour prouver qu'ils détectent le bug.
- `docs/superpowers/BLOCKERS-v1.0.md` : tableau de suivi de tous ces points + résolution proposée.

## Phase 4 — Finalisation
- MAJ `docs/wiki/Curriculum.md` (état d'avancement) + README si besoin.
- MAJ mémoire projet.
- **Go final demandé au proprio** avant `git tag v1.0.0` + push (déclenche `release.yml` = release publique).
