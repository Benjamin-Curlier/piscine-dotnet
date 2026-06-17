# Contribuer

Merci de l'intérêt que tu portes à la **Piscine .NET** ! Ce guide résume le flux de
contribution. Pour les détails techniques (ajouter un exercice, conventions de contenu),
voir le dossier **[`docs/contributing/`](docs/contributing/)**.

## Flux : fork → branche → PR

1. **Forke** le dépôt sur ton compte, puis clone ton fork.
2. Crée une **branche** dédiée depuis `main`, nommée selon le changement :
   `feat/…`, `fix/…`, `docs/…` ou `chore/…`.
3. Développe, en gardant `dotnet build Piscine.slnx` et `dotnet test Piscine.slnx` au vert
   en local (aucun avertissement d'analyseur : `TreatWarningsAsErrors` est activé ;
   `dotnet format` doit être propre).
4. Ouvre une **pull request** vers `main` et remplis le modèle de PR.

## Conventional Commits (en français)

Les messages de commit et le titre de PR suivent les
[Conventional Commits](https://www.conventionalcommits.org/fr/), rédigés **en français** :

```
feat(grading): ajouter le grader réseau
fix(cli): corriger le code de sortie de validate-content
docs: clarifier la boucle de travail de la recrue
chore(ci): mettre à jour les actions GitHub
```

## Revue et intégration

La branche `main` est **protégée**. Pour être fusionnée, une PR doit :

- avoir une **CI verte** (`build-test` + `validate-content`, et l'analyse **CodeQL**) ;
- recevoir **au moins une revue approuvée**.

Mets à jour le `CHANGELOG.md` quand le changement est pertinent pour les utilisateurs.

## Aller plus loin

- **Ajouter un exercice** : [`docs/contributing/ajouter-un-exercice.md`](docs/contributing/ajouter-un-exercice.md).
- **Documentation encadrants & contributeurs** (moulinette, workflow de rendu, curriculum) :
  le [wiki](docs/wiki/Home.md).

Merci de respecter notre [Code de conduite](CODE_OF_CONDUCT.md). Pour signaler une faille
de sécurité, suis la [politique de sécurité](SECURITY.md).
