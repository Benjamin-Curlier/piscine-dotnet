# Retex — Migration PhotinoX.Blazor 4.2.0 (release v3.1.0)

> Sprint du 2026-06-08. Branche `feat/photinox-migration` → PR [#55](https://github.com/Benjamin-Curlier/piscine-dotnet/pull/55).
> Spec : [2026-06-08-photinox-migration-design.md](../specs/2026-06-08-photinox-migration-design.md) ;
> plan : [2026-06-08-photinox-migration.md](../plans/2026-06-08-photinox-migration.md) ;
> ADR : [2026-06-08-photinox-fork.md](../adr/2026-06-08-photinox-fork.md).

## Objectif

Migrer `Piscine.Desktop` de `Photino.Blazor 3.2.0` vers le fork maintenu `PhotinoX.Blazor 4.2.0`
(net10-natif), puis publier **v3.1.0** via le flux CI standard.

## Ce qui a marché

- **Drop-in API confirmé** : PhotinoX conserve `namespace Photino.Blazor` + `PhotinoBlazorAppBuilder`.
  Build vert sans toucher `Program.cs` côté API. **Épingle WebView NU1605 supprimée** : le fork aligne
  `Microsoft.AspNetCore.Components.WebView` sur 10.0.x → build `0/0` sous `TreatWarningsAsErrors`.
- **Noms natifs identifiés en amont** par inspection du nupkg (`PhotinoX.Native.{dll,so}`,
  `WebView2Loader.dll` conservé) → assertions CI/docs corrigées sans deviner. Publish réel win-x64/linux-x64 :
  libs **à la racine** de `desktop/`, confirmé.
- **Fenêtre native PhotinoX lancée sur Windows** : titre/taille appliqués, app Blazor chargée, 0 exception.
- CI : `build-test` (267 tests + assertions libs natives) et `windows-installer-dryrun` **verts** dès la 1ʳᵉ PR.

## Surprises / écarts

- **1 ligne de code nécessaire** : PhotinoX annote `MainWindow.ShowMessage(text)` en **non-nullable** →
  `CS8604` sous `TreatWarningsAsErrors` sur `ExceptionObject.ToString()` (`string?`). Fix : fallback
  `?? "Exception inconnue"`.
- **AppImage offline non viable** (cause racine, vérifiée dans la source WebKit `findWebKitProcess`) :
  WebKitGTK en **build release** ne lit `WEBKIT_EXEC_PATH` que sous `ENABLE(DEVELOPER_MODE)` et résout
  `WebKitNetworkProcess`/`WebKitWebProcess` via le **`PKGLIBEXECDIR` absolu compilé**. Le webkit système
  d'Ubuntu étant un build release, l'AppImage **ne peut pas** embarquer un webkit fonctionnel sans
  bind-mount privilégié. **Décision proprio** : abandonner l'offline, publier l'**AppImage online**
  (webkit système). Constat rétroactif : l'ancien dry-run offline (v3.0.0/4.0) ne vérifiait que la ligne
  `Load(...)`, pas le rendu → l'offline ne rendait en réalité que là où le webkit système existait déjà.
- **Test `Grade_BareRepo_WithoutHeadRef_FailsMinCommits` rouge en local** : **pré-existant** (prouvé
  identique sur `main` pristine), dépendant de `init.defaultBranch=main` du poste — sans rapport avec la
  migration ; vert en CI. Suivi : tâche de correction de l'isolation du test.

## Résultat

- Release **v3.1.0**, **5 artefacts** : zips win/linux, installeurs Windows offline/online, AppImage Linux
  online. `ci.yml` : `appimage-offline-dryrun` → `appimage-online-dryrun` (garde de build).
- Suivi ouvert : **true-offline** Linux (AppRun bwrap/bind-mount montant les helpers webkit sur le chemin
  attendu) ; **isolation** du test git env-dépendant.
