# ex02 — Page déterministe

Écris une lecture EF Core sans tracking. Filtre par statut et, si fourni, après le curseur temporel.
Trie par date puis identifiant, projette vers `CommandSummary` et demande une ligne de plus que la
taille afin de calculer `hasMore` et `nextCursor`.

La requête reste côté SQL jusqu'à `ToListAsync(CancellationToken)` ; `Skip(page * taille)` est exclu.
