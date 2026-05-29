# Ajouter un exercice

1. Générer le squelette : `piscine new exercise <module> <id>` *(commande disponible à l'It. 1+)*.
   En attendant, copier un exercice existant.
2. Renseigner `manifest.yaml` (deliverables, grading, feedback) et `subject.md` (énoncé).
3. Placer les fichiers fournis dans `starter/`, les tests cachés dans `grader/`,
   et le corrigé de référence dans `solution/`.
4. Ajouter l'`id` de l'exercice dans un groupe de `module.yaml` (l'ordre = correction séquentielle).
5. Valider : `piscine validate-content` *(disponible à l'It. 1+)* — vérifie que le corrigé
   passe ses propres graders. La CI exécute la même vérification.

Aucune recompilation de l'application n'est nécessaire : le contenu est découvert au démarrage.
