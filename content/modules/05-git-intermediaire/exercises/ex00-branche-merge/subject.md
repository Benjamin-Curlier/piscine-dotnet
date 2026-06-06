# Branche et fusion

Premier exercice git **auto-noté** : ici, **aucun fichier de code à rendre**. La moulinette
**inspecte l'état de ton dépôt** (tes branches, tes commits, ta fusion). Tu travailles avec les
commandes git, exactement comme dans le cours.

## Ce qu'on attend de ton dépôt

À partir d'un dépôt qui contient déjà au moins un commit sur `main` :

1. **Crée une branche** nommée `feature` et place-toi dessus :
   ```bash
   git switch -c feature
   ```
2. Sur `feature`, **crée un fichier** `feature.txt` contenant le mot `salut`, puis **commite** :
   ```bash
   echo "salut" > feature.txt
   git add feature.txt
   git commit -m "ajout feature"
   ```
3. **Reviens sur `main`** et **fusionne** `feature` :
   ```bash
   git switch main
   git merge feature
   ```

## Ce qui est vérifié

- les branches `main` **et** `feature` existent ;
- `main` contient **au moins 2 commits** ;
- `feature` est bien **fusionnée** dans `main` (sa pointe est un ancêtre de `main`) ;
- le fichier `feature.txt` (vu depuis `main`) **contient** `salut` ;
- il **ne reste aucun marqueur de conflit** (`<<<<<<<`, `=======`, `>>>>>>>`) dans tes fichiers.

## Conseils

- `git switch -c <nom>` crée la branche **et** s'y place ; `git switch <nom>` y revient.
- Une fusion **fast-forward** suffit ici : tant que la pointe de `feature` est atteignable depuis
  `main`, l'exercice est validé.
- En cas de conflit lors d'un merge, **résous-le** puis termine par `git add` + `git commit` : ne
  laisse jamais les marqueurs `<<<<<<<` dans un fichier.

> Pas de livrable de code : c'est ton **historique git** qui est évalué. Reporte-toi au cours
> (sections [branches](cours.md#branches), [merge](cours.md#merge), [conflits](cours.md#conflits)).
