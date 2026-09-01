# Rush 6 — Asteria distribué

Dernière mission : produire la topologie de développement et le protocole de validation d'un système
composé d'une API, d'un worker et de NATS JetStream.

## AppHost.cs

- conteneur `nats:2.11-alpine`, argument `-js`, volume durable ;
- projets `Projects.Api` et `Projects.Worker` ;
- chacun utilise `WithReference(nats)` et `WaitFor(nats)` ;
- l'API expose un health check HTTP `/health` dans le modèle ;
- aucune adresse `localhost` ou chaîne contenant un mot de passe.

## ServiceDefaults.cs

Crée une extension `AddServiceDefaults` qui rassemble OpenTelemetry, service discovery, client HTTP
avec `AddStandardResilienceHandler` et health checks. `MapDefaultEndpoints` expose `/health` et
`/alive`. Aucun `Thread.Sleep` ne simule la disponibilité.

## OPERATIONS.md

Écris un runbook concret avec les sections :

- `## Scénario nominal` ;
- `## Coupure du worker` ;
- `## Coupure de NATS` ;
- `## Reprise`.

Pour chaque scénario, indique l'action, le signal observé et le critère de réussite. Le document doit
expliquer comment suivre un `correlationId` dans une trace, observer la redelivery, détecter un
duplicate sans double effet, et distinguer readiness de simple processus vivant.

La moulinette vérifie le contrat des trois fichiers. Exécute ensuite réellement ce runbook avec
`aspire run` : la preuve à chaud dépend de ton environnement et ne peut pas être simulée par le texte.
