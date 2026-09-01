# ex00 — Agréger des traces

Lis `N`, puis `N` spans `traceId service duree statut`, où le statut vaut `OK` ou `ERROR`. Pour
chaque trace, additionne les durées et compte les erreurs. Affiche les traces triées par identifiant :

```text
t1 DUREE 31 ERREURS 1
```

L'ordre d'arrivée des spans n'est pas garanti. Le nom du service sert au contexte mais n'entre pas
dans l'agrégat demandé.
