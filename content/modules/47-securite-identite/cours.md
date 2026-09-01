# Module 47 — Sécurité et identité

Une application lourde installée sur un poste de confiance n'est pas automatiquement sûre. Le poste,
le réseau, l'API, le broker et la base constituent des frontières distinctes.

## 1. Authentification et autorisation {#policies}

L'**authentification** établit l'identité. L'**autorisation** décide ce qu'elle peut faire. ASP.NET
Core représente l'identité par des claims et évalue des policies. Une policy métier comme
`asteria.command.send` est plus stable qu'un test dispersé sur le nom d'un rôle.

Dans une entreprise Windows, Negotiate/Windows Authentication peut convenir sur un réseau géré. Pour
des clients distribués ou multi-plateformes, OAuth 2.0 et OpenID Connect sont fréquents. Un client
public desktop ne conserve jamais un secret applicatif ; utilise un flux prévu pour client public et
un stockage sécurisé fourni par l'OS. Les services utilisent une identité de workload, certificat ou
jeton court.

## 2. Défense des API {#api-security}

Valide audience, issuer, signature et expiration du jeton. Autorise au plus juste sur chaque route.
TLS protège le transport ; mTLS peut aussi authentifier une machine ou un service. CORS concerne les
navigateurs et ne remplace jamais l'autorisation d'une API.

Rate limiting, limites de taille et timeouts réduisent les abus. Les messages de broker possèdent eux
aussi des permissions par sujet et des contrats validés.

## 3. Secrets et configuration {#secrets}

Le dépôt contient des noms de paramètres, jamais leurs valeurs sensibles. En développement, Secret
Manager ou un coffre local suffit. En CI et production, le système d'exécution injecte le secret.
Prévois rotation, expiration et révocation ; un secret imprimé dans un log est déjà compromis.

## 4. Modèle de menaces {#threat-model}

Avant le code, liste les actifs, acteurs, frontières de confiance et conséquences. Pour chaque menace,
choisis une prévention, une détection et une réponse. Exemples : rejeu d'une commande, poste volé,
élévation de privilège, message altéré, secret exposé, service usurpé ou dépendance compromise.

### Atelier réel

Protège une route de commande avec une policy, appelle-la sans jeton, avec un jeton expiré puis avec
le scope attendu. Vérifie les codes HTTP et l'absence de donnée sensible dans les logs.

Références :

- [Authentification ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/)
- [Sécurité ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/)
- [Windows Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/windowsauth)
