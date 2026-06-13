# Constat — l'app de bureau Photino rend un ÉCRAN NOIR (+ harnais de smoke de rendu)

> 2026-06-14. Déclencheur : smoke proprio de S0 → `dotnet run --project src/Piscine.Desktop` affiche
> une **fenêtre entièrement noire**. Investigation en débogage systématique. **Cause racine isolée,
> non encore corrigée** (décision proprio requise, cf. §Options). Un **harnais de test du rendu** a
> été ajouté (ce que la demande visait).

## Symptôme

La fenêtre native s'ouvre, reste vivante, **0 exception** — mais la zone de contenu WebView2 est
**100 % noire** (capture : aucun texte, pas même le « Chargement… » statique d'`index.html`).

## Cause racine (haute confiance)

**L'hôte PhotinoX.Blazor 4.2.0 ne sert pas le contenu Blazor sur Windows.** La webview navigue vers
`http://localhost/` (URL correcte sous Windows — PhotinoX intercepte ce host via WebView2
`WebResourceRequested`, cf. source `PhotinoWebViewManager.AppBaseUri`), **mais rien n'est servi** :
la page ne se charge pas → pas de JS → **Blazor ne démarre jamais** → écran noir.

Preuves convergentes :
- **Sonde web-message** (`SmokeProbe`) : `{"received":false}` — **zéro** web message reçu en 12 s, pas
  même l'IPC interne de Blazor (si Blazor tournait, son canal de rendu émettrait). → la page n'est pas chargée.
- **Console PhotinoX** : `Load(/)` → `** File "/" could not be found.` → `Load(http://localhost/)`, puis silence.
- `http://localhost/` ne répond pas hors webview (`curl` → HTTP 000) : c'est bien un host virtuel, pas un serveur.
- **Capture d'écran** : zone WebView2 noire, sans le `#app` « Chargement… ».

## Écarté par test à variable unique

- **Pas S0 (fondation nav)** : une page qui ne se charge JAMAIS ne peut pas dépendre du contenu RCL ;
  le DevHost (même RCL, hôte Blazor Server) rend `/`=board, `/cours`, pastilles — E2E `NavigationSmokeTests` vert.
- **Pas la CSP** (`index.html`, ajoutée en #58) : désactivée → toujours `received:false`. Restaurée.

## Pourquoi ça n'a jamais été vu

Le smoke historique « la fenêtre se lance + 0 exception » **ne vérifiait pas le rendu** — le retex de la
migration PhotinoX l'admet déjà (`2026-06-08-photinox-migration.md` : « ne vérifiait que la ligne `Load(...)`,
pas le rendu »). Le rendu PhotinoX sur Windows n'a, en réalité, jamais été confirmé visuellement.

## Harnais ajouté (la demande)

- `src/Piscine.Desktop/SmokeProbe.cs` + beacon dans `wwwroot/index.html` : en `PISCINE_SMOKE=1`, la page
  renvoie par web message un **bilan du DOM réellement rendu** (`appTextLen`, `h1`, `dashboard`, `navTabs`,
  `statusDots`) que la sonde écrit dans `PISCINE_SMOKE_OUT` puis termine le process. **Inerte hors mode smoke.**
- `tests/Piscine.DevHost.E2E/DesktopRenderSmokeTests.cs` : test **opt-in** (`PISCINE_DESKTOP_SMOKE=1`)
  qui lance l'app en mode sonde et **asserte qu'elle affiche du contenu**. Lancé aujourd'hui, il
  **échoue** (détecte l'écran noir) — c'est le test rouge qui reproduit le bug. Skip par défaut → CI verte.
- Lancer à la main : `PISCINE_SMOKE=1 PISCINE_SMOKE_OUT=/tmp/s.json PISCINE_CONTENT="$PWD/content" dotnet run --project src/Piscine.Desktop -c Release` puis lire `/tmp/s.json`.
- **Limite** : le chemin POSITIF du harnais (assert « contenu présent ») n'est pas encore prouvé tant que
  le rendu est cassé ; il est prouvé sur le chemin NÉGATIF (détecte le noir).

## Options de correction (décision proprio — touche le choix de lib)

1. **PhotinoX.Blazor** : 4.2.0 est déjà la dernière (4.1.0 / 4.1.1 / 4.2.0). Pas de bump.
2. **Photino.Blazor original 4.0.13** (sorti depuis la migration ; la migration avait écarté la 4.x car
   « plafonne net9 » — à revérifier sur 4.0.13) : la lib d'origine sert `http://localhost/` sur Windows
   et rend correctement chez les autres.
3. **Revenir à Photino.Blazor 3.2.0** (pré-migration ; réintroduit l'épingle WebView NU1605).
4. **Déboguer l'interception WebView2** de PhotinoX (pourquoi `http://localhost/` n'est pas servi :
   content root du manager ? filtre `WebResourceRequested` ? version WebView2 ?).

## MISE À JOUR — repro minimal (ce n'est NI notre code NI le fork)

Une app **PhotinoX hello-world minimale** (un seul `<h1>`, sans Router, RCL, indirection render modes,
résolveur de contenu, ni CSP) — `.scratch/photino-hello/` (supprimée après diagnostic) — rend **le même
écran noir** (`received:false`, même `Load(http://localhost/)` non servi). → **Notre configuration est
hors de cause.**

Puis, en **changeant uniquement le paquet** dans cette hello-world vers le **Photino.Blazor d'origine
4.0.13** : **même écran noir, identique** (`received:false`). → **Ce n'est pas non plus spécifique au
fork PhotinoX** ; les deux variantes de Photino échouent à faire servir `http://localhost/` par WebView2.

**Environnement** : **WebView2 Runtime 149.0.4022.62** (très récent), **aucun proxy** (winhttp direct,
pas d'`HTTP_PROXY`), **hosts propre** (pas d'entrée localhost custom).

### Cause racine affinée (haute confiance)

**WebView2 149 ne sert pas `http://localhost/` via l'interception `WebResourceRequested` dont Photino.Blazor
dépend sur Windows.** Les requêtes **loopback (`localhost`)** sont court-circuitées de l'interception — c'est
exactement la raison pour laquelle le BlazorWebView de Microsoft utilise `https://0.0.0.0/` et **pas**
`localhost`. Photino (fork comme origine) a gardé `http://localhost/` → cassé sur ce WebView2.

### Options de correction RÉVISÉES (un swap de version ne suffit PAS)

1. ~~Photino.Blazor 4.0.13~~ — **réfuté** : écran noir identique.
2. **Faire servir Photino par un host non-loopback** (`https://0.0.0.0/` façon BlazorWebView Microsoft) :
   c'est codé **dans `PhotinoWebViewManager` de la lib** (host Windows = `http://localhost/` en dur) → exige
   un **patch/PR amont Photino** ou un fork local. Vrai correctif de fond.
3. **Tester une version WebView2 antérieure** (où l'interception localhost marchait) : non maîtrisable
   (composant système auto-MAJ) ; utile surtout pour **confirmer** que 149 est la régression. La CI Windows
   (WebView2 potentiellement plus ancien) pourrait rendre OK → le harnais opt-in le révélerait.
4. **Abandonner Photino** au profit d'un hôte BlazorWebView qui sert via `0.0.0.0`/virtual-host-mapping
   (p.ex. WinUI/MAUI BlazorWebView, ou WebView2 `SetVirtualHostNameToFolderMapping`) — décision d'archi.

**Recommandation** : décision proprio. Court terme = ouvrir un **issue amont Photino** (« WebView2 149 :
loopback non intercepté → écran noir ; passer à `0.0.0.0` ») et **confirmer en CI** si un WebView2 plus
ancien rend (via le harnais opt-in). Fond = (2) patch host non-loopback ou (4) rethink de l'hôte. Le
**moteur/CLI/notation ne sont pas affectés** — seule l'UX de bureau Photino l'est.
