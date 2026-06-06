# Déploiement — préparer et publier une release

Ce guide s'adresse au **mainteneur** qui publie une nouvelle version de la piscine. La recrue, elle,
consomme le résultat (le zip self-contained) via [docs/mise-en-oeuvre.md](mise-en-oeuvre.md).

Publier = **pousser un tag `v*`**. Le workflow [`.github/workflows/release.yml`](../.github/workflows/release.yml)
fait tout le reste : il assemble le contenu, publie un binaire self-contained par OS, l'empaquette
et crée la **GitHub Release** avec les zips attachés.

> ⚠️ **Action publique et quasi irréversible.** Pousser un tag `v*` déclenche immédiatement la
> publication d'une release visible. Ne tague qu'après la check-list ci-dessous et avec l'accord du
> propriétaire du dépôt.

---

## 1. Versionnement

- **SemVer** : `vMAJEUR.MINEUR.CORRECTIF` (ex. `v2.0.0`).
- **Le tag git est l'unique source de vérité.** Il n'y a **aucun numéro de version** dans les
  `.csproj` ni dans `Directory.Build.props` : le workflow nomme la release et les zips d'après
  `github.ref_name` (le nom du tag). Pas de fichier à bumper avant de taguer.
- **Quand incrémenter MAJEUR** : ajout d'un palier de contenu / d'un grader, changement de format de
  contenu ou de CLI susceptible de casser un workspace existant.
- Historique des versions : [CHANGELOG.md](../CHANGELOG.md).

> Note de nommage : la *roadmap* parle de « paliers » v1 / v2 / v3 (cf.
> [roadmap-v2-v3](superpowers/plans/2026-05-31-roadmap-v2-v3.md)). Ce sont des **phases de contenu**,
> pas des numéros de release. Exemple : la release **`v2.0.0`** embarque à la fois le palier de
> contenu v2 (M24–M35) **et** le palier v3 (M36–M39). Documenter ce mapping dans le CHANGELOG.

---

## 2. Check-list avant tag

Tout doit être vert **sur `main`**, arbre propre :

- [ ] `dotnet test Piscine.slnx -c Release` → **0 échec** (mettre à jour le compteur dans le HANDOFF).
- [ ] `validate-content` → **« Contenu valide. »** :
      ```powershell
      $env:PISCINE_CONTENT = "$PWD\content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content
      ```
- [ ] `docs/wiki/Curriculum.md` reflète les modules/Rushes livrés.
- [ ] Entrée ajoutée en tête de [CHANGELOG.md](../CHANGELOG.md) pour la version visée.
- [ ] `git status` propre, tout poussé, **CI verte sur `main`** :
      ```powershell
      gh run list --branch main --limit 1 --json status,conclusion
      ```
- [ ] (Recommandé) **répétition locale du packaging** — voir §5 — pour détecter une régression
      d'assemblage sans déclencher de release.

---

## 3. Publier

Faire les appels **séparément** (un refus de push ne doit pas perdre le commit) :

```powershell
# 1. La doc de release (CHANGELOG, HANDOFF, Curriculum) est commitée et poussée sur main
git push origin main
# 2. Attendre la CI verte sur le commit à taguer (voir check-list)
# 3. Taguer ce commit, puis pousser le tag (déclenche release.yml)
git tag v2.0.0
git push origin v2.0.0
```

Suivre l'exécution :

```powershell
gh run list --workflow release.yml --limit 1 --json status,conclusion,headBranch
gh release view v2.0.0
```

---

## 4. Ce que produit `release.yml`

Pour chaque RID (`linux-x64`, `win-x64`, `osx-arm64`) :

1. **`package-content content artifacts/content`** — copie le contenu **sans les `solution/`**
   (les corrigés ne sont jamais distribués).
2. **`dotnet publish` self-contained** — runtime .NET + Roslyn embarqués (aucun SDK requis côté recrue).
3. Le dossier `content/` assemblé est copié à côté du binaire.
4. **Windows uniquement** : MinGit portable (version épinglée dans `release.yml`) est dézippé dans
   `mingit/` et `start-piscine.cmd` est ajouté → git fonctionne sans installation.
5. Zip nommé `piscine-<tag>-<rid>.zip`.

Puis `gh release create <tag> dist/*.zip` attache les trois zips. Le titre est `Piscine .NET <tag>`.

### Notes de release

`release.yml` pose une **note générique**. Pour publier de vraies notes (recommandé), les remplacer
après coup à partir du CHANGELOG :

```powershell
gh release edit v2.0.0 --notes-file release-notes.md   # extrait de la section v2.0.0 du CHANGELOG
```

---

## 5. Répétition locale (sans taguer)

Reproduire l'assemblage pour vérifier avant de déclencher la vraie release :

```powershell
dotnet run --project src/Piscine.Cli -c Release -- package-content content artifacts/content
dotnet publish src/Piscine.Cli -c Release -r win-x64 --self-contained true -o artifacts/pkg/piscine-win-x64
# vérifier que artifacts/content ne contient AUCUN dossier solution/
```

---

## 6. En cas de problème

- **`release.yml` échoue** : corriger sur `main`, **supprimer** la release ratée et son tag, puis
  re-taguer :
  ```powershell
  gh release delete v2.0.0 --yes
  git push origin :refs/tags/v2.0.0   # supprime le tag distant
  git tag -d v2.0.0                    # supprime le tag local
  ```
- **Mauvais contenu publié** : un corrigé qui apparaît dans un zip = `package-content` mal appelé ;
  vérifier l'étape §4.1 et la répétition locale §5.
- **SmartScreen / antivirus** côté recrue : c'est attendu (binaire non signé) — documenté dans
  [docs/mise-en-oeuvre.md](mise-en-oeuvre.md#5-dépannage).
