# ex04-composite — Propriétaire de ressources (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Quand un objet **détient** d'autres ressources, c'est à lui de les libérer : il implémente
`IDisposable` et, dans son `Dispose()`, libère chacune des ressources qu'il possède (en ordre
inverse d'ajout). Le client n'a plus qu'à libérer le propriétaire.

Lis des noms séparés par des espaces. Crée un `Groupe` dans un `using`, ajoute une ressource par
nom, affiche `travail`. À la fermeture du groupe, ses ressources sont libérées à l'envers.

Exemple : `A B` →
```
ouvre A
ouvre B
travail
ferme B
ferme A
```

## Livrable

- `Groupe.cs`

## Contraintes

- `Groupe` implémente `IDisposable` et libère ses ressources en **ordre inverse** d'ajout.
- Une seule mise en `using` (celle du `Groupe`) doit suffire à tout libérer.

## Indices

- `Groupe` garde une `List<Ressource>` (nécessite `using System.Collections.Generic;`).
- Dans `Dispose()`, parcours la liste de la fin vers le début et appelle `Dispose()` sur chaque
  ressource — c'est le pattern « ownership » : qui possède, libère.
