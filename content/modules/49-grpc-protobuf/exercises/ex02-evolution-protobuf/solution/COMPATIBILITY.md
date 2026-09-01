# Compatibilité Protobuf

## Règles de schéma

Ne jamais réutiliser un numéro ou changer sa signification. Un champ supprimé devient `reserved` par
numéro et nom. Un nouveau champ optionnel possède une valeur par défaut comprise des deux versions.

## Matrice de test

- ancien client / nouveau serveur : soumission et suivi inchangés ;
- nouveau client / ancien serveur : le nouveau champ optionnel peut être ignoré ;
- ancienne donnée / nouveau consumer : désérialisation et comportement métier conservés.

## Déploiement progressif

Déployer d'abord le serveur tolérant les deux formes, puis les postes par vagues. Suivre par
télémétrie la version des appels, erreurs de désérialisation et usage de l'ancien champ.

## Retrait

Après la période de coexistence et disparition mesurée des anciennes versions, retirer le code mort
mais conserver numéro et nom `reserved`. Un retour arrière reste testé pendant la fenêtre de release.
