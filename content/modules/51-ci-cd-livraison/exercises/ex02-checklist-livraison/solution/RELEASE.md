# Release Asteria

## Commit et artefacts

Associer version, commit, résultats de tests, SBOM et signatures aux artefacts immuables.

## Client lourd

Tester l'installateur signé sur une machine propre, la mise à jour depuis la version supportée et la
conservation contrôlée de la configuration. Archiver l'ancien installateur.

## Services

Scanner puis publier une image OCI épinglée et non-root. Vérifier health checks, readiness, secrets
injectés et déploiement progressif avant promotion.

## Données

Relire la migration, prendre une sauvegarde restaurable, tester montée et rollback ou stratégie
compensatoire. L'identité runtime n'a pas les droits de schéma.

## Compatibilité

Tester ancien client contre nouveau service et nouveau client contre ancienne version supportée. Les
contrats HTTP, gRPC et messages restent compatibles pendant la fenêtre de coexistence.

## Rollback

Décrire déclencheur, responsable et commandes pour service, client et base. Exécuter le rollback en
recette, vérifier les données et conserver les preuves dans la release.
