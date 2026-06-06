# Sprint 8 (V3) — Adoption contenu du grader git (issue #9)

> Scrum / loop V3. Branche `feat/git-content-adoption`. Rend le grader `git` (#1) utilisable en contenu.

## Constats de design
- `GitGrader` inspecte un dépôt par chemin et LibGit2Sharp marche sur un dépôt **bare** → `grade-received`
  peut noter un exo git directement sur le **dépôt bare origin** (`_layout.RemoteRepoPath`), sans
  réécrire `CommitExtractor` (qui produit un snapshot plat).
- Problème du gate : un exo git n'a **pas de `solution/`** (le « corrigé » est un état de dépôt).
  → On ajoute une **fixture déclarative** au manifest ; le gate construit un dépôt temporaire via
  LibGit2Sharp et y exécute le GitGrader (doit réussir). Le contenu git devient auto-validable.

## Conception
### Modèle (`GitAssertions.Fixture` : liste de `GitFixtureStep`)
Étapes ordonnées, champs optionnels :
```yaml
git:
  fixture:
    - { branch: main, message: "init", files: { "README.md": "Bonjour\n" } }  # commit (crée la branche si besoin)
    - { branch: feature, base: main }                                          # créer une branche depuis base
    - { branch: feature, message: "feat", files: { "feature.txt": "x\n" } }    # commit sur feature
    - { merge_into: main, merge_from: feature }                                # fusion
  branches: [main, feature]
  min_commits: 2
  merged: [{ into: main, branch: feature }]
  no_conflict_markers: true
```
`GitFixtureStep { Branch, Base, Message, Files(dict), MergeInto, MergeFrom }`.

### `GitFixtureBuilder` (Piscine.Grading, LibGit2Sharp)
`Build(steps, dir)` : `Repository.Init`, puis interprète chaque étape (commit / création de branche /
merge). 1re branche posée via `Refs.UpdateTarget("HEAD", refs/heads/<branch>)` avant le 1er commit.

### Gate (`ContentValidator`)
Pour un exo **git** (manifest a une étape `git`) : ne pas exiger `solution/` ; construire la fixture
dans un temp, `GradingContext(repositoryPath: fixtureDir)`, exécuter le grader → doit réussir.

### Production (`GradeReceivedCommand`)
Passe dédiée : pour chaque exo **git** du contenu, noter le **dépôt bare** (`RemoteRepoPath`) — exos
git autonomes (comme les rushes), notés à chaque push.

### Pilote M05 (`git`)
Bascule M05 de lecture → groupe avec un exo `git` (branches + merge + min_commits + no_conflict_markers),
fixture = scénario corrigé. cours.md ancré.

## DoD (Sprint 8)
- [ ] Modèle `Fixture`/`GitFixtureStep` + `GitFixtureBuilder` + tests unitaires
- [ ] `ContentValidator` valide les exos git via fixture
- [ ] `GradeReceivedCommand` note les exos git sur le dépôt bare
- [ ] Pilote **M05** en `git` ; `validate-content` vert
- [ ] `dotnet test Piscine.slnx -c Release` vert + revue agent + docs + retex + PR mergée CI verte
