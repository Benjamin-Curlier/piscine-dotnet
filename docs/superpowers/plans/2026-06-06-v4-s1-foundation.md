# v4 Sprint 1 — Fondation (RCL + bi-hôte + pyramide de tests) — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended)
> or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md).
> Sprints 2→10 = un plan par sprint, rédigé au démarrage du sprint (rythme V3).

**Goal:** Extraire les composants Razor dans une RCL partagée, faire tourner les MÊMES composants
dans deux hôtes (Photino.Blazor livré + `Piscine.DevHost` Blazor Server de test), et poser la pyramide
de tests (xUnit + bUnit + Playwright) — sans toucher au moteur ni au CLI.

**Architecture:** `Piscine.Components` (RCL) consommée par `Piscine.Desktop` (Photino) et
`Piscine.DevHost` (Blazor Server, non livré). `Piscine.App` (services, vide en S1) référencé par les
deux hôtes. Le moteur (`Core`/`Grading`/`Git`) et `Piscine.Cli` restent inchangés.

**Tech Stack:** .NET 10, Blazor (RCL + Server), Photino.Blazor (sur Photino.NET v3.x), Markdig
(déjà présent dans `Piscine.Web`), xUnit, bUnit, Microsoft.Playwright.

---

## ⚠️ Spike intégré (à lever AVANT de figer le code Photino)

Avant la Task 4, **vérifier le bootstrap Photino.Blazor courant** : scaffolder via le template/paquet
officiel (`Photino.Blazor`) et confronter à la doc (CLAUDE.md : consulter la doc avant impl SDK). La
forme canonique est fournie en Task 4 comme référence ; si l'API a bougé, adapter. Critère de réussite
du spike : **la fenêtre Photino s'ouvre et affiche un composant migré de la RCL**. Si le webview manque
(WebView2 Windows / `libwebkit2gtk` Linux), noter la procédure de setup (alimente la doc S9).

## Carte des fichiers

- Créer : `src/Piscine.Components/Piscine.Components.csproj` (RCL) + composants déplacés depuis `Piscine.Web`
- Créer : `src/Piscine.App/Piscine.App.csproj` (squelette services) ; `Piscine.App/Models/` (vide en S1)
- Créer : `src/Piscine.Desktop/Piscine.Desktop.csproj` + `Program.cs` + `wwwroot/index.html` + `App.razor`
- Renommer : `src/Piscine.Web` → `src/Piscine.DevHost` (projet de test/dev, NON livré)
- Créer : `tests/Piscine.App.Tests/`, `tests/Piscine.Components.Tests/` (bUnit), `tests/Piscine.DevHost.E2E/` (Playwright)
- Modifier : `Piscine.slnx` (ajouter/retirer projets) ; **ne pas** toucher `release.yml` (packaging = S9)

---

### Task 1 : RCL `Piscine.Components` + migration des composants

**Files:**
- Create: `src/Piscine.Components/Piscine.Components.csproj`
- Modify: déplacer les `.razor` / services de rendu Markdig depuis `src/Piscine.Web/` vers `src/Piscine.Components/`

- [ ] **Step 1 — Créer la RCL**

```bash
dotnet new razorclasslib -o src/Piscine.Components -f net10.0
dotnet sln Piscine.slnx add src/Piscine.Components/Piscine.Components.csproj   # slnx : sinon éditer à la main
```

- [ ] **Step 2 — Référencer le cœur + Markdig** dans `src/Piscine.Components/Piscine.Components.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Piscine.Core\Piscine.Core.csproj" />
  <PackageReference Include="Markdig" Version="0.37.0" /> <!-- aligner sur la version déjà utilisée par Piscine.Web -->
</ItemGroup>
```

- [ ] **Step 3 — Déplacer les composants** : `git mv` des composants de cours/sujet/exercice et du
  service de rendu (`CourseCatalog`, rendu Markdig, coloration) de `src/Piscine.Web/` vers
  `src/Piscine.Components/` (préserver l'historique). Corriger les `@namespace`/usings.

- [ ] **Step 4 — Build**

Run: `dotnet build src/Piscine.Components/Piscine.Components.csproj -c Release`
Expected: build réussi, 0 warning (WarningsAsErrors actif).

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): RCL Piscine.Components (composants migres depuis Piscine.Web)"
```

### Task 2 : Recycler `Piscine.Web` → `Piscine.DevHost`

**Files:**
- Rename: `src/Piscine.Web/` → `src/Piscine.DevHost/` (projet, AssemblyName, RootNamespace)
- Modify: `Piscine.DevHost` consomme `Piscine.Components` ; marquer non-livré

- [ ] **Step 1 — Renommer** le dossier/projet (`git mv src/Piscine.Web src/Piscine.DevHost`), renommer
  `Piscine.Web.csproj` → `Piscine.DevHost.csproj`, mettre à jour `AssemblyName`/`RootNamespace` et la slnx.

- [ ] **Step 2 — Référencer la RCL** et retirer les composants désormais dans la RCL :

```xml
<ItemGroup>
  <ProjectReference Include="..\Piscine.Components\Piscine.Components.csproj" />
</ItemGroup>
```

- [ ] **Step 3 — Lancer le DevHost**

Run: `dotnet run --project src/Piscine.DevHost`
Expected: démarre sur http://localhost:5244, une page rendant un composant migré (un cours) s'affiche.

- [ ] **Step 4 — Vérif visuelle (outils preview)** : `preview_start` sur le DevHost, `preview_snapshot`
  → confirmer qu'un cours markdown se rend (titres, code colorisé). `preview_screenshot` comme preuve.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "refactor(v4): Piscine.Web -> Piscine.DevHost (harnais Blazor Server, hors release)"
```

### Task 3 : Squelette `Piscine.App` + projet de tests xUnit

**Files:**
- Create: `src/Piscine.App/Piscine.App.csproj`, `tests/Piscine.App.Tests/Piscine.App.Tests.csproj`
- Test: `tests/Piscine.App.Tests/SanityTests.cs`

- [ ] **Step 1 — Créer le projet de services (vide en S1, rempli en S3+)**

```bash
dotnet new classlib -o src/Piscine.App -f net10.0
dotnet sln Piscine.slnx add src/Piscine.App/Piscine.App.csproj
```

Références dans `src/Piscine.App/Piscine.App.csproj` :

```xml
<ItemGroup>
  <ProjectReference Include="..\Piscine.Core\Piscine.Core.csproj" />
  <ProjectReference Include="..\Piscine.Grading\Piscine.Grading.csproj" />
  <ProjectReference Include="..\Piscine.Git\Piscine.Git.csproj" />
</ItemGroup>
```

- [ ] **Step 2 — Écrire le test (établit le projet de tests)** `tests/Piscine.App.Tests/SanityTests.cs`

```csharp
namespace Piscine.App.Tests;

public class SanityTests
{
    [Fact]
    public void App_assembly_references_the_engine()
    {
        // Garde-fou : l'assembly App charge bien Piscine.Core (modèles moteur accessibles).
        var coreLoaded = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(a => a.GetName().Name == "Piscine.Core")
            || typeof(Piscine.App.AppMarker).Assembly is not null;
        Assert.True(coreLoaded);
    }
}
```

- [ ] **Step 3 — Ajouter le marqueur** `src/Piscine.App/AppMarker.cs`

```csharp
namespace Piscine.App;

/// <summary>Type marqueur de l'assembly Piscine.App (point d'ancrage des tests et du DI).</summary>
public sealed class AppMarker;
```

- [ ] **Step 4 — Créer le projet de tests + références**

```bash
dotnet new xunit -o tests/Piscine.App.Tests -f net10.0
dotnet sln Piscine.slnx add tests/Piscine.App.Tests/Piscine.App.Tests.csproj
```

Dans `tests/Piscine.App.Tests/Piscine.App.Tests.csproj` : `<ProjectReference Include="..\..\src\Piscine.App\Piscine.App.csproj" />`.

- [ ] **Step 5 — Exécuter**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: PASS (1 test).

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): squelette Piscine.App + projet de tests xUnit"
```

### Task 4 : Hôte Photino `Piscine.Desktop` (spike + rendu d'un composant)

**Files:**
- Create: `src/Piscine.Desktop/Piscine.Desktop.csproj`, `Program.cs`, `App.razor`, `wwwroot/index.html`

> ⚠️ **Vérifier le bootstrap contre le template/paquet `Photino.Blazor` courant avant de figer.** La
> forme ci-dessous est la référence canonique ; Photino.NET est en v3.x et embarque les libs natives
> par RID (`runtimes/<rid>/native/` — `WebView2Loader.dll` Windows, `Photino.Native.so` Linux,
> `.dylib` macOS).

- [ ] **Step 1 — Créer le projet** (référence la RCL + App)

`src/Piscine.Desktop/Piscine.Desktop.csproj` :

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Photino.Blazor" Version="*" /> <!-- figer la version exacte au spike -->
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Piscine.Components\Piscine.Components.csproj" />
    <ProjectReference Include="..\Piscine.App\Piscine.App.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="wwwroot\**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2 — `wwwroot/index.html`** (point de montage Blazor)

```html
<!DOCTYPE html>
<html><head><meta charset="utf-8" /><base href="/" /></head>
<body><div id="app">Chargement…</div><script src="_framework/blazor.webview.js"></script></body></html>
```

- [ ] **Step 3 — `App.razor`** (monte un composant existant de la RCL — ex. le lecteur de cours)

```razor
@using Piscine.Components
<CourseList />  @* composant migré en Task 1 ; remplacer par le nom réel si différent *@
```

- [ ] **Step 4 — `Program.cs`** (forme canonique Photino.Blazor — à valider au spike)

```csharp
using Photino.Blazor;

var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
builder.RootComponents.Add<Piscine.Desktop.App>("#app");
// builder.Services.Add… (DI des services Piscine.App : rempli en S3+)

var app = builder.Build();
app.MainWindow
   .SetTitle("Piscine .NET")
   .SetUseOsDefaultSize(true);

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    app.MainWindow.ShowMessage("Erreur fatale", e.ExceptionObject.ToString());

app.Run();
```

- [ ] **Step 5 — Spike : lancer la fenêtre**

Run: `dotnet run --project src/Piscine.Desktop`
Expected: une fenêtre native s'ouvre et **affiche le composant migré**. (Je ne peux pas voir la
fenêtre native : valider visuellement à la main + confirmer l'absence d'erreur dans les logs.)
Si le webview manque → noter la procédure d'install (alimente S9).

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): hote Photino Piscine.Desktop rend un composant de la RCL (spike OK)"
```

### Task 5 : Tests de composants bUnit

**Files:**
- Create: `tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj`
- Test: `tests/Piscine.Components.Tests/CourseRenderTests.cs`

- [ ] **Step 1 — Créer le projet bUnit**

```bash
dotnet new xunit -o tests/Piscine.Components.Tests -f net10.0
dotnet sln Piscine.slnx add tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj
```

Ajouter dans le `.csproj` : `<PackageReference Include="bunit" Version="1.31.3" />` (figer la version
courante au moment de l'impl) + `<ProjectReference Include="..\..\src\Piscine.Components\Piscine.Components.csproj" />`.

- [ ] **Step 2 — Écrire le test (échoue d'abord si le composant n'existe pas sous ce nom)**
  `tests/Piscine.Components.Tests/CourseRenderTests.cs`

```csharp
using Bunit;
using Xunit;

namespace Piscine.Components.Tests;

public class CourseRenderTests : TestContext
{
    [Fact]
    public void Renders_markdown_heading_as_h1()
    {
        // Adapter le nom du composant + paramètre au composant réel migré en Task 1.
        var cut = RenderComponent<Piscine.Components.MarkdownView>(p => p
            .Add(c => c.Markdown, "# Titre"));

        cut.Find("h1");                       // ne lève pas → un <h1> est présent
        Assert.Contains("Titre", cut.Markup);
    }
}
```

- [ ] **Step 3 — Exécuter**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: PASS (1 test). Si le composant a un autre nom/param, ajuster le test au composant réel.

- [ ] **Step 4 — Commit**

```bash
git add -A
git commit -m "test(v4): bUnit rend le markdown d'un cours (h1)"
```

### Task 6 : E2E Playwright sur `Piscine.DevHost`

**Files:**
- Create: `tests/Piscine.DevHost.E2E/Piscine.DevHost.E2E.csproj`
- Test: `tests/Piscine.DevHost.E2E/SmokeTests.cs`

- [ ] **Step 1 — Créer le projet + Playwright**

```bash
dotnet new xunit -o tests/Piscine.DevHost.E2E -f net10.0
dotnet sln Piscine.slnx add tests/Piscine.DevHost.E2E/Piscine.DevHost.E2E.csproj
# Ajouter <PackageReference Include="Microsoft.Playwright" Version="1.47.0" /> puis :
dotnet build tests/Piscine.DevHost.E2E -c Release
pwsh tests/Piscine.DevHost.E2E/bin/Release/net10.0/playwright.ps1 install chromium
```

- [ ] **Step 2 — Écrire le smoke E2E** `tests/Piscine.DevHost.E2E/SmokeTests.cs`

```csharp
using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

public class SmokeTests : IAsyncLifetime
{
    private Process _host = null!;

    public async Task InitializeAsync()
    {
        _host = Process.Start(new ProcessStartInfo("dotnet",
            "run --project src/Piscine.DevHost --urls http://localhost:5247")
            { UseShellExecute = false })!;
        await Task.Delay(4000); // laisser Kestrel démarrer (remplacer par un poll de /health en S5)
    }

    public Task DisposeAsync() { _host.Kill(true); return Task.CompletedTask; }

    [Fact]
    public async Task DevHost_renders_a_course()
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://localhost:5247");
        await page.WaitForSelectorAsync("h1"); // un cours rendu expose au moins un titre
        Assert.Contains("Piscine", await page.TitleAsync());
    }
}
```

- [ ] **Step 3 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (1 test). Vérif locale par moi possible aussi via `preview_*` sur le DevHost.

- [ ] **Step 4 — Commit**

```bash
git add -A
git commit -m "test(v4): smoke E2E Playwright (DevHost rend un cours)"
```

### Task 7 : Vérification globale + slnx + PR

- [ ] **Step 1 — Build + tests complets**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build 0 warning ; **tous** les tests verts (164 existants + nouveaux ; les E2E peuvent être
exclus de la run unitaire et câblés en CI en S9 si trop lents).

- [ ] **Step 2 — Garde-fous** : `release.yml` **inchangé** (packaging Desktop = S9) ;
  `Piscine.DevHost` **absent** de tout chemin de release ; `Piscine.Cli` intact.

- [ ] **Step 3 — Valider le contenu (non régressé)**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR**

```bash
git push -u origin v4/s1-foundation
gh pr create --base main --title "v4 S1 — fondation (RCL + bi-hote + pyramide de tests)" --body "Implémente le sprint 1 du backlog v4 (issue dédiée). Spec: docs/superpowers/specs/2026-06-06-v4-photino-desktop-design.md"
```

---

## Self-review (couverture S1 vs spec §4/§7)

- RCL `Piscine.Components` ✅ T1 · `Piscine.App` ✅ T3 · `Piscine.Desktop` Photino ✅ T4 ·
  `Piscine.DevHost` ✅ T2.
- Pyramide : xUnit ✅ T3 · bUnit ✅ T5 · Playwright E2E ✅ T6 · smoke Photino manuel ✅ T4 Step 5.
- Invariant « moteur/CLI inchangés » ✅ T7 Step 2-3. Packaging/coaching/terminal/check = **hors S1**
  (sprints 2-9), volontairement.
- Versions de paquets (Photino.Blazor, bUnit, Playwright, Markdig) à **figer au moment de l'impl**
  contre la doc courante — marqué explicitement, pas un placeholder de logique.
