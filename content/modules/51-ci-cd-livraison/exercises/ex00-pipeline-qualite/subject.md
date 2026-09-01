# ex00 — Pipeline de qualité

Complète `.gitlab-ci.yml` avec des étapes build/test. La restauration utilise le lockfile, le build
Release ne restaure pas une seconde fois, puis les tests réutilisent le build. Ajoute vérification du
formatage et audit des paquets vulnérables.

Les résultats et logs sont conservés même lors d'un échec. La gate est bloquante, sans contournement
et sans image ou outil au tag `latest`.
