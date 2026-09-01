# Exploitation du poste Asteria

## Scénario nominal

Soumettre depuis le poste et suivre le `correlationId` dans API, outbox, NATS et worker. La readiness
reste saine, le statut gRPC progresse et un seul effet métier est observé.

## Poste déconnecté

Couper le réseau du poste pendant une soumission. L'UI reste réactive, affiche l'état non confirmé et
n'écrase pas une réponse récente lors de la reconnexion.

## Coupure API

Arrêter l'API, vérifier l'état dégradé et l'absence de boucle de retry agressive. Après reprise, une
nouvelle tentative avec la même clé retrouve l'identifiant initial : aucun double effet.

## Coupure NATS

Arrêter NATS après commit. L'outbox conserve l'événement. Au redémarrage, observer publication,
redelivery éventuelle, acquittement et aucun double effet côté worker.

## Conflit de données

Modifier la même commande depuis deux sessions. La seconde écriture reçoit un conflit, recharge la
version et demande une décision opérateur au lieu d'écraser.

## Compatibilité de versions

Tester ancien client avec nouveau service et nouveau client avec la version encore supportée. Suivre
la version du contrat dans la télémétrie avant retrait d'un champ.

## Rollback

Redéployer l'image précédente et réinstaller l'ancien client depuis les artefacts signés. Vérifier
readiness, compatibilité de schéma, reprise de l'outbox et absence de perte avant clôture.
