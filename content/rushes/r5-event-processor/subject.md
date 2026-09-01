# Rush 5 — Processeur d'événements résilient

Construis le processeur d'ordres du centre Asteria en réutilisant plusieurs concepts du parcours :
contrat JSON, ports/adaptateurs, async, idempotence et audit structuré.

## Entrée

La première ligne contient `N`, suivie de `N` événements JSON :

```json
{"id":"e1","type":"credit","amount":10}
```

`type` vaut `credit` ou `debit`, et `amount` doit être strictement positif.

## Comportement

Pour chaque événement :

1. contrat invalide : `REJECTED <id>` ; il n'est pas enregistré dans l'inbox ;
2. identifiant déjà appliqué : `DUPLICATE <id>` ; aucun nouvel effet ;
3. sinon, enregistre l'id, applique le crédit/débit et affiche
   `APPLIED <id> BALANCE <balance>`.

Après tous les événements, affiche `FINAL <balance>`. Un débit peut rendre le solde négatif.

## Architecture notée

- `Domain` : `EventMessage`, `IInbox`, `IAuditSink`, sans dépendance externe ;
- `Application` : `EventProcessor`, injecté avec les deux ports, méthode
  `ProcessAsync(EventMessage, CancellationToken)` ;
- `Infrastructure` : `MemoryInbox` avec `HashSet<string>` et `ConsoleAuditSink` ;
- `Program.cs` : composition, lecture et `JsonSerializer.Deserialize<EventMessage>`.

`Application` ne référence jamais `Infrastructure`. La méthode asynchrone propage et observe le
token même si l'implémentation en mémoire se termine immédiatement.
