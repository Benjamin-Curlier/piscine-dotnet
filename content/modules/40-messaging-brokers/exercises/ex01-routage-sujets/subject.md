# ex01 — Router des subjects

La première ligne contient un abonnement, puis `N` et `N` subjects. Affiche, dans l'ordre, les
subjects qui correspondent.

- un segment littéral doit être identique ;
- `*` correspond à exactement un segment ;
- `>` apparaît uniquement en dernier et correspond à **au moins un** segment restant ;
- sans wildcard, le nombre de segments doit être identique.

Écris une méthode `bool Correspond(string abonnement, string sujet)`.
