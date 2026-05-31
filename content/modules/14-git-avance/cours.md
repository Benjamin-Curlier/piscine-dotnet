# Module 14 — Git avancé & collaboration

Tu sais déjà **committer** et **pousser** ton travail (modules 00 et 05). Ce module n'introduit
**aucun exercice auto-noté** : c'est un module de **lecture et de pratique guidée**. Le but est de
te rendre à l'aise dans le travail **en équipe**, là où git prend tout son sens : plusieurs
personnes modifient le même dépôt, et il faut intégrer ces changements **proprement**.

On va voir comment **réorganiser** ton historique avec `rebase`, comment proposer ton travail via
une **Merge Request** (GitLab) ou une **Pull Request** (GitHub), comment **relire** le code d'un·e
collègue de façon constructive, et quelques **outils** qui te sauveront la mise (`stash`,
`cherry-pick`, `tag`, résolution de conflits).

> 💡 Tout ce qui suit se pratique sur un **dépôt jouet**. Crée un dossier de test, fais-y quelques
> commits sur des fichiers `.txt`, et essaie chaque commande sans risque. On apprend git en se
> trompant dans un coin sûr, pas sur le dépôt de l'équipe.

## 1. Rebase : réorganiser son historique {#rebase}

### 1.1 Merge vs rebase : deux façons d'intégrer

Imagine que tu as créé une branche `feature` à partir de `main`, puis que `main` a avancé
pendant que tu travaillais :

```
        A---B---C  feature
       /
  D---E---F---G    main
```

Tu veux récupérer les nouveautés de `main` (F, G) dans ta branche. Deux options :

**Le merge** crée un **commit de fusion** (`M`) qui réunit les deux histoires :

```bash
git switch feature
git merge main
```

```
        A---B---C-------M  feature
       /               /
  D---E---F---G--------/   main
```

**Le rebase** **rejoue** tes commits A, B, C **par-dessus** le sommet de `main`, comme si tu
avais commencé ta branche *après* G :

```bash
git switch feature
git rebase main
```

```
                    A'--B'--C'  feature
                   /
  D---E---F---G            main
```

- Le **merge** préserve l'histoire telle qu'elle s'est réellement passée, mais multiplie les
  commits de fusion : l'historique ressemble vite à un plat de spaghettis.
- Le **rebase** produit un historique **linéaire**, lisible « comme un livre », au prix d'une
  **réécriture** : A, B, C deviennent A', B', C' — ce sont de **nouveaux** commits (nouveaux
  identifiants), même si le contenu est identique.

> 🧠 Règle simple à retenir : **merge = on ajoute un nœud**, **rebase = on réécrit les commits**.

### 1.2 Réécrire l'historique local

`rebase` ne sert pas qu'à se mettre à jour : il permet aussi de **nettoyer** ta branche avant de
la partager. Le rebase **interactif** ouvre un éditeur listant tes commits :

```bash
git rebase -i main
```

Tu peux alors, ligne par ligne :

- `pick` : garder le commit tel quel ;
- `reword` : garder le commit mais **corriger son message** ;
- `squash` / `fixup` : **fusionner** un commit dans le précédent (pratique pour regrouper
  un « oops, faute de frappe » avec le vrai commit) ;
- `drop` : **supprimer** un commit ;
- réordonner les lignes pour changer l'ordre des commits.

Résultat : tu présentes à l'équipe une suite de commits **propres et atomiques** plutôt que la
trace brute de tes tâtonnements (« wip », « test », « ça marche enfin »).

### 1.3 La règle d'or : ne jamais rebaser ce qui est publié {#regle-dor}

Comme le rebase **réécrit** les commits (nouveaux identifiants), il **réécrit l'histoire**. C'est
sans danger tant que ces commits ne vivent **que chez toi**. Mais si tu rebases des commits
**déjà poussés et partagés**, tu crées une histoire divergente : les collègues qui avaient les
anciens commits se retrouvent avec un dépôt incohérent, et il faudra un `push --force` qui peut
**écraser le travail des autres**.

> ⚠️ **Règle d'or du rebase** : on **réécrit librement** l'historique **local** (pas encore
> poussé) ; on ne rebase **jamais** une branche **publique/partagée** (comme `main`) sur laquelle
> d'autres travaillent.

En pratique : rebase ta **branche de feature perso** autant que tu veux **avant** d'ouvrir la
MR ; une fois la branche partagée et relue, **évite** de la rebaser sauvagement.

### 1.4 `git pull --rebase`

Un `git pull` classique = `git fetch` (récupérer) + `git merge` (fusionner). Si toi et un·e
collègue avez commité chacun de votre côté, ça crée un petit commit de fusion à chaque pull,
ce qui pollue l'historique.

L'alternative :

```bash
git pull --rebase
```

= `git fetch` + `git rebase` : tes commits locaux sont **rejoués au-dessus** de ce que tu viens
de récupérer. L'historique reste **linéaire**, sans commit de fusion parasite. Ce sont **tes**
commits locaux (non encore poussés) qui sont rebasés : la règle d'or est respectée.

Pour en faire le comportement par défaut une fois pour toutes :

```bash
git config --global pull.rebase true
```

## 2. Workflow de Merge Request (GitLab) {#merge-request}

Dans une équipe, on ne pousse **jamais** directement sur `main`. On passe par une **Merge
Request** (MR) : une demande d'intégrer ta branche dans `main`, **relue** avant d'être acceptée.
Voici le cycle complet.

**Étape 1 — Créer une branche de feature.** Pars toujours d'une `main` à jour :

```bash
git switch main
git pull --rebase            # se mettre à jour
git switch -c feat/calcul-tva   # créer + basculer sur la nouvelle branche
```

> 🏷️ Nomme tes branches de façon parlante : `feat/...` (nouvelle fonctionnalité),
> `fix/...` (correction), `docs/...` (documentation). Une branche = un sujet.

**Étape 2 — Travailler et committer.** Fais des commits **petits et atomiques** (voir §3.4) :

```bash
git add .
git commit -m "Ajoute le calcul de la TVA à 20 %"
```

**Étape 3 — Pousser la branche.** La première fois, on déclare la branche distante :

```bash
git push -u origin feat/calcul-tva
```

Le `-u` (`--set-upstream`) relie ta branche locale à la branche distante : les `git push` et
`git pull` suivants n'auront plus besoin d'arguments.

**Étape 4 — Ouvrir la MR.** Sur l'interface GitLab, un bouton **« Create merge request »**
apparaît après le push. Tu choisis la branche **source** (`feat/calcul-tva`) et la branche
**cible** (`main`), tu donnes un **titre clair** et une **description** : *quoi*, *pourquoi*,
*comment tester*.

**Étape 5 — Discussion et revue.** Les collègues commentent ligne par ligne (voir §3). Tu
réponds, tu corriges, tu repousses sur la **même branche** : la MR se **met à jour
automatiquement**. C'est un dialogue, pas un examen.

**Étape 6 — Intégration.** Une fois la MR **approuvée** et la CI **verte**, on clique sur
**« Merge »**. GitLab propose souvent des options utiles :

- **Squash commits** : compacter tous les commits de la branche en un seul sur `main` ;
- **Delete source branch** : supprimer la branche de feature, désormais inutile.

```bash
# Après l'intégration, on nettoie localement :
git switch main
git pull --rebase
git branch -d feat/calcul-tva   # supprime la branche locale fusionnée
```

> 🔁 **GitLab MR ≈ GitHub PR.** Une **Pull Request** (PR) sur GitHub, c'est exactement la même
> idée et le même cycle (brancher → pousser → ouvrir → relire → fusionner). Seul le vocabulaire
> change : GitLab dit « Merge Request », GitHub dit « Pull Request ». Les concepts sont
> identiques, tu sauras passer de l'un à l'autre.

## 3. La revue de code {#revue}

La **revue de code** (*code review*) est le cœur de la collaboration : avant qu'un changement
entre dans `main`, au moins une autre personne le lit. Ce n'est **pas** un jugement de la
personne, c'est une **amélioration partagée** du code et une diffusion des connaissances.

### 3.1 Que regarder en relisant

- **Correction** : le code fait-il ce que la MR annonce ? Les cas limites sont-ils gérés ?
- **Lisibilité** : noms clairs, code compréhensible sans explication orale ?
- **Tests** : y a-t-il des tests, couvrent-ils l'essentiel ?
- **Cohérence** : le style respecte-t-il les conventions du projet ?
- **Simplicité** : peut-on faire plus simple ? Pas de complexité inutile (*YAGNI*).

### 3.2 Commenter de façon constructive

Un bon commentaire de revue est **précis**, **bienveillant** et **orienté solution** :

- ✅ « Ici, `montant` peut être négatif si l'entrée est vide. On pourrait ajouter une garde
  `if (montant < 0)` ? »
- ❌ « C'est faux. »

Quelques principes :

- **Vise le code, jamais la personne** : « cette fonction » plutôt que « tu as mal fait ».
- **Distingue le bloquant du souhaitable.** Préfixe les remarques mineures par `nit:`
  (*nitpick*, détail non bloquant) pour ne pas paralyser la MR.
- **Pose des questions** plutôt que des ordres : ça ouvre la discussion.
- **Souligne aussi le positif** : « belle simplification ici ! » motive autant qu'une critique.

### 3.3 Faire de petites MR

Une MR de 50 lignes se relit en profondeur en 10 minutes. Une MR de 2 000 lignes reçoit un
« LGTM » (*Looks Good To Me*) distrait : personne ne relit vraiment. **Découpe** ton travail en
petites MR ciblées : la revue est meilleure, les retours arrivent plus vite, et le risque de
conflit diminue.

### 3.4 Commits atomiques & messages clairs (rappel) {#commits}

Un **commit atomique** = **un seul changement cohérent**. On ne mélange pas « renommer une
variable » et « ajouter une fonctionnalité » dans le même commit : si l'un pose problème, on
peut annuler l'autre sans tout casser.

Un bon **message de commit** explique *pourquoi*, pas seulement *quoi* :

```
Corrige le calcul de TVA pour les montants à zéro

La division renvoyait NaN quand le total valait 0. On retourne
maintenant 0 explicitement avant le calcul.
```

- **Première ligne** : un résumé court (≈ 50 caractères), à l'**impératif présent**
  (« Ajoute… », « Corrige… »), sans point final.
- **Ligne vide**, puis un **corps** facultatif qui explique le contexte et la raison.

> 📌 Commits atomiques + messages clairs = un historique qui se lit, des MR faciles à relire,
> et un `git revert` propre le jour où il faut annuler un changement.

## 4. Outils utiles au quotidien {#outils}

### 4.1 `git stash` — mettre de côté temporairement {#stash}

Tu es en plein milieu d'une modification et tu dois **changer de branche en urgence**, mais git
refuse parce que ton travail n'est pas commité. `stash` met tes changements **de côté** et
rend ton répertoire propre :

```bash
git stash               # range les modifications en cours (suivies)
git switch main         # tu peux changer de branche, faire un fix urgent...
git switch -            # reviens sur ta branche précédente
git stash pop           # ré-applique les modifications mises de côté
```

Autres commandes utiles : `git stash list` (voir la pile), `git stash drop` (jeter une entrée).
Le stash est un **brouillon temporaire**, pas un système de sauvegarde : sors-en vite.

### 4.2 `git cherry-pick` — rapatrier un commit précis {#cherry-pick}

`cherry-pick` **rejoue un commit donné** sur ta branche courante, sans fusionner toute la branche
d'où il vient. Pratique pour appliquer **un correctif urgent** déjà fait ailleurs :

```bash
git cherry-pick a1b2c3d        # applique le commit a1b2c3d ici
```

Comme pour le rebase, le commit rejoué reçoit un **nouvel identifiant**.

### 4.3 `git tag` — marquer une version {#tag}

Un **tag** est une **étiquette** posée sur un commit, typiquement pour marquer une **version
livrée**. Contrairement à une branche, il ne bouge plus :

```bash
git tag -a v1.0.0 -m "Première version stable"   # tag annoté
git push origin v1.0.0                            # les tags ne sont pas poussés par défaut
git tag                                           # liste les tags
```

Les noms suivent souvent le **versionnage sémantique** (`MAJEUR.MINEUR.CORRECTIF`, ex. `v1.4.2`).

### 4.4 Résoudre un conflit pendant un rebase {#conflits}

Un **conflit** survient quand deux changements touchent **les mêmes lignes**. Pendant un rebase,
git s'arrête sur le commit en conflit et marque le fichier ainsi :

```
<<<<<<< HEAD
montant = prix * 1.20;      // version déjà sur main
=======
montant = prix * tauxTva;   // ta version
>>>>>>> (ton commit)
```

Tu **édites** le fichier pour ne garder que la bonne version (en supprimant les marqueurs
`<<<<<<<`, `=======`, `>>>>>>>`), puis :

```bash
git add fichier-resolu.cs   # marque le conflit comme résolu
git rebase --continue       # poursuit le rebase avec les commits suivants
```

Deux issues de secours :

```bash
git rebase --skip      # ignore le commit qui pose problème (rare)
git rebase --abort     # tout annuler et revenir à l'état d'avant le rebase
```

> 🛟 En cas de panique pendant un rebase, `git rebase --abort` te ramène **exactement** à l'état
> de départ. Tu ne casses rien : tu peux toujours recommencer tranquillement.

## 5. Checklist pratique {#checklist}

Reproduis un **cycle complet** sur ton dépôt jouet (idéalement **en binôme**, chacun sur un
poste, en partageant un même dépôt distant) :

1. [ ] Sur `main` à jour, crée une branche : `git switch -c feat/mon-sujet`.
2. [ ] Fais **2 ou 3 commits atomiques** avec des messages clairs.
3. [ ] Nettoie ta branche avec un rebase interactif : `git rebase -i main`
       (essaie un `reword` et un `squash`).
4. [ ] Pousse : `git push -u origin feat/mon-sujet`.
5. [ ] Ouvre une **Merge Request** (ou Pull Request) vers `main`, avec titre et description.
6. [ ] Demande à ton binôme de **relire** et de laisser **au moins un commentaire** constructif.
7. [ ] Réponds, corrige, repousse : observe la **MR se mettre à jour**.
8. [ ] Provoque un **conflit** volontairement (modifie la même ligne sur `main` et sur ta
       branche), puis **résous-le** lors d'un `git rebase main`.
9. [ ] Une fois approuvée, **fusionne** la MR, puis nettoie : `git branch -d feat/mon-sujet`.
10. [ ] Pose un **tag** sur `main` : `git tag -a v0.1.0 -m "Premier jet"`.

Quand tu as fait tout ça **sans regarder ce cours**, tu maîtrises l'essentiel de la
collaboration git. 🎉

## Références externes {#references}

- **Atlassian** — *Merging vs Rebasing* (clair, illustré) :
  <https://www.atlassian.com/git/tutorials/merging-vs-rebasing>
- **Atlassian** — *Tutoriels Git* (en français) :
  <https://www.atlassian.com/fr/git/tutorials>
- **GitLab Docs** — *Merge requests* :
  <https://docs.gitlab.com/ee/user/project/merge_requests/>
- **GitHub Docs** — *À propos des pull requests* (en français) :
  <https://docs.github.com/fr/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests>
- **Pro Git** (livre de référence, gratuit, en français) — chapitre *Les branches avec Git* et
  *Rebaser* : <https://git-scm.com/book/fr/v2>
- **Vidéo** — *Git Rebase* (freeCodeCamp / fireship, intro visuelle au rebase) :
  <https://www.youtube.com/watch?v=f1wnYdLEpgI>
