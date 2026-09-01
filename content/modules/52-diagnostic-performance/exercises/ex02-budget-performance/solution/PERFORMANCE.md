# Budget de performance Asteria

## Environnement

Poste 8 cœurs/16 Go, réseau de recette contrôlé, jeu de 100 000 commandes. Conserver version, charge,
configuration, baseline et artefacts de mesure.

## Démarrage du poste

Premier écran utilisable en moins de 3 secondes ; critère d'échec au-delà de 3,5 secondes sur trois
exécutions médianes.

## Réactivité UI

Aucun handler supérieur à 50 ms et p95 des interactions inférieur à 100 ms sous synchronisation.

## API

À 100 requêtes/s, p95 inférieur à 250 ms et moins de 1 % de réponses 5xx.

## Broker

Backlog inférieur à 1 000 messages et âge maximal inférieur à 30 secondes à la charge nominale.

## Mémoire

Après cinq cycles, heap stabilisé sous 600 Mo avec croissance inférieure à 5 % entre les deux derniers.

## Reprise

Après coupure broker de 30 secondes, reprise en moins de 60 secondes sans double effet. Archiver
compteurs, trace, logs corrélés et rapport de comparaison.
