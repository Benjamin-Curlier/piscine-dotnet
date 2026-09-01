# Contenu pédagogique

- `modules/<NN-slug>/` : un module = un dossier ordonné par `order` dans `module.yaml` ; les champs
  `prerequisites`, `arc` et `mission` décrivent sa place dans le parcours.
- `rushes/<slug>/` : projets de synthèse solo. Leur manifest ajoute `recommended_after` pour le
  jalon conseillé et `manual_validation: true` lorsqu'une revue de preuves terrain complète la
  moulinette automatique.

Chaque module contient `module.yaml`, `cours.md`, et `exercises/<id>/`.
Chaque exercice contient `manifest.yaml`, `subject.md`, `starter/`, `grader/`, `solution/`.
Un manifest déclare explicitement `difficulty`, `estimated_minutes`, `xp`, `tags` et `story_beat`.
Le grader `fichier` permet de vérifier des livrables texte non compilables localement (Dockerfile,
Razor, shader, AppHost) ; il ne remplace pas l'atelier réel dans l'environnement cible.

Voir `docs/contributing/ajouter-un-exercice.md` et `docs/contributing/qualite-pedagogique.md`.
Le fil rouge et le rythme attendu sont décrits dans [`parcours.md`](parcours.md).
Les dossiers `solution/` ne sont jamais inclus dans le zip distribué.
