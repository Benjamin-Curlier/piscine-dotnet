# Modèle de menaces Asteria

## Actifs

Identité opérateur, commandes, résultats, contrats, certificats et données d'audit.

## Frontières de confiance

Poste vers API, API vers broker, worker vers base et chaîne CI vers environnement de livraison.

## Rejeu de commande

- Prévention : jeton OIDC court, clé d'idempotence et horodatage contrôlé.
- Détection : doublons par identité, clé et correlationId dans l'audit.
- Réponse : refuser le second effet, révoquer le jeton et enquêter sur la source.

## Poste compromis

- Prévention : stockage OS, moindre privilège et aucune identité de service permanente.
- Détection : comportement anormal, volume et origine des commandes.
- Réponse : désactiver le poste, rotation des éléments exposés et réinstallation maîtrisée.

## Service usurpé

- Prévention : TLS, mTLS entre workloads et validation stricte de l'identité distante.
- Détection : échec de certificat, audience incohérente et topologie inattendue.
- Réponse : isoler l'instance, révoquer le certificat et redéployer depuis un artefact vérifié.
