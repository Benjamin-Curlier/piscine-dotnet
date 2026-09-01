# ex01 — Service Defaults

Complète `Extensions.cs` avec deux extensions :

- `AddServiceDefaults` appelle `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`,
  `AddServiceDiscovery`, puis `ConfigureHttpClientDefaults` avec découverte et
  `AddStandardResilienceHandler` ;
- `MapDefaultEndpoints` mappe `/health` pour la readiness et `/alive` pour la liveness.

Le livrable est inspecté comme code d'infrastructure autonome : les packages Aspire ne sont pas
requis par la moulinette.
