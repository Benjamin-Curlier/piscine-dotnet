# Module 05 — Git intermédiaire

Dans le **Module 00**, tu as appris la boucle de base : `git add`, `git commit`,
`git push origin main`. Tu sais donc déjà **enregistrer** ton travail et le **rendre**.

Ce module-ci va plus loin. On va apprendre à **travailler sur plusieurs versions en
parallèle** (les *branches*), à **réunir** ces versions (le *merge*), à **gérer les
disputes** entre deux versions (les *conflits*) et à **explorer puis défaire** des
changements sans tout casser.

> **Ce module ne se corrige pas par la moulinette.** Il n'y a pas d'exercice auto-noté.
> Tu pratiques sur **ton propre dépôt git**, à la main. Une notation dédiée aux opérations
> git arrivera plus tard, via un outil séparé. Pour l'instant : lis, tape les commandes,
> observe ce qui se passe. C'est en cassant (puis en réparant) qu'on apprend git.

Un peu de vocabulaire avant de commencer, pour que rien ne te surprenne :

- **dépôt** (*repository*, « repo ») : le dossier suivi par git, avec tout son historique.
- **commit** : une photo de ton projet à un instant T, avec un message qui la décrit.
- **HEAD** : un curseur qui pointe sur « là où tu es » dans l'historique (en général, le
  dernier commit de la branche courante).
- **branche** : une ligne de développement. On détaille juste en dessous.

---

## 1. Les branches {#branches}

### À quoi ça sert ?

Une **branche** est une **ligne de travail indépendante**. Imagine ton historique comme
une suite de commits :

```
A --- B --- C        (main)
```

Tu veux essayer une nouvelle idée — disons ajouter une fonctionnalité — **sans risquer de
casser** ce qui marche déjà sur `main`. Tu crées une branche. À partir de là, tes nouveaux
commits partent sur un chemin séparé :

```
A --- B --- C                (main)
             \
              D --- E         (ma-fonctionnalite)
```

`main` reste **intacte** pendant que tu bricoles sur `ma-fonctionnalite`. Si ton idée est
mauvaise, tu jettes la branche : `main` n'a jamais bougé. Si elle est bonne, tu la
**fusionnes** dans `main` (section 2).

> **L'image à retenir :** une branche n'est qu'une **étiquette qui pointe sur un commit**.
> Quand tu commits, l'étiquette de la branche courante avance toute seule sur le nouveau
> commit. C'est tout. C'est pour ça que créer une branche est instantané et gratuit.

### Les commandes

```bash
git branch                 # liste les branches ; un astérisque (*) marque la branche courante
git branch ma-fonctionnalite   # crée la branche, mais NE bascule PAS dessus
git switch ma-fonctionnalite   # bascule sur une branche existante
git switch -c ma-fonctionnalite  # crée la branche ET bascule dessus (le raccourci usuel)
git switch -                   # revient à la branche précédente (comme `cd -`)
```

Le plus souvent, tu utiliseras directement :

```bash
git switch -c ma-fonctionnalite
```

Le `-c` veut dire *create*. Tu crées et tu sautes dessus en une seule commande.

> **Note :** tu croiseras peut-être l'ancienne commande `git checkout` (`git checkout -b ...`
> pour créer une branche, `git checkout nom` pour basculer). Elle fonctionne encore, mais
> elle fait *trop de choses différentes*, ce qui prête à confusion. Git a depuis séparé ses
> rôles : `git switch` pour changer de branche, `git restore` pour restaurer des fichiers
> (section 4). **Préfère `switch` et `restore`** : c'est plus clair.

### Conventions de nommage

Le nom d'une branche doit dire **ce qu'on y fait**. Quelques règles communes en équipe :

- **minuscules**, mots séparés par des **tirets** : `ajout-login`, pas `Ajout Login`.
- **pas d'espaces ni d'accents** (ça évite les mauvaises surprises selon les systèmes).
- souvent un **préfixe** qui donne le type de travail :
  - `feat/recherche-produits` — une nouvelle fonctionnalité (*feature*) ;
  - `fix/division-par-zero` — une correction de bug ;
  - `docs/relecture-readme` — de la documentation.

Exemple : `feat/panier-achat`. On lit en un coup d'œil : « branche d'ajout de
fonctionnalité, le panier d'achat ».

---

## 2. Fusionner deux branches : le merge {#merge}

Tu as fini ta fonctionnalité sur `ma-fonctionnalite`. Tu veux **rapatrier** ce travail dans
`main`. C'est le rôle de **`git merge`**.

La règle d'or : on se place **sur la branche qui doit recevoir** le travail, puis on
fusionne l'autre **dedans**.

```bash
git switch main                 # 1. on se met sur la branche d'accueil
git merge ma-fonctionnalite     # 2. on y verse le travail de l'autre branche
```

Il existe **deux façons** dont git réalise cette fusion. Comprendre la différence t'évitera
bien des surprises en lisant l'historique.

### a) La fusion « fast-forward » (avance rapide)

Si `main` **n'a pas bougé** depuis que tu as créé ta branche, git n'a aucune décision à
prendre : il lui suffit de **faire glisser l'étiquette `main`** vers l'avant, jusqu'au
dernier commit de ta branche. Aucun commit n'est créé.

Avant le merge :

```
A --- B --- C            (main)
             \
              D --- E     (ma-fonctionnalite)
```

Après `git merge ma-fonctionnalite` (fast-forward) :

```
A --- B --- C --- D --- E     (main, ma-fonctionnalite)
```

L'historique reste une **belle ligne droite**. C'est le cas simple.

### b) Le commit de merge

Si **les deux branches ont avancé chacune de leur côté** (par exemple, quelqu'un a poussé
un commit sur `main` pendant que tu travaillais), git ne peut pas simplement avancer une
étiquette : il doit **réconcilier deux histoires**. Il crée alors un **commit de merge**
spécial, qui a **deux parents** (les deux têtes de branche).

Avant :

```
A --- B --- C --- F          (main)        <- F est arrivé après ton départ
             \
              D --- E         (ma-fonctionnalite)
```

Après `git merge ma-fonctionnalite` :

```
A --- B --- C --- F --- M     (main)
             \         /
              D --- E         (ma-fonctionnalite)
```

`M` est le **commit de merge**. Il scelle la réunion des deux lignes. Git ouvre souvent un
éditeur pour te faire confirmer son message (un texte du type `Merge branch
'ma-fonctionnalite'` est déjà rempli — tu peux valider tel quel).

> **Astuce mémoire :** *fast-forward* = la ligne droite, rien à réconcilier.
> *commit de merge* = un Y dans l'historique, parce que deux chemins se rejoignent.

Quand un merge touche **le même endroit dans un fichier** des deux côtés, git ne peut pas
deviner qui a raison : c'est un **conflit**. C'est le sujet de la section suivante — et ce
n'est **pas** une erreur de ta part, juste une étape normale.

---

## 3. Les conflits {#conflits}

### Pourquoi ça arrive

Un **conflit** survient quand deux branches ont modifié **les mêmes lignes du même
fichier** de façon différente. Git sait fusionner des changements qui ne se chevauchent
pas (chacun dans son coin du fichier), mais quand deux versions se disputent **la même
ligne**, il **refuse de choisir à ta place**. Il s'arrête et te demande de trancher.

Un conflit n'est **pas** un bug ni une faute. C'est git qui dit poliment : « je ne sais pas
laquelle de ces deux versions garder, dis-le moi ».

### Lire les marqueurs de conflit

Quand un merge conflicte, git **modifie le fichier concerné** pour y insérer les deux
versions, encadrées par des **marqueurs**. Ça ressemble à ça :

```text
Bonjour,
<<<<<<< HEAD
Voici la version qui est sur MA branche actuelle (là où je suis).
=======
Voici la version qui vient de la branche que je fusionne.
>>>>>>> ma-fonctionnalite
À bientôt.
```

Décodons les marqueurs :

- `<<<<<<< HEAD` : début de **ta** version (celle de la branche courante).
- `=======` : la **frontière** entre les deux versions.
- `>>>>>>> ma-fonctionnalite` : fin de la version **entrante** (la branche fusionnée).

Tout ce qui est **entre** `<<<<<<<` et `=======` vient de chez toi. Tout ce qui est entre
`=======` et `>>>>>>>` vient de l'autre branche.

### Résoudre un conflit

Résoudre, c'est **éditer le fichier à la main** pour obtenir le texte final que tu veux,
puis **supprimer les trois marqueurs**. Tu peux :

- garder ta version,
- garder l'autre version,
- **mélanger les deux**, ou écrire une troisième version qui combine les deux idées.

Par exemple, on décide de garder un mélange. Le fichier devient simplement :

```text
Bonjour,
Voici la version finale, qui combine les deux idées.
À bientôt.
```

Plus aucun `<<<<<<<`, `=======` ni `>>>>>>>` : le fichier doit redevenir un fichier
**normal**, tel que tu veux le livrer.

Ensuite, tu **marques le conflit comme résolu** avec `git add`, puis tu **conclus** le
merge avec un commit :

```bash
# ... tu as édité le ou les fichiers en conflit ...
git add fichier-en-conflit.txt   # dit à git : "ce fichier-là est réglé"
git status                       # vérifie qu'il ne reste plus de conflit
git commit                       # finalise le commit de merge (message déjà pré-rempli)
```

> **Vérifie toujours avec `git status`** pendant un conflit : git te liste les fichiers
> « non fusionnés » (*unmerged*) restants. Tant qu'il en reste, le merge n'est pas terminé.

> **Au secours, je veux tout annuler !** Si tu paniques au milieu d'un merge, tu peux
> revenir à l'état d'avant la commande avec :
> ```bash
> git merge --abort
> ```
> Git remet tout comme avant le `git merge`. Rien n'est perdu, tu peux recommencer
> tranquillement.

---

## 4. Explorer l'historique et revenir en arrière {#historique}

Git garde **toute** ton histoire. Savoir la **lire** et la **défaire prudemment** est une
compétence à part entière.

### Lire l'historique

```bash
git log                          # historique complet (q pour quitter l'affichage)
git log --oneline                # une ligne par commit : court et lisible
git log --oneline --graph        # ajoute un schéma ASCII des branches et merges
git log --oneline --graph --all  # ... pour TOUTES les branches, pas seulement la courante
```

`git log --oneline --graph --all` est la commande à connaître par cœur. Elle dessine
exactement les schémas qu'on a vus plus haut, directement dans ton terminal :

```text
*   9f3a1c2 Merge branch 'ma-fonctionnalite'
|\
| * 7b2d4e1 ajoute le calcul du total
| * 3c8f0a9 cree le panier
* | 1a5e6b3 corrige la page d'accueil
|/
* 0d4c2f8 version initiale
```

Chaque ligne est un commit ; le `*` marque sa position ; les `|` et `\` `/` montrent les
branches et leur fusion.

### Voir ce qui a changé

```bash
git diff                 # changements PAS encore ajoutés (dans ton dossier de travail)
git diff --staged        # changements DÉJÀ ajoutés (prêts à être commités)
git show                 # affiche le dernier commit : message + diff complet
git show 7b2d4e1         # affiche un commit précis (on prend son identifiant dans le log)
```

`git diff` répond à « qu'est-ce que j'ai modifié ? », `git show` répond à « qu'est-ce que ce
commit-là contenait ? ».

### Revenir en arrière — du plus doux au plus dangereux

Il y a **plusieurs** façons de défaire, et elles ne se valent pas du tout. Choisis la plus
douce qui répond à ton besoin.

**`git restore` — annuler des modifications NON commitées**

Tu as modifié un fichier, tu n'as pas encore commité, et tu veux **jeter** ces
modifications pour revenir à la dernière version commitée :

```bash
git restore fichier.cs       # rétablit fichier.cs tel qu'au dernier commit
git restore --staged fichier.cs   # retire fichier.cs de la zone d'ajout (sans toucher son contenu)
```

> `git restore fichier.cs` **détruit** tes modifications non sauvegardées de ce fichier.
> C'est sans danger pour l'historique, mais le travail non commité de ce fichier est perdu.

**`git revert` — annuler un commit DÉJÀ partagé (la méthode sûre)**

Tu as commité (et peut-être poussé) quelque chose, et tu veux l'**annuler**. La bonne
méthode est `git revert` : elle **crée un nouveau commit** qui fait l'**inverse** du commit
visé. L'historique n'est **pas réécrit** — on ajoute simplement une « annulation » par
dessus.

```bash
git revert 7b2d4e1       # crée un commit qui défait les changements de 7b2d4e1
```

C'est **la** méthode à utiliser dès que le commit a été partagé (poussé sur `origin`), parce
qu'elle ne change pas l'histoire des autres.

**`git reset` — déplacer le curseur (PUISSANT et DANGEREUX)**

`git reset` **recule** la branche sur un commit antérieur, ce qui **fait disparaître** les
commits situés après. Selon l'option, ça touche aussi tes fichiers :

```bash
git reset --soft HEAD~1   # annule le dernier commit, GARDE les changements (prêts à recommiter)
git reset --mixed HEAD~1  # annule le dernier commit, garde les changements mais les "désajoute"
git reset --hard HEAD~1   # annule le dernier commit ET SUPPRIME les changements (irréversible)
```

(`HEAD~1` signifie « un commit avant HEAD », `HEAD~2` deux commits avant, etc.)

> **AVERTISSEMENT — `git reset`, surtout `--hard`.**
>
> - `git reset --hard` **détruit** du travail sans confirmation. Ce qui n'était pas commité
>   est **définitivement perdu**.
> - **Ne fais JAMAIS de `reset` sur des commits que tu as déjà poussés** et que d'autres
>   ont peut-être récupérés : tu **réécris une histoire partagée**, ce qui casse le dépôt de
>   tes coéquipiers. Pour défaire un commit déjà poussé, utilise **`git revert`**.
> - Tant que tu débutes, considère `reset --hard` comme un outil **à éviter**. `restore` et
>   `revert` couvrent presque tous tes besoins, **sans risque**.

**Règle simple à retenir :**

| Situation | Commande |
|---|---|
| Jeter des modifications **non commitées** d'un fichier | `git restore fichier` |
| Annuler un commit **déjà poussé / partagé** | `git revert <commit>` |
| Réorganiser des commits **locaux, non poussés** (avec prudence) | `git reset` |

---

## 5. Checklist pratique — à reproduire sur TON dépôt {#checklist}

Voici un parcours complet à faire **toi-même**, dans n'importe quel dépôt git de test (tu
peux en créer un vide : `mkdir bac-a-sable && cd bac-a-sable && git init`). Le but est de
**vivre** chaque notion de ce module, conflit compris.

Suis les étapes dans l'ordre, et **observe** la sortie de chaque commande.

```bash
# --- Préparation : un premier commit sur main ---
echo "ligne 1 : titre" > notes.txt
git add notes.txt
git commit -m "version initiale"

# --- 1. Créer une branche et y faire 2 commits ---
git switch -c ma-fonctionnalite     # crée + bascule
echo "ligne 2 : ajout depuis la branche" >> notes.txt
git add notes.txt
git commit -m "commit 1 sur la branche"

echo "ligne 3 : encore un ajout" >> notes.txt
git add notes.txt
git commit -m "commit 2 sur la branche"

git log --oneline --graph --all     # observe : main est en arrière, ta branche a 2 commits de plus

# --- 2. Fusionner la branche dans main (fast-forward) ---
git switch main
git merge ma-fonctionnalite         # main n'a pas bougé -> fusion en avance rapide
git log --oneline --graph --all     # observe : une belle ligne droite
```

Maintenant, **provoque un conflit volontaire**. L'idée : modifier **la même ligne** sur deux
branches différentes, puis fusionner.

```bash
# --- 3. Préparer le terrain du conflit ---
git switch -c experience            # nouvelle branche à partir de main
# modifie la ligne 1 sur cette branche :
echo "ligne 1 : titre MODIFIE PAR experience" > notes.txt
echo "ligne 2 : ajout depuis la branche" >> notes.txt
echo "ligne 3 : encore un ajout" >> notes.txt
git add notes.txt
git commit -m "modifie le titre depuis experience"

# reviens sur main et modifie LA MEME ligne 1, differemment :
git switch main
echo "ligne 1 : titre MODIFIE PAR main" > notes.txt
echo "ligne 2 : ajout depuis la branche" >> notes.txt
echo "ligne 3 : encore un ajout" >> notes.txt
git add notes.txt
git commit -m "modifie le titre depuis main"

# --- 4. Provoquer le conflit ---
git merge experience                # BOUM : conflit sur la ligne 1 de notes.txt
git status                          # git liste notes.txt comme "non fusionne" (unmerged)
```

Ouvre `notes.txt` dans ton éditeur : tu vois les marqueurs `<<<<<<<`, `=======`,
`>>>>>>>`. **Résous-les** : choisis le titre que tu veux, **supprime les trois marqueurs**,
puis termine la fusion.

```bash
# --- 5. Resoudre puis conclure ---
# (tu as edite notes.txt a la main et retire tous les marqueurs)
git add notes.txt                   # marque le conflit comme resolu
git status                          # verifie : plus aucun fichier "unmerged"
git commit                          # finalise le commit de merge (valide le message propose)

git log --oneline --graph --all     # observe le Y du commit de merge dans le schema
```

Si tu as réussi à voir le **fast-forward**, le **conflit**, ses **marqueurs**, puis le
**commit de merge** dans ton `git log --graph` : bravo, tu maîtrises l'essentiel du git
quotidien en équipe.

> **Bonus :** entraîne-toi aussi à **défaire**. Fais un commit « bidon », puis annule-le avec
> `git revert HEAD` et regarde le commit d'annulation apparaître dans le log. C'est le
> réflexe sûr à avoir.

---

## Références externes {#references}

Pour approfondir, voici des ressources fiables et gratuites :

- **Microsoft Learn — Git & contrôle de version** (en français) :
  <https://learn.microsoft.com/fr-fr/training/paths/intro-to-vc-git/>
- **Atlassian — Tutoriels Git** (branches, merge, conflits, très bien illustré) :
  <https://www.atlassian.com/fr/git/tutorials>
- **Pro Git** (le livre de référence, gratuit, en français — chapitre 3 « Les branches
  avec Git ») : <https://git-scm.com/book/fr/v2>
- **Documentation officielle des commandes Git** :
  <https://git-scm.com/docs>
- **Vidéo — « Git & GitHub Crash Course » (freeCodeCamp, YouTube)** : une prise en main
  visuelle de bout en bout : <https://www.youtube.com/watch?v=RGOj5yH7evk>

Prends le temps de **manipuler** : git ne s'apprend pas en lisant, mais en tapant les
commandes et en observant ce que raconte `git log --oneline --graph --all`.
