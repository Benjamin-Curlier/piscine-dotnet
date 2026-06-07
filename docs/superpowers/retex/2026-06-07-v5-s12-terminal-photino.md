# Retex — v5 S12 : terminal embarqué + coaching git dans Photino (+ packager GitShim)

> Issue #45. Branche `v5/s12-terminal-photino`. Plan : [../plans/2026-06-07-v5-s12-terminal-photino.md](../plans/2026-06-07-v5-s12-terminal-photino.md).
> **Verdict : objectif atteint.** L'app de bureau packagée a désormais un **terminal embarqué + coaching
> git** ; le `GitShim` est livré dans le zip. **Linux PROUVÉ via Docker** (PTY forkpty + coaching
> named-pipe/socket Unix + shim) → lève les checklists proprio S2/S3. Build 0 warning ; **257 tests** ;
> Grading/CLI/Core intacts ; aucun tag.

## Ce qui a été fait
- **`TerminalPolicy(bool Enabled)`** (`Piscine.App.Terminal`) remplace la garde `IHostEnvironment` codée
  dans la page RCL (que Photino ne pouvait pas satisfaire) : **Photino** `TerminalPolicy(true)`,
  **DevHost** `TerminalPolicy(IsDevelopment())`.
- **`ShimLocator.Resolve()`** : cherche `<exe>/gitshim/git[.exe]` (app packagée) **puis** la sortie de
  build dev (walk `Piscine.slnx`). Surcharge `Resolve(baseDir)` testable.
- **`TerminalPage`** : `@inject TerminalPolicy` + `ShimLocator.Resolve()` (méthode privée `ResolveShimDir`
  supprimée, `IHostEnvironment`/`Microsoft.Extensions.Hosting` retirés). **Hôte-agnostique.**
- **Photino `Program.cs`** : enregistre `PtyService`, `CoachingService`, `ICoachingChannel`
  (`NamedPipeCoachingChannel`), `TerminalPolicy(true)`. **NavMenu** : lien `/terminal`.
- **`release.yml`** : `dotnet publish src/Piscine.GitShim` self-contained par RID dans `desktop/gitshim/`.

## Prouvé (automatique)
- **Linux (Docker `mcr.microsoft.com/dotnet/sdk:10.0`)** : `dotnet test tests/Piscine.App.Tests -c Release`
  **57/57 verts** sur `Linux …WSL2 x86_64` → **PtyService (forkpty)**, **coaching IPC named-pipe (socket
  domaine Unix)**, **shim**, `GitStatusService`/`GitPathResolver`/règles coaching, `ShimLocator`/`TerminalPolicy`,
  `ProgressFileWatcher`, `CheckService` — tous OK sur Linux. **Méthode** : `git archive HEAD` (fichiers
  suivis seulement, pas de bin/obj) → tar extrait dans le conteneur → build+test **isolés** (rien écrit sur
  l'hôte ; mount `:ro`).
- **Windows** : build 0 warning ; **257 tests** (Core 46 + Components 25 + Git 9 + App 57 + Grading 111 +
  E2E 9) ; DevHost E2E **coaching vert** (non-régression de la bascule `TerminalPolicy`/`ShimLocator`) ;
  smoke Photino (0 crash, DI terminal résolue).
- `ShimLocator` (+2) priorité packagé / null si absent ; `TerminalPolicy` (+2). `validate-content` OK ;
  garde `git diff origin/main...HEAD -- src/Piscine.Grading src/Piscine.Cli src/Piscine.Core` = **vide**.

## Checklist smoke par OS — **proprio** (la fenêtre native)
- [ ] **Windows** : `start-piscine-desktop.cmd` → page **Terminal** → taper `git init` puis `git commit`
  rien stagé → la **carte de coaching** apparaît (shim packagé `desktop/gitshim` + MinGit sur PATH).
- [ ] **Linux** : `./start-piscine-desktop.sh` → page Terminal → idem (git système). *(PTY+coaching déjà
  prouvés en Docker ; reste à confirmer le rendu dans la fenêtre native.)*
- [ ] **macOS** : idem (git système ; pas de runner macOS → proprio).

## Décisions / limites
- **Sécurité** : le terminal Photino est un shell **local in-process, sans réseau** → activer est sûr
  (le risque S2/S3 « shell distant sans auth » concernait le harnais SignalR ; la garde dev-only y reste).
- **NavMenu lie `/terminal` dans les deux hôtes** : en DevHost hors Development, la page affiche
  l'avertissement « désactivé » (policy off). Acceptable.
- **Vrai git du shim** : Windows = MinGit (`desktop/../mingit/cmd`, mis sur PATH par le lanceur) ;
  Linux/macOS = git système. `GitPathResolver` exclut le dossier shim (pas de récursion).
- Coalescence de la sortie PTY verbeuse : toujours en suivi (non bloquant).

## Pièges réutilisables
- **Tester du natif Linux depuis Windows sans WSL général** : Docker Desktop + `git archive HEAD | tar -x`
  dans `dotnet/sdk:10.0`, build/test **dans le conteneur** (jamais bind-mount le repo en écriture → évite
  d'écraser les bin/obj Windows et les soucis d'ownership). `git archive` exclut nativement bin/obj
  (fichiers non suivis). Mount `artifacts/*.tar` en `:ro`. (`MSYS_NO_PATHCONV=1` pour ne pas mangler les
  chemins conteneur sous Git Bash.)
- **Garde d'hôte dans une RCL partagée** : ne pas injecter `IHostEnvironment` (absent des hôtes WebView) ;
  exposer une **policy injectable** que chaque hôte fournit (Web App via `IsDevelopment()`, WebView en dur).
- **Ressource liée au packaging** (shim) : un `ShimLocator` qui tente **packagé d'abord, dev ensuite** rend
  le même composant fonctionnel en dev (harnais) et en livraison (zip), sans `#if`.
