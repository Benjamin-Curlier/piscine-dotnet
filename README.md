# Piscine .NET

Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux **C#** (.NET 10),
avec une **moulinette auto-correctrice locale**, l'apprentissage du **vrai git**, et une
**distribution autonome** (installeurs ou zips self-contained, zéro SDK). **Windows et Linux.**

## Pour la recrue

Deux UX au choix, même moteur : une **app de bureau** (cours, vérification, progression, terminal +
coaching git, résultat) ou le **CLI** `piscine`. Récupère le paquet de la
[dernière release](../../releases/latest) :

- **Installeur** (recommandé) : Windows `.exe` (par utilisateur, sans admin) ou Linux `.AppImage`,
  en variante **offline** (tout embarqué, hors-ligne) ou **online**.
- **Zip portable** : `piscine-<v>-win-x64.zip` / `-linux-x64.zip` (rien n'est installé).

Aucun SDK à installer (runtime .NET + Roslyn embarqués). Guide pas-à-pas (installation, premier
lancement, boucle de travail, dépannage) : **[docs/mise-en-oeuvre.md](docs/mise-en-oeuvre.md)**.

## Pour développer le bootcamp

Pré-requis : SDK .NET 10.

```bash
dotnet build Piscine.slnx
dotnet test Piscine.slnx
dotnet run --project src/Piscine.Cli           # le CLI (moteur, grade-received, validate-content…)
dotnet run --project src/Piscine.Desktop -c Release   # l'app de bureau Photino (fenêtre native)
```

## Site du cours / harnais de dev (navigateur)

Le cours et les sujets se consultent aussi dans un navigateur via un site **Blazor**
(`Piscine.DevHost`, stack .NET, façon Docusaurus, mode sombre) qui lit directement `content/` et
sert de **harnais de test** des composants. **Hors release** (jamais distribué) :

```bash
dotnet run --project src/Piscine.DevHost      # http://localhost:5244
```

Le site présente le cours et les sujets (sans les corrigés) ; il localise `content/`
automatiquement (ou via la variable `PISCINE_CONTENT`).

## Documentation (encadrants & contributeurs)

Le wiki du projet est versionné dans le dépôt : **[docs/wiki/Home.md](docs/wiki/Home.md)**
(fonctionnement de la moulinette, workflow de rendu, ajouter un exercice, curriculum, mise en œuvre).
Publier une release (mainteneur) : **[docs/deploiement.md](docs/deploiement.md)**.

## Structure

- `src/` :
  - **moteur & CLI** — `Piscine.Core` (modèles + découverte de contenu), `Piscine.Grading`
    (Roslyn + graders `io`/`unit`/`norme`/`mutation`/`git`/`projet`/`reseau`), `Piscine.Git`
    (rendu git LibGit2Sharp + `grade-received`), `Piscine.Cli` (binaire `piscine`).
  - **app de bureau** — `Piscine.Components` (RCL Blazor partagée : pages/composants + rendu Markdig),
    `Piscine.App` (services UI-less : check, statut/coaching git, progression, terminal PTY, init,
    surveillance du push), `Piscine.Desktop` (hôte Photino.Blazor livré), `Piscine.DevHost`
    (site/harnais Blazor de dev, hors release), `Piscine.GitShim` (shim `git` pour le coaching).
- `tests/` : tests xUnit (+ bUnit pour les composants, Playwright pour l'E2E DevHost).
- `content/` : cours, exercices et rushes (voir `content/README.md`). Les dossiers `solution/`
  (corrigés) sont **exclus du paquet distribué** (`package-content`).
- `docs/` : doc recrue/mainteneur (`mise-en-oeuvre.md`, `deploiement.md`), wiki (`docs/wiki/`),
  specs/plans/retex/ADR (`docs/superpowers/`), guide contributeur (`docs/contributing/`).

Design complet : `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md`.

**Reprendre le développement à froid** : `docs/superpowers/HANDOFF.md` (état du projet, méthode,
prochaines étapes, pièges).
