# Retex — Isolation de l'exécution recrue en processus enfant jetable

> Sprint du 2026-06-09. Branche `fix/review-hardening-followups`.
> Spec : [2026-06-09-grading-sandbox-isolation-design.md](../specs/2026-06-09-grading-sandbox-isolation-design.md) ;
> plan : [2026-06-09-grading-sandbox-isolation.md](../plans/2026-06-09-grading-sandbox-isolation.md).

## Objectif

Rendre une soumission qui dépasse le délai **réellement terminable** : exécuter tout code recrue non
fiable dans un **processus enfant jetable** (`Piscine.Sandbox`) que le parent tue (arbre complet) au
timeout. Supprime les fuites de thread/assembly et la corruption de sortie inter-exécutions du modèle
in-process (`Task.Run` + `task.Wait` jamais annulé).

## Ce qui a marché

- **Frontière de processus = isolation.** `ProgramRunner`/`XunitRunner` deviennent des clients qui
  écrivent `asm.dll` + `request.json` dans un dossier temp, lancent le bac à sable, attendent avec
  timeout, **tuent l'arbre** (`Process.Kill(entireProcessTree: true)`) au dépassement, lisent
  `result.json`, nettoient. La mort du processus récupère thread + assembly ; **aucune mutation du
  `Console` global côté parent** ⇒ contamination impossible. Les parades in-process envisagées
  (AsyncLocal/ContinueWith) ont pu être **abandonnées**.
- **Co-localisation automatique du binaire.** `Piscine.Grading` référence `Piscine.Sandbox` (exe) ;
  MSBuild copie l'**apphost** `Piscine.Sandbox.exe` + `runtimeconfig.json` + `deps.json` dans la
  sortie de **chaque** consommateur — confirmé pour `dotnet test`, `dotnet build` DevHost, et
  `dotnet publish` CLI. Le lanceur emprunte donc le chemin apphost direct (pas de repli `dotnet exec`).
- **Dispose des fixtures** déplacé dans le bac à sable (`RunOne`, `finally`, `IDisposable` +
  `IAsyncDisposable`) ; prouvé bout-en-bout (la fixture écrit un marqueur fichier même quand le
  `[Fact]` lève).
- **TDD au bon niveau** : le test de non-contamination/dispose est rouge sur le modèle in-process,
  vert via le processus enfant. 139 tests `Piscine.Grading.Tests` verts.

## Surprises / écarts

- **`validate-content` a rattrapé 2 régressions que `dotnet test` ne voyait pas** (le code recrue
  s'exécute désormais dans le processus enfant, pas dans l'hôte de correction). 20 corrigés en échec
  → 0 après correctifs :
  1. **`JsonSerializerIsReflectionEnabledByDefault=false`** (mis pour la sûreté AOT de NOTRE contrat)
     est un commutateur **process-global** : il désactivait la sérialisation JSON par réflexion pour
     le **code recrue** (exercices `serialiser`). Retiré ; notre contrat reste en source-gen explicite
     (`SandboxJsonContext`), indépendant du commutateur.
  2. **Dépendances absentes du jeu minimal du bac à sable** (`Microsoft.Extensions.{DI,Logging,Hosting}`,
     `Microsoft.Data.Sqlite`). Le code recrue est compilé contre le TPA de l'hôte ; il faut donc passer
     ces **chemins runtime** (`CompilationService.ReferenceAssemblyPaths`) en `ReferencePaths`, résolus
     par `alc.Resolving`. Pour le **natif** (`e_sqlite3`), ajout d'un `alc.ResolvingUnmanagedDll` qui
     sonde `…/runtimes/<rid>/native/`. Leçon : un modèle out-of-process doit **transporter la clôture
     de dépendances** que l'in-process obtenait gratuitement.
- **Garde de régression ajoutée** (`SandboxResolutionTests`) : code recrue référençant un assembly
  présent sur le TPA de l'hôte mais absent du bac à sable (Roslyn) — sinon la logique de résolution
  n'est exercée que par `validate-content`, hors `dotnet test`.
- **Activité git concurrente** sur le poste pendant l'implémentation (pull `main`, commits
  parallèles, bascule de branche). Mes 2 premiers commits ont atterri sur `main` ; un `git add
  <dossier>` a aussi capté du travail tiers (`GitGrader`/`ContentValidator`). Historique linéaire et
  fonctionnel, laissé tel quel sur décision proprio. Leçon : **`git add` par chemins exacts**, jamais
  par dossier, surtout en présence d'édition concurrente.

## Résultat

- Nouveau projet `src/Piscine.Sandbox` (exe, deps : xunit.core/assert uniquement). `ProgramRunner`/
  `XunitRunner` réécrits en clients de lancement ; `RunOutcome.Error` → `RunError(TypeName, Message)`.
  Fail-closed dans `ExerciseGrader` si le bac à sable est indisponible (échec interne, pas de repli).
- Vérifié : 139 tests grading verts ; `validate-content` **Contenu valide** en dev **et depuis
  l'artefact publié** ; natif sqlite bundlé. (Modèle de co-localisation corrigé : cf. post-mortem ci-dessous.)
- **Suivi ouvert** : (a) sémantique `Environment.Exit` recrue gérée best-effort (hook `ProcessExit`) ;
  (b) si `Piscine.Sandbox` est un jour publié **trimmed/AOT**, le code recrue par réflexion (JSON)
  casserait — à documenter pour le packaging.

## Post-mortem — release v3.1.1 cassée au 1ᵉʳ tag (correction d'architecture)

**Erreur** : le 1ᵉʳ tag `v3.1.1` a été poussé après n'avoir validé qu'un `dotnet publish`
**framework-dependent** ; `release.yml` a échoué (aucune release créée) et la CI de `main` est passée
au rouge. Deux défauts, tous deux dans le chemin **self-contained** non testé :

1. **NETSDK1152** — `Piscine.Grading` référençait l'**exe** `Piscine.Sandbox` ; au publish
   `--self-contained -r <rid>` des front-ends, l'apphost/`deps.json`/`runtimeconfig.json` du sandbox
   entraient en collision (RID-agnostique vs RID-spécifique).
2. **Incompatible « sans SDK »** — le sandbox était **framework-dependent** : il n'aurait pas pu se
   lancer sur un poste recrue sans .NET installé, même build réussi.

**Correction (modèle `Piscine.GitShim`, le précédent du dépôt pour « helper exe sans SDK »)** :
- Contrat IPC extrait dans **`Piscine.Sandbox.Contracts`** (lib sans dépendances) ; `Piscine.Grading`
  s'y lie **sans référencer l'exe** → NETSDK1152 supprimé.
- `Piscine.Sandbox` publié **self-contained dans `sandbox/`** par bundle ; localisé par **`SandboxLocator`**
  (calqué sur `ShimLocator` : surcharge env → co-localisé tests → `sandbox/` packagé → build dev).
- `ProjectReference` vers l'exe conservé **uniquement** dans `Piscine.Grading.Tests` (co-localisation
  framework-dependent pour les tests ; sans impact sur le publish des front-ends).
- Gardes de packaging ajoutées dans `ci.yml` (desktop-dryrun + appimage-dryrun publient le sandbox
  self-contained et **assertent** l'apphost) — la régression aurait été attrapée avant le tag.

**Vérifié cette fois** : 305 tests verts (Release) ; **publish self-contained reproduit localement**
(CLI + sandbox), `validate-content` → **Contenu valide** depuis l'artefact self-contained (preuve
réelle « sans SDK » : les deux exe ignorent le runtime système). v3.1.1 re-coupé sur le commit corrigé.

**Leçon** : pour un livrable **sans SDK**, tout helper doit suivre le modèle gitshim (self-contained,
sous-dossier, locator) ; **valider le chemin `--self-contained`** avant tout tag, jamais seulement le
publish framework-dependent.
