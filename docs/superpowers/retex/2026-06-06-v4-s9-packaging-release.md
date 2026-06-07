# Retex — v4 S9 : packaging/release Photino + docs setup webview

> Issue #30. Branche `v4/s9-packaging-release`. Plan : [../plans/2026-06-06-v4-s9-packaging-release.md](../plans/2026-06-06-v4-s9-packaging-release.md).
> **Verdict : objectif atteint.** L'app de bureau `Piscine.Desktop` est désormais empaquetée par OS
> dans les zips de `release.yml` (self-contained, libs natives Photino) à côté du CLI `piscine`
> **intact** ; un **dry-run CI 3 RID** prouve le publish à chaque PR ; le setup webview par OS est
> documenté + une checklist smoke proprio. **Aucun fichier moteur source touché ; aucun tag poussé.**

## Décisions techniques

- **`OutputType=WinExe` conservé (pas de modif csproj).** T1 (gating) a confirmé sur cette machine que
  le publish self-contained de `src/Piscine.Desktop` **réussit pour les 3 RID** (`win-x64`, `linux-x64`,
  `osx-arm64`) sans toucher au csproj : `WinExe` n'est qu'un flag de sous-système PE, ignoré hors
  Windows. Le repli `OutputType` conditionnel prévu au plan **n'a pas été nécessaire** (gardé documenté
  dans le plan au cas où un RID casserait plus tard → le dry-run T3 le rattraperait).
- **CORRECTION du plan — libs natives à la RACINE de sortie.** Le plan asserait
  `runtimes/<rid>/native/Photino.Native.*`. **C'est faux** : un publish self-contained de Photino.Blazor
  3.2.0 pose les libs natives **à la racine** du dossier de sortie — vérifié par publish réel des 3 RID :
  `Photino.Native.dll` + `WebView2Loader.dll` (win-x64), `Photino.Native.so` (linux-x64),
  `Photino.Native.dylib` (osx-arm64), et `Piscine.Desktop(.exe)` à la racine ; **rien** sous
  `runtimes/<rid>/native/`. Les assertions T3 (CI) et la doc utilisent ce chemin corrigé.
- **Packaging dans la même boucle RID, DevHost exclu par construction.** `release.yml` ajoute, dans la
  boucle `for rid`, un `dotnet publish src/Piscine.Desktop ... -o "$out/desktop"` après le publish CLI.
  L'app garde son nom `Piscine.Desktop(.exe)` (pas d'`AssemblyName` override → pas de collision avec le
  CLI `piscine`). `Piscine.DevHost` (site/harnais de dev) n'est **jamais nommé** dans un publish → exclu
  sans liste d'exclusion.
- **Lanceurs par OS.** `start-piscine-desktop.cmd` (Windows : MinGit sur `PATH` puis `start ""
  desktop\Piscine.Desktop.exe`) et `start-piscine-desktop.sh` (`exec "$DIR/desktop/Piscine.Desktop"`).
  Le CLI conserve son `start-piscine.cmd` inchangé (le hook `post-receive` l'appelle).
- **Vérification SANS tag (release = action proprio).** Deux filets, aucun tag : (1) **smoke local** des
  3 RID (T1, cette session) ; (2) **dry-run CI** (T3) qui rejoue le publish des 3 RID + asserte les libs
  natives **à chaque PR** — la même opération que la release, exercée avant tout tag. La validation
  finale « la fenêtre native s'ouvre » reste une **checklist proprio** par OS (non agent-vérifiable).

## Ce qui est PROUVÉ (par l'agent, automatique)

- **Publish cross-RID réel** (cette machine, Windows) : `dotnet publish src/Piscine.Desktop -c Release
  -r <rid> --self-contained` réussit pour `win-x64`, `linux-x64` **et** `osx-arm64` (cross-RID).
- **Libs natives à la racine** : assertions `test -f "$out/Photino.Native.{dll,so,dylib}"` (+
  `WebView2Loader.dll` win) + exe → **OK pour les 3 RID** (exactement le script du dry-run CI, simulé
  localement contre les sorties de publish réelles).
- **Dry-run CI ajouté** (`ci.yml`) : publie les 3 RID + asserte exe & libs natives à chaque PR → preuve
  automatique avant tout tag (et garde-fou contre une « rotation » du packaging entre sprints).
- **Garde « moteur source intact »** : `git diff --name-only origin/main...HEAD -- src/Piscine.Core
  src/Piscine.Grading src/Piscine.Git src/Piscine.Cli` = **vide** (aucun `src/` touché du tout).
- Build solution **0 warning** ; **246 tests verts** (Core 46 + Components 23 + Git 7 + App 51 +
  Grading 111 + DevHost.E2E 8) ; `validate-content` = « Contenu valide. » ; **aucun tag** à HEAD.

## Checklist smoke par OS — **À EXÉCUTER PAR LE PROPRIO** (après tag d'une pré-release)

Un agent ne peut pas ouvrir une fenêtre native. Le packaging est livré ; la validation finale =
taguer une **pré-release** (`gh release ... --prerelease`), télécharger le zip par OS et dérouler :

- [ ] **Windows** : dézipper → double-clic `start-piscine-desktop.cmd` → fenêtre + cours (titre + gras
  + bloc de code colorisé). (Éditions N / images minimales : installer l'Evergreen WebView2 Runtime.)
- [ ] **Linux** : `sudo apt install libwebkit2gtk-4.1-0` (ou `dnf install webkit2gtk4.1`) →
  `./start-piscine-desktop.sh` → fenêtre + cours.
- [ ] **macOS** : `./start-piscine-desktop.sh` → fenêtre + cours (WKWebView intégré, rien à installer).
- [ ] **CLI intact** dans le même zip : `piscine init` puis `piscine status` répondent (le hook de
  correction n'est pas affecté par l'ajout de l'app de bureau).

## Limites connues (acceptables, à suivre)

- **Fenêtre native non vérifiée par l'agent** (par nature) → checklist proprio ci-dessus. Le dry-run CI
  prouve la *construction*, pas l'*ouverture*.
- **`osx-arm64` non lancé** (cross-publié + libs assertées seulement) : pas de runner macOS ; le
  `.dylib` est posé à la racine comme les autres RID, le smoke proprio macOS lève le doute final.
- **Hôte Photino non câblé sur les services `Piscine.App`** (suivi HANDOFF (b)) : `Piscine.Desktop` ne
  monte aujourd'hui que `MarkdownView` (spike S1). S9 livre le **packaging** de cet hôte ; le câblage
  complet (CourseCatalog/GitStatus/Check/Progress + routage RCL + terminal) reste un sprint à part.
  La checklist smoke « fenêtre + cours » reflète donc l'état actuel de l'hôte.
- **Pas de signature / notarisation / installeur** (hors périmètre) : on livre des zips self-contained,
  comme le CLI. SmartScreen/Gatekeeper côté recrue = déjà documenté (mise-en-oeuvre §2/§5).

## Pièges réutilisables (pour le HANDOFF / sprints suivants)

- **Photino.Blazor self-contained → libs natives à la RACINE** du dossier de sortie, **pas** sous
  `runtimes/<rid>/native/`. Toute assertion de packaging (CI, doc, scripts) doit viser
  `"$out/Photino.Native.{dll,so,dylib}"` (+ `WebView2Loader.dll` Windows). (Vérifié par publish réel.)
- **Cross-RID publish depuis Windows** : `dotnet publish -r linux-x64|osx-arm64 --self-contained`
  fonctionne et **télécharge les runtime packs** du RID (restore au publish) → ne pas mettre
  `--no-restore` sur ces étapes (la restore solution ne contient pas les packs par RID).
- **`OutputType=WinExe` est cross-RID-safe** : c'est un flag PE Windows, ignoré sur les autres OS ; pas
  besoin de le rendre conditionnel pour publier Linux/macOS.
- **Exclure un projet du packaging = ne jamais le nommer** dans un `publish` (pas de liste d'exclusion) :
  `Piscine.DevHost` reste hors release simplement parce qu'aucune étape ne le publie.
- **Dry-run de packaging en CI** = exécuter la *même* opération que la release (publish par RID +
  assertions d'artefacts) sur chaque PR : attrape une régression d'empaquetage **avant** le tag, sans
  publier de release. Modèle réutilisable pour tout job « release-at-tag ».
- **Outil/here-string** : le Bash tool exécute du **bash**, pas PowerShell → ne pas utiliser les
  here-strings `@'...'@` (le `@` fuit dans le message de commit). Utiliser `git commit -F - <<'EOF'`
  (délimiteur quoté = pas d'expansion des `'`, `(`, `$`).
