# Plan — v4 S10 : docs recrue/encadrant + Curriculum/HANDOFF/CHANGELOG (flux desktop)

> Issue **#31** (milestone #2 « v4 », label `v4`). Branche : `v4/s10-docs-desktop`.
> Dépendances : **S1–S9b mergées** (`main`, 247 tests). **Dernier sprint du backlog v4 (#22–#31).**
> Plan S9b : [`2026-06-07-v4-s9b-photino-wiring.md`](2026-06-07-v4-s9b-photino-wiring.md).

## Pourquoi maintenant (et pas avant)
S9b a rendu le **flux desktop réel** (l'app Photino monte cours/`/check`/`/progress`/`/init`/`/resultat`).
Avant S9b, documenter un « flux desktop de bout en bout » aurait été **mensonger** (l'app n'était qu'un
spike `MarkdownView`). Maintenant la doc peut décrire honnêtement : **app de bureau = UX recrue
principale** (cours + vérification + progression + init + résultat) ; **terminal OS** (via
`start-piscine-desktop` / `start-piscine`) pour `git add/commit/push` (le rendu officiel). Le **CLI reste**
documenté (équivalent sans fenêtre / le hook l'appelle).

## Garde-fous
- **Sprint DOCS uniquement** : modifs sous `docs/**` + `CHANGELOG.md` (racine). **NE PAS toucher `src/`,
  `release.yml`, `ci.yml`.** Pas de nouveau test (aucun code).
- **`validate-content` reste vert** (la gate lit `content/`, pas `docs/` — mais le lancer par sûreté).
- **NE PAS pousser de tag** (release = action proprio). S10 prépare une section CHANGELOG « Non publié » ;
  le proprio la versionnera s'il décide de taguer.
- **Honnêteté** : ne PAS documenter le **terminal embarqué dans l'app** (déféré S9b, pas dans le zip).
  Le terminal de la recrue = terminal OS. Ne pas promettre le coaching git in-app.

## Carte des fichiers
| Fichier | Action |
|---|---|
| `docs/mise-en-oeuvre.md` | T1 — le flux **desktop** devient le parcours principal (cours/check/progress/init/resultat dans l'app ; git via terminal OS) ; CLI conservé en alternative ; checklist encadrant webview |
| `CHANGELOG.md` | T2 — section **`## [Non publié]`** en tête : app de bureau Photino v4 (S1→S9b) |
| `docs/wiki/Curriculum.md` | T3 — note « UX recrue = app de bureau (ou CLI) » (touche légère ; le curriculum de contenu est inchangé) |
| `docs/deploiement.md` | T4 — MAJ smoke : « la fenêtre **route** le flux » (S9b) ; sinon S9 déjà à jour |
| `docs/superpowers/HANDOFF.md` | T5 — vérifier que « l'état v4 » est repris (déjà fait S1→S9b ; touche-up si besoin) |
| `docs/superpowers/retex/2026-06-07-v4-s10-docs-desktop.md` | T6 — retex court + clôture backlog v4 |

---

## T1 — `docs/mise-en-oeuvre.md` : flux desktop principal
- [ ] **§1 Prérequis** : déjà MAJ en S9 (webview par OS). Vérifier la cohérence (l'app n'est plus
  « optionnelle » : c'est l'UX principale, le CLI reste possible).
- [ ] **§2 Installation** : contenu du zip déjà listé (S9 : `desktop/` + lanceur). OK ; ajuster le ton
  (desktop = recommandé).
- [ ] **§3 Premier lancement** : **mener avec l'app de bureau** : `start-piscine-desktop.cmd` (Windows) /
  `./start-piscine-desktop.sh` (Linux/macOS) → fenêtre ; **Initialiser** via la page `/init` (bouton)
  **ou** `piscine init` au terminal. Garder la voie CLI en sous-section « Sans l'app (CLI) ».
- [ ] **§4 Boucle de travail** : décrire la boucle **desktop** : choisir un module/exo (cours), coder dans
  l'IDE externe, **Vérifier** (page `/check`, autant de fois qu'on veut, ne compte pas), suivre
  **Progression** (`/progress`), puis **rendu officiel** = `git add/commit/push` au **terminal OS**
  (via `start-piscine-desktop`/`start-piscine` qui mettent git sur le PATH), enfin **Résultat** (`/resultat`
  s'auto-rafraîchit). Conserver l'équivalent CLI (`piscine check`/`status` + git) en parallèle.
- [ ] **§6 Encadrant** : checklist = prérequis **webview** en place par OS + l'app ouvre la fenêtre et
  **route** (cours/check/progress/init/resultat) + `git push` (rendu) fonctionne depuis le terminal.

## T2 — `CHANGELOG.md` : section « Non publié » (app de bureau v4)
- [ ] Insérer, **avant `## [v2.0.0]`**, une section :
  ```
  ## [Non publié]

  ### Ajouté — Application de bureau (Photino.Blazor)
  - **UX recrue de bureau** : … (cours, vérification instantanée, progression, init, résultat de push).
  - **Packaging** : `Piscine.Desktop` self-contained par OS dans le zip (dossier `desktop/` + lanceur
    `start-piscine-desktop`), à côté du CLI `piscine` **inchangé** ; dry-run CI 3 RID.
  - **Prérequis webview** par OS (WebView2 / libwebkit2gtk-4.1 / WKWebView).
  - Limites : terminal embarqué/coaching git **non** inclus (terminal OS pour `git push`).
  ### Inchangé
  - Moteur de correction, CLI headless et `grade-received` (hook `post-receive`) **identiques**.
  ```
  (Texte FR « Keep a Changelog ». Lister S1→S9b en une poignée de puces lisibles, pas exhaustif.)

## T3 — `docs/wiki/Curriculum.md` : note UX desktop
- [ ] Sous l'encadré release, ajouter une note : **UX recrue = app de bureau Photino** (cours + vérif +
  progression + init + résultat) **ou** le CLi `piscine` ; le **contenu** (M00–M39 + Rushes) est inchangé.
  (Touche légère — ne pas réécrire le tableau des modules.)

## T4 — `docs/deploiement.md` : smoke « route le flux »
- [ ] Dans la checklist smoke pré-release (S9), préciser que la fenêtre doit **router** (Accueil → module →
  cours colorisé, `/check` rend un verdict, `/progress`, `/init`, `/resultat`) — pas seulement « afficher un
  cours ». (Reflète S9b.)

## T5 — `docs/superpowers/HANDOFF.md`
- [ ] Vérifier que la section v4 reprend l'état (S1→S9b déjà consigné). Au besoin : pointer S10 comme
  **clôture du backlog v4** une fois mergé (fait par le parent à la consignation).

## T6 — Vérif + retex + PR (sans tag)
- [ ] `validate-content` → « Contenu valide. ». (`dotnet build` facultatif : aucun `src/` touché — le
  vérifier via `git diff --name-only origin/main...HEAD -- src .github` = vide.)
- [ ] Retex `docs/superpowers/retex/2026-06-07-v4-s10-docs-desktop.md` : doc alignée sur le flux desktop
  réel ; **backlog v4 (#22–#31) clôturé** ; rappel : tag = décision proprio (CHANGELOG « Non publié » prêt).
- [ ] PR (push + create **séparés**) → CI verte → `gh pr merge --squash --delete-branch` → `Fixes #31`
  (anglais) ou `gh issue close 31`. Consigner (HANDOFF + mémoire) : **v4 TERMINÉ**.

Expected : doc recrue/encadrant décrit le **flux desktop réel** de bout en bout (sans promettre le terminal
in-app) ; CHANGELOG « Non publié » prêt ; `validate-content` OK ; **0 fichier `src/`/CI touché** ; aucun
tag ; **milestone #2 (v4) clos**. Le proprio décidera d'un tag de pré-release pour le smoke par OS.

## Self-review (couverture vs #31)
- mise-en-oeuvre flux desktop ✅ T1 ; checklist encadrant webview ✅ T1/§6 ; Curriculum ✅ T3 ; HANDOFF ✅
  (déjà à jour) T5 ; CHANGELOG ✅ T2. Acceptation « un nouvel arrivant suit la doc desktop de bout en
  bout » → vraie depuis S9b. Pas de gold-plating (pas de tag, pas de réécriture du contenu pédagogique).
