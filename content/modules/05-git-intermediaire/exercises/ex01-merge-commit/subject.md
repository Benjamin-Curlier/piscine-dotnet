# Fusion avec commit de merge

L'exercice précédent fusionnait en **avance rapide** (*fast-forward*) : une ligne droite. Ici, tu vas
provoquer l'**autre** type de fusion — le **commit de merge** — celui qui apparaît quand **les deux
branches ont avancé chacune de leur côté**. C'est le cas le plus courant en équipe.

Comme au premier exercice, **aucun fichier de code à rendre** : la moulinette **inspecte l'état de
ton dépôt** (branches, commits, fusion).

## Ce qu'on attend de ton dépôt

À partir d'un dépôt qui contient déjà au moins un commit sur `main` :

1. **Crée une branche** `dev` et place-toi dessus, puis ajoute un premier fichier :
   ```bash
   git switch -c dev
   echo "module A" > module-a.txt
   git add module-a.txt
   git commit -m "ajout module A"
   ```
2. **Reviens sur `main`** et fais-y un commit **différent** (c'est ça qui fait *diverger* les branches) :
   ```bash
   git switch main
   echo "module B" > module-b.txt
   git add module-b.txt
   git commit -m "ajout module B"
   ```
3. **Fusionne** `dev` dans `main`. Comme `main` a avancé entre-temps, git ne peut pas faire d'avance
   rapide : il crée un **commit de merge** à deux parents (il te propose un message, valide-le) :
   ```bash
   git merge dev
   ```

## Rendre ton travail

La moulinette inspecte le dépôt reçu (`origin`). Pousse **toutes** tes branches :

```bash
git push origin --all
```

> Tant que la branche `dev` n'est pas poussée, l'exercice est considéré comme **non commencé**.

## Ce qui est vérifié

- les branches `main` **et** `dev` existent ;
- `main` contient **au moins 4 commits** (init + les deux commits + le commit de merge) ;
- `dev` est bien **fusionnée** dans `main` ;
- `module-a.txt` **et** `module-b.txt` sont présents (vus depuis `main`) : la fusion a bien réuni les
  deux lignes de travail ;
- il **ne reste aucun marqueur de conflit** dans tes fichiers.

## Conseils

- Le **commit de merge** n'apparaît que si `main` **et** `dev` ont chacune un commit après le point de
  séparation. Si tu oublies le commit sur `main`, tu obtiendras une simple avance rapide (pas de commit
  de merge).
- Visualise le « Y » de la fusion : `git log --oneline --graph --all`.
- Reporte-toi au cours, section [merge](cours.md#merge) (partie « commit de merge »).
