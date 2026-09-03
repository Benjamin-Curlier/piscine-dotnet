# ex03-etat — Machine à états comme union

## Objectif

Avec une union, **chaque état porte ses propres données** et les transitions autorisées deviennent
explicites. La machine commence en `EnAttente`. Lis un entier `N`, puis `N` commandes :

- `demarrer` : `EnAttente` devient `EnCours(0)` ;
- `progres P` : un `EnCours` devient `EnCours(P)`, avec `P` entre 0 et 100 ;
- `terminer X` : un `EnCours` devient `Termine(X)`.

Après une transition acceptée, affiche le nouvel état : `en cours a P%` ou `termine: X`.
Toute autre transition affiche `transition refusee` et laisse l'état inchangé. Un état `Termine`
est donc terminal.

Exemple :

```text
Entrée :
3
demarrer
progres 50
terminer ok

Sortie :
en cours a 0%
en cours a 50%
termine: ok
```

## Livrable

- `Etat.cs`

## Contraintes

- Base abstraite avec des variantes scellées, chacune déclarant exactement les données qui la concernent.
- Décide la transition à partir de l'état courant **et** de la commande ; n'utilise pas une suite
  de drapeaux booléens.

## Indices

- `sealed record EnAttente : Etat;` (sans paramètre), `sealed record EnCours(int Pourcent) : Etat;`,
  `sealed record Termine(string Resultat) : Etat;`.
- Un `switch` sur le tuple `(etat, commande[0])` permet d'écrire les trois transitions autorisées.
- Fais renvoyer `null` à la fonction de transition quand la commande est refusée ; dans ce cas,
  conserve l'ancien état.
