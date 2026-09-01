# ex00 — Reporter un hotfix

Travaille dans le dépôt Git de l'exercice :

1. sur `main`, crée `alertes.txt` avec `seuil=100` et commite ;
2. crée `release-1.0` depuis cet état ;
3. crée `hotfix-alerte` depuis `main`, remplace le contenu par `seuil=90` et commite ;
4. fusionne `hotfix-alerte` dans `main` ;
5. fusionne aussi `hotfix-alerte` dans `release-1.0` ;
6. vérifie l'absence de conflit puis pousse les trois branches.

Le correctif doit rester un commit atteignable depuis les deux branches cibles. Ne duplique pas la
modification à la main dans deux commits différents.
