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

Recommandation : tester (2) d'abord (changement de version le plus propre, garde net10), avec le harnais
ci-dessus comme juge de rendu.
