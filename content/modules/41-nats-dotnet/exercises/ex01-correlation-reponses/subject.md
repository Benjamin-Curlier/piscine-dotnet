# ex01 — Corréler des réponses

Lis `N` identifiants de requêtes dans leur ordre d'émission, puis `R` réponses `correlationId valeur`
dans leur ordre d'arrivée. Affiche une ligne par requête initiale : `id=valeur`, ou `id=TIMEOUT` si
aucune réponse n'est arrivée.

Utilise un dictionnaire ; ne suppose jamais que la ième réponse correspond à la ième requête.
