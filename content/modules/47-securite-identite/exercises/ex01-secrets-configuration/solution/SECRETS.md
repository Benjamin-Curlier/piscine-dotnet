# Gestion des secrets

En développement, initialiser `dotnet user-secrets` et injecter les valeurs hors du dépôt. Une
variable d'environnement peut alimenter un processus isolé sans modifier le binaire.

En recette et production, utiliser un coffre géré avec droits minimaux, audit et rotation. Révoquer
immédiatement une valeur exposée et ne jamais journaliser un jeton, mot de passe ou certificat.
