# Ajouter un exercice

1. Générer le squelette : `piscine new exercise <module> <id>`.
   En attendant, copier un exercice existant.
2. Renseigner `manifest.yaml` (deliverables, grading, feedback) et `subject.md` (énoncé).
3. Placer les fichiers fournis dans `starter/`, les tests cachés dans `grader/`,
   et le corrigé de référence dans `solution/`.
4. Ajouter l'`id` de l'exercice dans un groupe de `module.yaml` (l'ordre = correction séquentielle).
5. Valider : `piscine validate-content` — vérifie que le corrigé passe ses propres graders.
6. Relire : `piscine audit-content` — signale sans bloquer les indices trop proches du corrigé,
   objectifs similaires, difficultés suspectes et collisions de noms file-based.

Voir aussi [Qualité pédagogique](qualite-pedagogique.md) pour les indices progressifs et le grader
`source`. La CI exécute la validation bloquante ; l'audit reste une aide à la revue humaine.

Aucune recompilation de l'application n'est nécessaire : le contenu est découvert au démarrage.
