# Protocole d'exploitation Asteria

## Scénario nominal

Publier une commande avec un `correlationId` neuf. Vérifier dans le dashboard que la trace relie API,
publication et worker, que le message est acquitté et qu'un seul effet métier est visible. La
readiness de l'API et du worker doit être saine.

## Coupure du worker

Arrêter le worker depuis le dashboard puis publier une commande. Elle doit rester dans JetStream :
aucun effet n'est encore produit et l'âge du message augmente. Redémarrer le worker et observer la
redelivery, la même corrélation et un seul traitement.

## Coupure de NATS

Arrêter NATS pendant une publication. L'API doit borner son attente, exposer un échec observable et
ne pas prétendre être ready si le broker est une dépendance obligatoire. Les logs et la trace portent
le `correlationId`, sans boucle de retry infinie.

## Reprise

Redémarrer NATS puis le worker. Vérifier reconnexion, présence du stream et reprise des messages. Une
redelivery déjà appliquée est reconnue comme `duplicate`, acquittée, et ne produit aucun double effet.
Confirmer le retour de la readiness et conserver la preuve dans les traces, logs et métriques.
