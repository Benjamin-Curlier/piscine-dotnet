# Module 49 — gRPC et Protobuf

HTTP/JSON reste excellent aux frontières ouvertes. Entre services maîtrisés ou pour un client lourd
qui échange fréquemment des messages typés, gRPC apporte génération de client, streaming et contrat
Protobuf compact.

## 1. Le fichier `.proto` {#proto}

Le schéma déclare messages, numéros de champs et services. Les numéros font partie du format binaire :
ne les réutilise jamais pour une autre signification. Lorsqu'un champ disparaît, marque son numéro et
son nom `reserved`. Ajouter un champ optionnel est généralement compatible avec les anciens clients.

Le contrat ne doit pas exposer directement les entités EF. Utilise des messages de transport stables,
des unités explicites et des timestamps définis.

## 2. Hébergement ASP.NET Core {#service}

Le serveur enregistre gRPC dans la DI puis mappe le service généré. Le service traduit le contrat vers
un cas d'usage applicatif. Logging, authentification, autorisation et observabilité du pipeline
ASP.NET Core restent disponibles.

gRPC utilise HTTP/2. En production, configure TLS, taille maximale, deadlines et politiques adaptées.
Le serveur observe `ServerCallContext.CancellationToken` : si le poste abandonne, le travail annulable
ne doit pas continuer inutilement.

## 3. Types d'appels {#streaming}

- unaire : une requête, une réponse ;
- streaming serveur : le poste reçoit une suite ;
- streaming client : le poste envoie une suite ;
- duplex : les deux flux évoluent indépendamment.

Un flux n'est pas une file durable. Pour conserver et rejouer des commandes après une coupure, le
broker reste la bonne abstraction.

## 4. Évolution et déploiement progressif {#compatibilite}

Teste ancien client contre nouveau serveur et nouveau client contre ancien serveur. Déploie d'abord
les changements compatibles, mesure l'usage puis retire les anciens champs dans une version majeure.
Les erreurs gRPC utilisent des statuts stables ; ne force pas le client à analyser une phrase.

### Atelier réel

Génère client et serveur depuis le proto, appelle le service depuis le poste, ajoute un champ
optionnel puis rejoue les tests avec l'ancien client. Coupe la connexion pendant un streaming.

Références :

- [Vue d'ensemble gRPC pour .NET](https://learn.microsoft.com/aspnet/core/grpc/)
- [Services gRPC avec ASP.NET Core](https://learn.microsoft.com/aspnet/core/grpc/aspnetcore)
