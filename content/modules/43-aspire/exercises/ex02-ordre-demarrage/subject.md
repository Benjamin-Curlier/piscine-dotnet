# ex02 — Ordre de démarrage

Lis `N`, puis `N` lignes `ressource dependance`. `-` signifie aucune dépendance ; chaque ressource a
au plus une dépendance dans cet exercice.

Construis un ordre valide. À chaque étape, choisis **lexicalement la première** ressource dont la
dépendance est déjà démarrée. Si aucun ordre complet n'existe, affiche seulement `CYCLE`.

Cette simplification entraîne le raisonnement derrière `WaitFor` : un vrai AppHost peut avoir
plusieurs dépendances et les démarrages indépendants sont parallèles.
