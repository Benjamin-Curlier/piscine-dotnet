# Sprint S2 — Plan de travail de l'exercice + bouton « Ouvrir » — Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps en `- [ ]`.
> **Fonction phare de l'épic.** Spec §5.3/§5.4.

**Goal:** Transformer la page d'exercice (lecture seule) en **plan de travail** : une barre d'action avec **Ouvrir** (éditeur auto-détecté / dossier / terminal intégré / terminal système) + **Vérifier**, et le **scaffolding implicite** du starter à la 1ʳᵉ ouverture (équivalent in-app de `piscine start`).

**Architecture:** Logique de lancement dans **`Piscine.App/Launch/`**, testable via une abstraction `IProcessLauncher` (on asserte la commande+args **sans spawn réel**). `WorkspaceLauncher` enrobe `ContentLocator.FindExercise` + `PiscineLayout.WorkspaceExerciseDir` + `StarterInstaller.Install` (réutilise le moteur, **aucun seam** : déjà `public static`). `EditorResolver` (pur, sonde PATH injectée) choisit l'éditeur ; surcharge via un `SettingsService` minimal (JSON dans le répertoire d'état). UI : barre d'action dans `Exercise.razor` (RCL) ; « Terminal intégré » navigue vers `/terminal?cwd=<dir>` (la page Terminal lit le `cwd`). **Invariant : moteur / `Piscine.Cli` / `grade-received` / `release.yml` intacts.**

**Décisions (rappel brainstorming) :** IDE = **auto-détection + surcharge Réglages**, repli « ouvrir le dossier » ; CLI = **terminal intégré (principal) + terminal système (secondaire)** ; scaffolding implicite à l'ouverture.

**Conventions :** commits FR + trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` ; commit ≠ push ; 0 warning ; bUnit/xUnit ; E2E skip-sans-Chromium. **Sécurité** : jamais de concaténation de commande — args en tableau (`ArgumentList`) ; ne cible que le dossier de workspace résolu.

---

## Tâche 1 : `IProcessLauncher` + `ProcessLauncher` + `LaunchSpec`

**Files:** Create `src/Piscine.App/Launch/IProcessLauncher.cs`, `src/Piscine.App/Launch/ProcessLauncher.cs`.

```csharp
// IProcessLauncher.cs
using System.Collections.Generic;
namespace Piscine.App.Launch;

/// <summary>Description d'un lancement de processus (commande + arguments, jamais concaténés).</summary>
public sealed record LaunchSpec(string FileName, IReadOnlyList<string> Arguments);

/// <summary>Abstraction du lancement de processus OS — permet d'asserter la commande sans spawn réel.</summary>
public interface IProcessLauncher
{
    /// <summary>Lance le processus détaché (best-effort). Renvoie true si démarré.</summary>
    bool Launch(LaunchSpec spec);
}
```
```csharp
// ProcessLauncher.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
namespace Piscine.App.Launch;

/// <summary>Lance réellement via <see cref="Process"/> (UseShellExecute : ouvre dossiers/éditeurs via le shell).</summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    public bool Launch(LaunchSpec spec)
    {
        try
        {
            var psi = new ProcessStartInfo(spec.FileName) { UseShellExecute = true };
            foreach (var a in spec.Arguments) psi.ArgumentList.Add(a);
            return Process.Start(psi) is not null;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return false;
        }
    }
}
```
Pas de test unitaire pour `ProcessLauncher` (side-effecting) ; l'interface est testée via ses consommateurs. Commit `feat(qol/s2): IProcessLauncher + ProcessLauncher`.

---

## Tâche 2 : `EditorResolver` (pur) + `ExecutableProbe`

**Files:** Create `src/Piscine.App/Launch/EditorResolver.cs`, `src/Piscine.App/Launch/ExecutableProbe.cs` ; Test `tests/Piscine.App.Tests/EditorResolverTests.cs`.

```csharp
// EditorResolver.cs
using System;
namespace Piscine.App.Launch;

/// <summary>Un éditeur lançable : libellé affiché + commande.</summary>
public sealed record EditorOption(string Label, string FileName);

/// <summary>
/// Choix de l'éditeur : la surcharge (Réglages) prime ; sinon 1ʳᵉ commande candidate présente dans le
/// PATH (sonde injectée → testable). null si rien (l'UI retombe sur « ouvrir le dossier »). Pur.
/// </summary>
public static class EditorResolver
{
    private static readonly (string Label, string Cmd)[] Candidates =
        [("VS Code", "code"), ("Rider", "rider"), ("Visual Studio", "devenv")];

    public static EditorOption? Resolve(string? overrideCommand, Func<string, bool> isOnPath)
    {
        if (!string.IsNullOrWhiteSpace(overrideCommand))
        {
            return new EditorOption(overrideCommand!, overrideCommand!);
        }

        foreach (var (label, cmd) in Candidates)
        {
            if (isOnPath(cmd))
            {
                return new EditorOption(label, cmd);
            }
        }
        return null;
    }
}
```
```csharp
// ExecutableProbe.cs
using System;
using System.IO;
namespace Piscine.App.Launch;

/// <summary>Teste si une commande est résoluble via le PATH (ajoute .exe/.cmd/.bat sous Windows).</summary>
public static class ExecutableProbe
{
    public static bool OnPath(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return false;
        var exts = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", ".bat", "" } : new[] { "" };
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            foreach (var ext in exts)
            {
                try { if (File.Exists(Path.Combine(dir, command + ext))) return true; }
                catch (ArgumentException) { /* dir invalide */ }
            }
        }
        return false;
    }
}
```
Tests (`EditorResolverTests`) : override prime ; 1ʳᵉ candidate présente ; null si rien. Ex :
```csharp
[Fact] public void Override_wins() => Assert.Equal("micro", EditorResolver.Resolve("micro", _ => true)!.FileName);
[Fact] public void Picks_first_on_path() => Assert.Equal("code", EditorResolver.Resolve(null, c => c == "code")!.FileName);
[Fact] public void Picks_rider_if_only_rider() => Assert.Equal("Rider", EditorResolver.Resolve(null, c => c == "rider")!.Label);
[Fact] public void Null_when_none() => Assert.Null(EditorResolver.Resolve(null, _ => false));
[Fact] public void Blank_override_ignored() => Assert.Equal("code", EditorResolver.Resolve("  ", c => c == "code")!.FileName);
```
Commit `feat(qol/s2): EditorResolver (auto-détection + surcharge)`.

---

## Tâche 3 : `WorkspaceLauncher` (scaffold + open)

**Files:** Create `src/Piscine.App/Launch/WorkspaceLauncher.cs` ; Test `tests/Piscine.App.Tests/WorkspaceLauncherTests.cs`.

`ContentLocator.FindExercise(layout.Content, exerciseId)` → `location { ModuleId, ContentDir }` (Piscine.Core) ; `layout.WorkspaceExerciseDir(moduleId, exerciseId)` ; `StarterInstaller.Install(contentDir, workspaceDir)` (Piscine.Core).

```csharp
using System;
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
namespace Piscine.App.Launch;

/// <summary>
/// Ouvre l'exercice côté recrue : résout le dossier de travail, le **scaffolde** depuis le starter à la
/// 1ʳᵉ ouverture (équivalent de `piscine start`), puis le lance (dossier / éditeur / terminal système)
/// via <see cref="IProcessLauncher"/>. Le « terminal intégré » est une navigation UI (hors de ce service).
/// </summary>
public sealed class WorkspaceLauncher(PiscineLayout layout, IProcessLauncher launcher)
{
    /// <summary>Dossier de travail de l'exo, scaffoldé si vide/absent. null si exo introuvable.</summary>
    public string? PrepareWorkspace(string exerciseId)
    {
        var loc = ContentLocator.FindExercise(layout.Content, exerciseId);
        if (loc is null) return null;
        var dir = layout.WorkspaceExerciseDir(loc.ModuleId, exerciseId);
        if (!Directory.Exists(dir) || !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            StarterInstaller.Install(loc.ContentDir, dir);
        }
        return dir;
    }

    public bool OpenFolder(string exerciseId) => Launch(exerciseId, FolderSpec);
    public bool OpenEditor(string exerciseId, EditorOption editor)
        => Launch(exerciseId, d => new LaunchSpec(editor.FileName, [d]));
    public bool OpenSystemTerminal(string exerciseId) => Launch(exerciseId, TerminalSpec);

    private bool Launch(string exerciseId, Func<string, LaunchSpec> spec)
    {
        var dir = PrepareWorkspace(exerciseId);
        return dir is not null && launcher.Launch(spec(dir));
    }

    private static LaunchSpec FolderSpec(string dir) => OperatingSystem.IsWindows()
        ? new LaunchSpec("explorer.exe", [dir])
        : new LaunchSpec("xdg-open", [dir]);

    private static LaunchSpec TerminalSpec(string dir) => OperatingSystem.IsWindows()
        ? new LaunchSpec("wt.exe", ["-d", dir])   // Windows Terminal ; best-effort
        : new LaunchSpec("x-terminal-emulator", ["--working-directory", dir]);
}
```
Tests (`WorkspaceLauncherTests`, avec un `RecordingLauncher : IProcessLauncher` qui mémorise le dernier `LaunchSpec` et renvoie true, + un `PiscineLayout` sur dossiers temp + un content fixture minimal d'un exo) :
- `PrepareWorkspace` crée le dossier + y copie le starter quand il est absent (preuve FS) ; idempotent si déjà rempli.
- `OpenFolder` → spec `explorer.exe`/`xdg-open` avec le dossier de l'exo.
- `OpenEditor(code)` → spec `code <dir>`.
- exo introuvable → false, aucun lancement.

(Construire un content fixture : créer `content/modules/NN-x/exercises/exNN-y/{manifest.yaml,starter/F.cs}` minimal dans un temp, + `PiscineLayout(content, workspace, state)`. S'inspirer des fixtures de `Piscine.App.Tests` existantes. Si `ContentLocator.FindExercise` exige un `module.yaml`, l'ajouter.)

Commit `feat(qol/s2): WorkspaceLauncher (scaffold + ouvrir dossier/éditeur/terminal)`.

---

## Tâche 4 : `SettingsService` minimal (surcharge éditeur)

**Files:** Create `src/Piscine.App/Settings/AppSettings.cs`, `src/Piscine.App/Settings/SettingsService.cs` ; Test `tests/Piscine.App.Tests/SettingsServiceTests.cs`.

JSON dans `layout` (répertoire d'état) : `{ "editorCommand": "code" }`. `SettingsService(PiscineLayout)` : `Load()` (défaut si absent/corrompu — fail-soft), `Save(AppSettings)`. Champ S2 : `EditorCommand` (string?). Tests : round-trip Save/Load ; Load défaut si fichier absent ; Load défaut (pas d'exception) si JSON corrompu. Commit `feat(qol/s2): SettingsService minimal (surcharge éditeur)`.

(Emplacement du fichier : un chemin sous le répertoire d'état de `PiscineLayout`. Vérifier l'API exacte de `PiscineLayout` pour un sous-chemin d'état — réutiliser le même dossier que `ProgressPath` mais un fichier `settings.json`.)

---

## Tâche 5 : Barre d'action de l'exercice (UI) + `/terminal?cwd=`

**Files:** Modify `src/Piscine.Components/Components/Pages/Exercise.razor` (+ `.razor.css`) ; Modify la page Terminal (`TerminalPage.razor`) pour lire `?cwd=`. DI : enregistrer `IProcessLauncher`/`WorkspaceLauncher`/`SettingsService` dans `Piscine.Desktop/Program.cs` **et** `Piscine.DevHost/Program.cs`.

- Barre d'action en tête de l'exercice (remplace l'encart « Pour démarrer : `piscine start`… ») :
  - **Ouvrir ▾** : menu → éditeur détecté (via `EditorResolver.Resolve(settings.EditorCommand, ExecutableProbe.OnPath)`, libellé dynamique ; si null → juste « Ouvrir le dossier ») · **Ouvrir le dossier** (`WorkspaceLauncher.OpenFolder`) · **Terminal intégré** (`NavigationManager` → `/terminal?cwd=<dir>`, `dir = WorkspaceLauncher.PrepareWorkspace`) · **Terminal système** (`WorkspaceLauncher.OpenSystemTerminal`).
  - **Vérifier** : lien vers `/check` (page existante).
  - `data-testid` : `ex-open`, `ex-open-folder`, `ex-open-editor`, `ex-open-terminal-embedded`, `ex-open-terminal-system`, `ex-check`.
- `TerminalPage.razor` : si `?cwd=` présent et valide (sous le workspace), démarrer le PTY dans ce dossier (sinon comportement actuel = temp isolée). Garder rétro-compat.
- Tests : bUnit sur la barre (rendu des items selon `EditorResolver` — injecter un faux launcher/settings) ; E2E : aller sur une page d'exercice, cliquer « Ouvrir le dossier » avec un `IProcessLauncher` **mocké** côté DevHost (via env/DI de test) → assert le spec capturé. (Le clic réel ne doit PAS spawn en CI : injecter un `RecordingLauncher` dans le DevHost quand une variable d'env de test est posée.)

Commit `feat(qol/s2): plan de travail de l'exercice (barre Ouvrir + Vérifier)`.

---

## Tâche 6 : Vérification finale
- [ ] `dotnet test Piscine.slnx -c Release` → vert, 0 warning.
- [ ] Smoke de rendu **local** Photino (session propre) : page d'exercice montre la barre d'action ; « Ouvrir le dossier » ouvre l'explorateur sur le dossier scaffoldé.
- [ ] `validate-content` OK.

## Notes de revue
- Spec §5.3/§5.4 : barre d'action (T5), WorkspaceLauncher + scaffold + dossier/éditeur/terminal système (T1-T3), éditeur auto-détecté + surcharge (T2/T4), terminal intégré via `/terminal?cwd=` (T5). ✔
- Invariants : `Piscine.App/{Launch,Settings}`, `Piscine.Components` (Exercise/TerminalPage), DI des hôtes, tests. Moteur/CLI/`grade-received`/`release.yml` intacts (réutilise `ContentLocator`/`StarterInstaller`/`PiscineLayout` **publics existants**). ✔
- Sécurité : args en tableau (`ArgumentList`), cible = dossier workspace résolu, jamais de concaténation. ✔
- Hors S2 : page Réglages complète (S6) ; check inline riche (déjà en S0 via `/check`).
