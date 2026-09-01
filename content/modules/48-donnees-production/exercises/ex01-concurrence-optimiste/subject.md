# ex01 — Conflit explicite

Ajoute un token `Version` marqué `ConcurrencyCheck` à l'entité. `SaveAsync` incrémente cette version,
appelle `SaveChangesAsync` avec le token reçu et renvoie `Saved`.

Si EF Core lève précisément `DbUpdateConcurrencyException`, retourne `Conflict`. Ne masque pas les
autres erreurs et ne recrée pas le schéma à l'exécution.
