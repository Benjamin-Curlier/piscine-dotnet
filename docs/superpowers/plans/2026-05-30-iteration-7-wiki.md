# Itération 7 — Wiki GitHub — Implementation Plan

> **For agentic workers:** itération de **documentation** (prose), pas de TDD/tests. Cases `- [ ]` pour le suivi.

**Goal:** Publier la documentation projet (encadrants & contributeurs) dans le **Wiki GitHub** du dépôt (spec §10.2). Pages : Accueil, Moulinette, Workflow de rendu, Ajouter un exercice, Curriculum, Mise en œuvre + une barre latérale.

**Architecture / approche :** Les pages sont **sources de vérité dans le dépôt principal** sous `docs/wiki/` (versionnées, revues, sauvegardées, poussées via le workflow `main` déjà autorisé), **puis publiées en miroir** dans le dépôt de wiki `Benjamin-Curlier/piscine-dotnet.wiki.git` (édité en local puis poussé). Le wiki GitHub étant **désactivé** sur le dépôt (`has_wiki=false`), il faut l'activer ; le dépôt de wiki n'existe qu'après la 1re page (le 1er `git push` vers `*.wiki.git` l'initialise quand le wiki est activé). Noms de fichiers = conventions wiki GitHub (`Home.md`, `_Sidebar.md`, espaces→`-`).

**Tech Stack:** Markdown. `gh` (authentifié, scopes repo) pour activer le wiki ; `git` pour pousser le dépôt de wiki.

**Contexte repo (It.0→It.6 faites, release v0.1.0 en ligne) :** spec complète dans `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md` (philosophie §2, moulinette §4.3, flux git §5, curriculum §6, doc §10). `docs/mise-en-oeuvre.md` (It.6b) ; `docs/contributing/ajouter-un-exercice.md`. Commandes CLI : `list/start/check/status/init/grade-received/validate-content/package-content`. Graders `io`/`unit`/`norme` ; `GroupGrader` (stop au 1er KO) ; hook `post-receive`. Commits français finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. **Process : `git commit` et `git push` en appels SÉPARÉS.**

---

## File Structure

| Fichier (`docs/wiki/`) | Page wiki | Contenu |
|---|---|---|
| `Home.md` | Accueil | objectif, philosophie (retour éducatif, pas de note), liens |
| `Moulinette.md` | Fonctionnement de la moulinette | Roslyn, graders io/unit/norme, groupe stop-1er-KO, progression |
| `Workflow-de-rendu.md` | Workflow de rendu | dépôt bare, hook post-receive, `check` vs `git push` |
| `Ajouter-un-exercice.md` | Ajouter un exercice / module | format `manifest.yaml`/`module.yaml`, `validate-content` |
| `Curriculum.md` | Curriculum | carte modules + Rushes + références externes |
| `Mise-en-oeuvre.md` | Mise en œuvre | renvoi vers `docs/mise-en-oeuvre.md` |
| `_Sidebar.md` | (navigation) | liens vers toutes les pages |

---

## Task 1 : Rédiger les pages sous `docs/wiki/`

- [ ] **Step 1** : Créer les 7 fichiers ci-dessus (contenu fidèle à la spec §2/§4/§5/§6/§10).
- [ ] **Step 2** : Relecture cohérence (noms de commandes, statuts *Réussi / À revoir / Non corrigé*, liens internes wiki `[[Titre]]`).
- [ ] **Step 3 : Commit** (sources dans main)

```bash
git add docs/wiki
git commit -m "docs(wiki): pages du wiki GitHub (sources)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 4 : push main** (appel séparé) + `gh run watch` (CI verte — pas de code touché).

---

## Task 2 : Publier sur le Wiki GitHub

- [ ] **Step 1 : Activer le wiki** (changement de réglage du dépôt) :

```bash
gh api -X PATCH repos/Benjamin-Curlier/piscine-dotnet -F has_wiki=true --jq '.has_wiki'
```
Expected : `true`.

- [ ] **Step 2 : Cloner / initialiser le dépôt de wiki dans un dossier ignoré** (`artifacts/` est gitignored) :

```bash
git clone https://github.com/Benjamin-Curlier/piscine-dotnet.wiki.git artifacts/wiki 2>&1 || git init artifacts/wiki
```
Si le clone échoue (« does not exist »), initialiser un dépôt vide et ajouter le remote :
```bash
git -C artifacts/wiki init
git -C artifacts/wiki remote add origin https://github.com/Benjamin-Curlier/piscine-dotnet.wiki.git
```

- [ ] **Step 3 : Copier les pages, commit, push** :

```bash
cp docs/wiki/*.md artifacts/wiki/
git -C artifacts/wiki add -A
git -C artifacts/wiki commit -m "Publication initiale du wiki (It.7)"
git -C artifacts/wiki branch -M master   # le wiki GitHub utilise master
git -C artifacts/wiki push -u origin master
```
> Si le push échoue avec « The wiki ... does not exist » : le wiki n'est pas initialisable par push seul → **demander au proprio de créer la 1re page via l'UI** (Wiki → Create the first page → Save), puis re-pousser. Les sources `docs/wiki/` restent le livrable dans tous les cas.

- [ ] **Step 4 : Vérifier** : `gh api repos/Benjamin-Curlier/piscine-dotnet/pages` n.a. ; vérifier via `git -C artifacts/wiki ls-remote origin` que `master` existe, ou ouvrir l'onglet Wiki. Nettoyer : `Remove-Item -Recurse artifacts`.

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.7) :** 6 pages spec §10.2 + sidebar, sources dans `docs/wiki/` (T1), miroir publié sur le Wiki GitHub après activation (T2). ✓

**Décision :** sources de vérité dans `docs/wiki/` (versionné, revu, dans la CI) + miroir wiki — léger surcoût de synchro assumé pour la durabilité. **Risque connu :** un wiki jamais initialisé peut refuser le 1er push → repli = création de la 1re page via l'UI par le proprio (action manuelle hors agent).

**Reporté :** It.8 (Module 00 complet — 1er vrai contenu, dogfood du parcours), It.9+ (contenu M01→M23 + Rushes).
