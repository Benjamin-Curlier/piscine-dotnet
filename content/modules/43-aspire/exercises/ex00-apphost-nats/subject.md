# ex00 — AppHost NATS

Complète `AppHost.cs` pour décrire :

- un conteneur `nats` basé sur l'image `nats:2.11-alpine`, lancé avec `-js` et un volume de données ;
- un projet `Projects.Api` nommé `api` ;
- un projet `Projects.Worker` nommé `worker` ;
- les deux projets référencent `nats` avec `WithReference` et attendent sa disponibilité avec
  `WaitFor` ;
- la distribution est construite et exécutée.

N'écris pas `localhost:4222` : la découverte/configuration doit venir du modèle Aspire.

Ce livrable est inspecté comme fichier AppHost ; la moulinette n'exige pas les projets générés
`Projects.Api` et `Projects.Worker`.
