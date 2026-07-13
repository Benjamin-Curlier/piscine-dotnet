# Résoudre un conflit de fusion

Un **conflit** n'est ni un bug ni une faute : c'est git qui, face à **deux versions de la même
ligne**, refuse de choisir à ta place et te demande de trancher. Ici, tu vas **provoquer** un conflit
volontairement, puis le **résoudre** proprement.

Comme les exercices git précédents, **aucun fichier de code à rendre** : la moulinette **inspecte
l'état de ton dépôt**.

## Ce qu'on attend de ton dépôt

1. Sur `main`, crée un fichier `chanson.txt` avec quelques lignes, dont une ligne
   `refrain: A COMPLETER`, et commite-le.
2. **Crée une branche** `refrain`, et sur cette branche, remplace la ligne du refrain par
   exactement :
   ```
   refrain: on chante ensemble
   ```
   puis commite.
3. **Reviens sur `main`** et modifie **la même ligne** `refrain: ...` **autrement** (n'importe quel
   autre texte), puis commite. Les deux branches ont désormais touché **la même ligne** : le décor du
   conflit est planté.
4. **Fusionne** : `git merge refrain`. 💥 git signale un **conflit** sur `chanson.txt`.
5. **Résous-le** : ouvre `chanson.txt`, garde la version `refrain: on chante ensemble`, **supprime les
   trois marqueurs** (`<<<<<<<`, `=======`, `>>>>>>>`), puis termine la fusion :
   ```bash
   git add chanson.txt
   git commit
   ```

## Rendre ton travail

```bash
git push origin --all
```

> Tant que la branche `refrain` n'est pas poussée, l'exercice est considéré comme **non commencé**.

## Ce qui est vérifié

- les branches `main` **et** `refrain` existent ;
- `refrain` est **fusionnée** dans `main` (le commit de merge est bien là) ;
- `chanson.txt` (vu depuis `main`) contient `on chante ensemble` : tu as gardé la bonne version ;
- il **ne reste aucun marqueur de conflit** (`<<<<<<<`, `=======`, `>>>>>>>`) dans tes fichiers.

## Conseils

- Pendant un conflit, `git status` liste les fichiers **non fusionnés** (*unmerged*). Tant qu'il en
  reste, la fusion n'est pas finie.
- Résoudre = obtenir le **texte final voulu** puis **retirer les trois marqueurs**. Le fichier doit
  redevenir un fichier normal.
- Paniqué au milieu d'un merge ? `git merge --abort` remet tout comme avant, tu peux recommencer.
- Reporte-toi au cours, section [conflits](cours.md#conflits).
