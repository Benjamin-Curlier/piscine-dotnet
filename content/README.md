# Contenu pédagogique

- `modules/<NN-slug>/` : un module = un dossier ordonné par `order` dans `module.yaml`.
- `rushes/<slug>/` : projets de synthèse solo.

Chaque module contient `module.yaml`, `cours.md`, et `exercises/<id>/`.
Chaque exercice contient `manifest.yaml`, `subject.md`, `starter/`, `grader/`, `solution/`.

Voir `docs/contributing/ajouter-un-exercice.md`. Les dossiers `solution/` ne sont jamais
inclus dans le zip distribué.
