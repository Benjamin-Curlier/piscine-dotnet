# ex02 — Consommateur idempotent

Lis `N`, puis `N` lignes `messageId montant`. Une redélivrance conserve le même identifiant. Le
montant ne doit être appliqué qu'à la **première** apparition d'un identifiant, même si une occurrence
ultérieure contient une autre valeur.

Affiche le total appliqué puis le nombre de doublons :

```text
TOTAL 20
DOUBLONS 2
```

Utilise un `HashSet<string>` pour représenter l'inbox locale.
