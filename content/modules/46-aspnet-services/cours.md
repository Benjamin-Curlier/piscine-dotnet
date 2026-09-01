# Module 46 — ASP.NET Core comme façade de service

Ici, ASP.NET Core ne remplace pas l'application lourde. Il fournit une frontière réseau standard pour
les commandes, requêtes, mises à jour et intégrations externes du poste opérateur.

## 1. Pipeline et Minimal APIs {#minimal-api}

Une application `Microsoft.NET.Sdk.Web` construit un `WebApplication`, enregistre ses dépendances,
configure les middlewares puis mappe les routes. Les Minimal APIs conviennent aux services ciblés ;
les contrôleurs restent utiles pour des surfaces plus conventionnelles. La règle importante est de
garder le métier hors des handlers HTTP.

Les entrées réseau sont non fiables. Valide DTO, taille, format et règles métier avant publication.
Utilise les codes HTTP selon le résultat : `200` pour une lecture, `201` pour une création immédiate,
`202` pour une commande acceptée mais asynchrone, `400` pour une requête invalide, `404`, `409` pour
un conflit et `503` pour une dépendance temporairement indisponible.

## 2. Erreurs stables {#problem-details}

`ProblemDetails` fournit une enveloppe standard. Le client desktop doit décider selon un code ou un
type stable, pas analyser un texte français. Ne renvoie ni stack trace ni chaîne de connexion. Le
`traceId` aide le support à relier l'erreur aux logs et traces.

Une clé d'idempotence évite qu'un retry du client crée deux commandes. La réponse doit distinguer une
nouvelle acceptation d'une commande déjà connue.

## 3. OpenAPI comme contrat {#openapi}

`AddOpenApi` et `MapOpenApi` génèrent le document de la surface HTTP. Décris identifiants d'opération,
schémas, statuts et sécurité. Le document peut produire un client, alimenter les tests de contrat et
signaler un changement incompatible avant livraison.

L'endpoint OpenAPI est souvent limité à l'environnement de développement ; le document de build peut
être publié comme artefact de CI.

## 4. Client lourd {#client-lourd}

Le desktop utilise un `HttpClient` fourni par la factory, un timeout, un `CancellationToken` et les
politiques de résilience déjà étudiées. Il ne fabrique pas une URL depuis `localhost` : configuration,
découverte ou Aspire fournissent l'adresse.

### Atelier réel

Héberge la façade localement, génère son OpenAPI, appelle-la depuis un petit client console puis depuis
le ViewModel du module précédent. Coupe le service et vérifie l'état dégradé du client.

Références :

- [OpenAPI dans ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview)
- [Générer le document OpenAPI](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
