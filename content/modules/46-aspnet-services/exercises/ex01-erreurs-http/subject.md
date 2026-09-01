# ex01 — Erreurs contractuelles

Définis `CommandError` pour validation, absence et conflit. Une méthode de mapping renvoie un
`ProblemHttpResult` typé avec les statuts 400, 404 et 409. Chaque problème contient un `traceId` dans
ses extensions.

N'expose jamais stack trace, exception complète ou chaîne de connexion.
