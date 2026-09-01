# ex01 — Options validées

Dans `OptionsSetup.cs`, crée `StationOptions` avec une constante `SectionName`, un `StationId` requis
et un timeout borné. Écris ensuite une extension DI qui lie la section correspondante, applique les
DataAnnotations et valide les options au démarrage.

Le code ne doit lire directement aucune variable d'environnement ni contenir de secret.
