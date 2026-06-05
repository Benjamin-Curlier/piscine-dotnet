# Piscine .NET

Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux **C#** (.NET 10),
avec moulinette auto-correctrice locale, apprentissage du vrai **git**, et distribution autonome.

## Pour la recrue

Télécharge le zip de la [dernière release](../../releases/latest), dézippe, puis lance `piscine`
(`piscine.exe` sous Windows). Aucun SDK à installer.

Guide pas-à-pas (installation, premier lancement, boucle de travail, dépannage) :
**[docs/mise-en-oeuvre.md](docs/mise-en-oeuvre.md)**.

## Pour développer le bootcamp

Pré-requis : SDK .NET 10.

```bash
dotnet build Piscine.slnx
dotnet test Piscine.slnx
dotnet run --project src/Piscine.Cli
```

## Site du cours (navigateur)

Le cours et les sujets d'exercices se consultent dans un navigateur via un site **Blazor**
(stack .NET, façon Docusaurus) qui lit directement `content/` :

```bash
dotnet run --project src/Piscine.Web
```

Puis ouvrir http://localhost:5244. Le site présente le cours et les sujets (sans les corrigés) ;
il localise `content/` automatiquement (ou via la variable `PISCINE_CONTENT`).

## Documentation (encadrants & contributeurs)

Le wiki du projet est versionné dans le dépôt : **[docs/wiki/Home.md](docs/wiki/Home.md)**
(fonctionnement de la moulinette, workflow de rendu, ajouter un exercice, curriculum, mise en œuvre).

## Structure

- `src/` : application (`Piscine.Cli`), bibliothèques (`Core`, `Grading`, `Git`) et site du cours (`Piscine.Web`).
- `tests/` : tests xUnit.
- `content/` : cours, exercices et rushes (voir `content/README.md`).
- `docs/` : specs (`docs/superpowers/specs/`), plans (`docs/superpowers/plans/`),
  guide contributeur (`docs/contributing/`).

Design complet : `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md`.

**Reprendre le développement à froid** : `docs/superpowers/HANDOFF.md` (état du projet, méthode,
prochaines étapes, pièges).
