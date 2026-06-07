# Workflow de rendu

La piscine apprend le **vrai geste git/GitLab** : chaque rendu officiel passe par un `git push`.
Tout est **local** — un dépôt bare joue le rôle du « GitLab » de l'équipe.

## Mise en place — `piscine init`

Au premier lancement, `piscine init` crée :

- le **workspace** : l'espace de code de la recrue (un dépôt git de travail) ;
- un **dépôt bare local** (`~/.piscine`-style, sous l'état de la piscine) ajouté comme **`origin`** ;
- un **hook `post-receive`** dans le dépôt bare, qui déclenche la moulinette à chaque push ;
- la configuration du git bundlé (MinGit sous Windows) → la recrue tape de **vraies** commandes git.

## Deux boucles distinctes

| Commande | Rôle | Effet |
|---|---|---|
| `piscine check [exo]` | Itération rapide, sans commit | Feedback éducatif instantané. **Ne compte pas** comme rendu. |
| `git push origin main` | **Rendu officiel** (vrai geste GitLab) | Le hook lance la moulinette sur le commit reçu, affiche le feedback **et enregistre la progression**. |

> **Dans l'app de bureau**, ces deux boucles existent sans la ligne de commande : la page *Vérifier*
> rejoue `check` (diff/indice/lien cours), et le `git push` du rendu se fait au **terminal embarqué**
> (avec **coaching git**) ou à un terminal système. Le verdict riche du push (verdict + diff + indice +
> cours, par exercice) s'affiche dans *Résultat*, qui **s'auto-rafraîchit**.

## Déroulé d'un `git push`

1. Le hook `post-receive` du dépôt bare appelle `piscine grade-received <sha>`.
2. La moulinette **matérialise l'arbre du commit reçu** dans un dossier temporaire isolé.
3. Elle détecte les exercices **présents** dans le rendu, les corrige **par groupe, dans l'ordre,
   arrêt au premier échec** (suivants → *Non corrigé*). Voir [Moulinette](Moulinette). Les exercices
   **`git`** sont notés contre le **dépôt bare** (l'historique reçu), uniquement s'ils ont été « tentés ».
4. Le feedback éducatif est imprimé pendant le push **et** la progression est persistée (statut +
   verdict riche relu par la page *Résultat* de l'app).

> Le hook est un script `#!/bin/sh` portable ; il appelle le binaire `piscine` via le chemin
> réel résolu à l'installation. Sous Windows, il s'exécute via le `sh` de MinGit.

## Cycle de travail type

```bash
piscine start ex00-hello   # copie le starter dans le workspace
# ... la recrue code ...
piscine check              # feedback instantané, autant de fois qu'elle veut
git add .
git commit -m "ex00"
git push origin main       # rendu officiel → moulinette
```

## Suivi de progression

L'état local conserve le statut par exercice (*Réussi / À revoir / Non corrigé*), les tentatives et
le dernier feedback. `piscine status` et `piscine list` donnent la vue d'ensemble.
