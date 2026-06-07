# Plan — v5 S12 : terminal embarqué + coaching git dans Photino (+ packager GitShim)

> Issue **#45** (milestone #3 « v5 », label `v5`). Branche : `v5/s12-terminal-photino`.
> Dépendances : S2 (PTY), S3 (coaching/shim), S9 (packaging), S9b (hôte Photino câblé), S11 (mergé).
> Autorisé : **Docker** (SDK linux) pour vérifier Linux ; **2ᵉ écran/souris** pour Photino. **Aucun tag.**

## Constat (code lu, S9b/S3)
`Piscine.Components/Components/Terminal/TerminalPage.razor` marche dans `Piscine.DevHost` (E2E S3) mais
est **inatteignable/non câblé dans Photino** (S9b l'a différé). Blocages :
1. **Shim non packagé** : `TerminalPage.ResolveShimDir()` cherche `src/Piscine.GitShim/bin/<cfg>/net10.0/git[.exe]`
   en remontant jusqu'à `Piscine.slnx` → **absent du zip**.
2. **`@inject IHostEnvironment Env`** + garde `_enabled = Env.IsDevelopment()` : Photino n'enregistre pas
   `IHostEnvironment`, et la garde « dev-only » n'a pas de sens dans l'app livrée (le terminal local EST la
   fonctionnalité).
3. `Piscine.Desktop/Program.cs` n'enregistre pas `PtyService`/`CoachingService`/`ICoachingChannel`.
4. `NavMenu` ne lie pas `/terminal`.

## Décisions de conception
- **(1) Politique d'activation injectable** (retire `IHostEnvironment` de la RCL) : nouvelle classe
  `Piscine.App.Terminal.TerminalPolicy(bool Enabled)` (singleton). `TerminalPage` fait
  `@inject TerminalPolicy Policy` → `_enabled = Policy.Enabled`. **Photino** : `new TerminalPolicy(true)`.
  **DevHost** : `new TerminalPolicy(env.IsDevelopment())` (garde S3 conservée côté harnais).
- **(2) Résolution du shim multi-emplacement** : extraire un `ShimLocator.Resolve()` (dans `Piscine.App.Terminal`)
  qui tente dans l'ordre : (a) **`<AppContext.BaseDirectory>/gitshim/git[.exe]`** (app packagée), puis
  (b) le walk `Piscine.slnx` → `src/Piscine.GitShim/bin/{Release,Debug}/net10.0` (dev). `null` si absent →
  terminal nu sans coaching (dégradation S2). `TerminalPage` appelle `ShimLocator.Resolve()` (plus de
  `ResolveShimDir` privé lié à la slnx).
- **(3) Packager le shim** (`release.yml`) : publier `src/Piscine.GitShim` self-contained par RID dans
  **`$out/desktop/gitshim/`** (à côté de l'app desktop). L'exe = `git`/`git.exe` (AssemblyName=git). Le vrai
  git : Windows = MinGit (`$out/mingit/cmd/git.exe`, déjà mis sur PATH par `start-piscine-desktop.cmd`) ;
  Linux/macOS = git système. `GitPathResolver` (S3) résout le vrai git hors du dossier shim.
- **(4) DI Photino** : enregistrer `PtyService`, `CoachingService`, `ICoachingChannel = NamedPipeCoachingChannel`,
  `TerminalPolicy(true)` (`GitStatusService` déjà enregistré en S9b). **NavMenu** : ajouter le lien `/terminal`
  (visible dans les deux hôtes ; la page affiche l'avertissement « désactivé » si la policy est off — DevHost
  hors Development).
- **Sécurité** : terminal Photino = shell OS **local, in-process, sans réseau** → activer est sûr. cwd temp
  isolée par session conservée. (Le risque S2/S3 « shell distant sans auth » concernait le harnais SignalR.)

## Garde-fous
- Modifs : `src/Piscine.Components` (TerminalPage), `src/Piscine.App` (TerminalPolicy, ShimLocator),
  `src/Piscine.Desktop` (DI), `src/Piscine.DevHost` (DI policy), `.github/workflows/release.yml` (shim),
  `tests/**`, `docs/**`. **NE PAS** toucher la **logique de notation** (Grading) ni le **CLI** `piscine`.
- Build **0 warning** ; tests verts (DevHost E2E coaching **toujours vert** = non-régression S3) ; **aucun tag**.

## Carte des fichiers
| Fichier | Action |
|---|---|
| `src/Piscine.App/Terminal/TerminalPolicy.cs` (neuf) | T1 — `sealed class TerminalPolicy(bool Enabled)` |
| `src/Piscine.App/Terminal/ShimLocator.cs` (neuf) | T1 — résolution packagé→dev du dossier shim |
| `src/Piscine.Components/Components/Terminal/TerminalPage.razor` | T2 — `@inject TerminalPolicy` (au lieu d'`IHostEnvironment`) + `ShimLocator.Resolve()` |
| `src/Piscine.DevHost/Program.cs` | T2 — enregistrer `TerminalPolicy(env.IsDevelopment())` |
| `src/Piscine.Desktop/Program.cs` | T3 — DI PtyService/Coaching/Channel/`TerminalPolicy(true)` |
| `src/Piscine.Components/Components/Layout/NavMenu.razor` | T3 — lien `/terminal` |
| `.github/workflows/release.yml` | T4 — publier GitShim par RID dans `desktop/gitshim/` |
| `tests/**` | T5 — unit (ShimLocator, TerminalPolicy) ; non-régression DevHost E2E coaching |
| (Docker) | T5 — **vérif Linux** : PtyService + coaching IPC dans `dotnet/sdk:10.0` |

---

## T1 — TerminalPolicy + ShimLocator (App)
- [ ] `TerminalPolicy.cs` : `public sealed class TerminalPolicy(bool enabled) { public bool Enabled => enabled; }`.
- [ ] `ShimLocator.cs` : `public static string? Resolve()` — (a) `Path.Combine(AppContext.BaseDirectory, "gitshim")`
  si `git[.exe]` y est ; sinon (b) walk `Piscine.slnx` → `src/Piscine.GitShim/bin/{Release,Debug}/net10.0`
  (logique reprise de l'ancien `ResolveShimDir`). Unit tests (T5).
- [ ] Build App → 0 warning. Commit : `feat(app): TerminalPolicy + ShimLocator (shim packagé ou dev)`

## T2 — TerminalPage agnostique de l'hôte (Components + DevHost)
- [ ] `TerminalPage.razor` : retirer `@using Microsoft.Extensions.Hosting` + `@inject IHostEnvironment Env` ;
  ajouter `@inject Piscine.App.Terminal.TerminalPolicy Policy` ; `OnInitialized` → `_enabled = Policy.Enabled` ;
  remplacer l'appel `ResolveShimDir()` par `ShimLocator.Resolve()` et **supprimer** la méthode privée.
- [ ] `DevHost/Program.cs` : `builder.Services.AddSingleton(new TerminalPolicy(builder.Environment.IsDevelopment()));`
- [ ] Build + **DevHost E2E coaching** (`CoachingSmokeTests`) **vert** (non-régression S3). Commit :
  `refactor(terminal): activation par TerminalPolicy + shim via ShimLocator (hôte-agnostique)`

## T3 — Câbler le terminal dans Photino (Desktop + NavMenu)
- [ ] `Piscine.Desktop/Program.cs` : enregistrer `PtyService`, `CoachingService`,
  `ICoachingChannel`=`NamedPipeCoachingChannel`, `new TerminalPolicy(true)` (singletons ; `GitStatusService`
  déjà là). `using Piscine.App.Terminal; using Piscine.App.Coaching; using Piscine.App.Git;`.
- [ ] `NavMenu.razor` : ajouter un lien `/terminal` (`data-testid="nav-terminal"`).
- [ ] Build + **smoke Photino** (timeout ~18 s, `PISCINE_CONTENT`) : 0 crash. Commit :
  `feat(desktop): cabler le terminal embarqué + coaching dans l'hôte Photino`

## T4 — Packager le shim (release.yml)
- [ ] Dans la boucle RID, après le publish desktop : `dotnet publish src/Piscine.GitShim --configuration Release
  -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$out/desktop/gitshim"`.
- [ ] Vérif statique `git diff` ; (option) étendre le **dry-run CI** (`ci.yml`) pour asserter `desktop/gitshim/git[.exe]`.
  Commit : `build(release): empaqueter Piscine.GitShim (git shim) a cote de l'app desktop`

## T5 — Vérification (Windows + **Linux via Docker**) + retex + PR
- [ ] `dotnet build Piscine.slnx -c Release` 0 warning ; `dotnet test Piscine.slnx -c Release` verts (+ nouveaux) ;
  `validate-content` OK.
- [ ] **Linux (Docker)** — lève les checklists proprio Linux S2/S3 :
  ```bash
  docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
    bash -lc "apt-get update -qq && apt-get install -y -qq git >/dev/null && \
              dotnet test tests/Piscine.App.Tests -c Release"
  ```
  → PtyService (forkpty) + coaching IPC (named pipe = socket Unix) + shim **verts sur Linux**. (Les E2E
  Playwright se sautent sans chromium — OK.) Documenter le résultat dans le retex.
- [ ] **Garde** : `git diff --name-only origin/main...HEAD -- src/Piscine.Grading src/Piscine.Cli src/Piscine.Core`
  — Grading/CLI/Core intacts (S12 ne touche que App/Components/Desktop/DevHost/release.yml).
- [ ] Retex (décisions policy/shim, **preuve Linux Docker**, checklist smoke proprio : Windows terminal+coaching
  visuels, Linux idem, macOS proprio) + PR (push/create séparés) → CI verte → squash-merge `Fixes #45` → consigner.

Expected : l'app de bureau packagée a un **terminal embarqué fonctionnel + coaching git** (Windows prouvé ;
**Linux prouvé via Docker**) ; shim livré dans le zip ; Grading/CLI intacts ; aucun tag. Débloque la doc S14.

## Self-review (vs #45)
- Shim packagé ✅ T4 ; résolution packagé→dev ✅ T1 ; garde `IHostEnvironment` remplacée par policy ✅ T1/T2 ;
  DI Photino + NavMenu ✅ T3 ; **Linux confirmé (Docker)** ✅ T5 ; macOS = proprio ; Grading/CLI intacts ✅.
- Pas de gold-plating : pas d'installeur (S13), pas de coalescence PTY (suivi), pas de nouvelles pages.
