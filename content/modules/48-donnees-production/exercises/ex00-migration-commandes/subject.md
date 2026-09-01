# ex00 — Migration relue

Écris une migration PostgreSQL transactionnelle créant `commands` avec identifiant primaire, clé
d'idempotence obligatoire, statut, payload, version de concurrence et date de création.

Ajoute un index unique sur la clé d'idempotence et un index de lecture sur statut/date. Aucun droit
global, secret ou suppression de base n'appartient à cette migration.
