# ex01 — Adaptateur gRPC

Implémente `CommandGrpcService` dérivé de la base générée. `Submit` refuse une clé d'idempotence vide
avec le statut `InvalidArgument`, observe la deadline, transmet `context.CancellationToken` au handler
et retourne l'identifiant dans `SubmitCommandReply`.

Ne remplace pas l'annulation par `CancellationToken.None` et ne masque pas toutes les exceptions.
