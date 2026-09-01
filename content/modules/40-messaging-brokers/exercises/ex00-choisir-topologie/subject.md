# ex00 — Choisir une topologie

Lis `N`, puis `N` contraintes parmi :

- `diffusion-ephemere` ;
- `file-metier-durable` ;
- `pair-a-pair-embarque` ;
- `stream-rejouable`.

Affiche respectivement `NATS_CORE`, `RABBITMQ`, `ZEROMQ` ou `NATS_JETSTREAM`. Utilise un `switch`
pour rendre la décision explicite. Ici, chaque scénario donne volontairement une contrainte dominante
afin d'apprendre le vocabulaire ; un choix réel demanderait davantage de mesures.
