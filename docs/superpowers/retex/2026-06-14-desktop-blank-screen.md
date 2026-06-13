# RÉSOLU — l'app de bureau Photino rendait un ÉCRAN NOIR (cause : MTA + IConfiguration manquant)

> 2026-06-14. Déclencheur : smoke proprio de S0 → `dotnet run --project src/Piscine.Desktop` affichait
> une **fenêtre entièrement noire**. **CORRIGÉ** (2 bugs masqués) + **harnais de smoke de rendu ajouté**.

## Symptôme

Fenêtre native ouverte, vivante, **0 exception côté hôte** — zone WebView2 **100 % noire** (pas même le
`#app` « Chargement… »).

## Cause racine RÉELLE (2 bugs, le 1er masquait le 2nd)

1. **Thread principal MTA.** `Program.cs` utilisait des **top-level statements** → le `Main` synthétisé
   n'a **pas** `[STAThread]` → thread principal en **MTA**. Or **WebView2 (Blazor Hybrid) repose sur COM
   et exige un thread STA** : en MTA, la fenêtre Win32 s'ouvre mais la **webview ne rend rien** (noir),
   et **aucun web message** ne circule (Blazor ne démarre jamais).
2. **`IConfiguration` non enregistré.** Une fois le rendu débloqué (STA), Blazor a jeté
   *« Unable to resolve service for type `IConfiguration` while activating `CourseCatalog`. »* :
   `PhotinoBlazorAppBuilder` **n'enregistre pas** d'`IConfiguration` (contrairement à `WebApplication`
   du DevHost), alors que `CourseCatalog`/`ContentRootResolver` en dépendent. → render exception →
   `#app` restait sur « Chargement… ».

## Le correctif (`src/Piscine.Desktop/Program.cs`)

- **`[STAThread] static void Main`** explicite (fin des top-level statements) — comme tout hôte
  Photino/WebView2 correct.
- **Enregistrer une `IConfiguration`** basée sur les variables d'environnement :
  `builder.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build())`.

**Prouvé** : `PISCINE_SMOKE=1` → `{"received":true,"h1":"Tableau de bord","dashboard":true,"navTabs":7,"statusDots":170}`
+ confirmation visuelle proprio (« it renders »). Le tableau de bord, les onglets et la sidebar à pastilles s'affichent.

## Comment on l'a trouvé (et ce qui était FAUX)

- Repro minimal PhotinoX nu → même écran noir → **ni notre code (S0/CSP/Router/RCL), ni le fork**.
- **Comparaison avec un projet PhotinoX qui marche** (fourni par le proprio) → seule différence
  significative : **`[STAThread] static void Main`** (vs nos top-level statements). Ajout de `[STAThread]`
  au repro → **rendu OK**, ce qui a révélé le 2nd bug (`IConfiguration`).
- **Hypothèses ÉCARTÉES (red herrings)** : la log `Load(/)` → `File "/" could not be found` →
  `Load(http://localhost/)` est **normale** (présente aussi quand ça marche) ; le service de
  `http://localhost/` par WebView2 **fonctionnait** ; **WebView2 149 / loopback, Cloudflare WARP,
  `localhost`→`::1`, Chrome installé ou non** = **sans rapport**. La piste « régression WebView2 » était
  erronée — corrigée par la comparaison au projet qui marche.

## Harnais de smoke de RENDU ajouté (la demande initiale)

- `src/Piscine.Desktop/SmokeProbe.cs` + beacon `wwwroot/index.html` : en `PISCINE_SMOKE=1`, la page
  renvoie par web message le **bilan du DOM réellement rendu** ; la sonde l'écrit (`PISCINE_SMOKE_OUT`)
  puis termine. **Prouve le rendu** (pas juste « process vivant ») — ce qui aurait attrapé ce bug d'emblée.
- `tests/Piscine.DevHost.E2E/DesktopRenderSmokeTests.cs` : test **opt-in** (`PISCINE_DESKTOP_SMOKE=1`)
  qui lance l'app en mode sonde et **asserte un rendu non vide**. **Vert maintenant** (était le test rouge
  qui reproduisait le bug). Skip par défaut → CI verte. Lancer à la main :
  `PISCINE_SMOKE=1 PISCINE_SMOKE_OUT=/tmp/s.json PISCINE_CONTENT="$PWD/content" dotnet run --project src/Piscine.Desktop -c Release` puis lire le JSON.

## Leçon

Le smoke historique « la fenêtre se lance + 0 exception » **ne prouvait pas le rendu** (le retex migration
PhotinoX l'admettait déjà). Le harnais de rendu comble ce trou. **Moteur/CLI/notation jamais affectés** —
seul l'hôte Photino l'était.
