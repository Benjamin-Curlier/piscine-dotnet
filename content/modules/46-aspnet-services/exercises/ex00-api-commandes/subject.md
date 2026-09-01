# ex00 — API de commandes

Complète une Minimal API .NET 10 : active `ProblemDetails` et OpenAPI, publie le document seulement en
développement, expose `/health` et accepte `POST /commands`.

Le handler reçoit un DTO et un `CancellationToken`, appelle `ICommandBus` puis répond `202 Accepted`
avec l'URI et l'identifiant de suivi. Aucune adresse locale ne doit être codée en dur.
