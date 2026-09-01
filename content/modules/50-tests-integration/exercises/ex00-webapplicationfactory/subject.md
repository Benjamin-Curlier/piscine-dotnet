# ex00 — Pipeline HTTP testé

Écris une classe xUnit utilisant `WebApplicationFactory<Program>`. Trois tests envoient réellement
une requête au pipeline : commande valide et authentifiée → 202, DTO invalide → 400, absence
d'identité → 401.

Utilise le client de la factory et des tokens d'annulation. Aucune adresse réseau ni attente
artificielle ne doit apparaître.
