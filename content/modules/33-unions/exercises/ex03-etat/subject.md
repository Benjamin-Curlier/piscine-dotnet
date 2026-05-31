# ex03-etat — Machine à états comme union

## Objectif

Avec une union, **chaque état porte ses propres données** : plus besoin d'une grosse classe avec
des champs nuls la plupart du temps. L'état `EnCours` a un pourcentage, `Termine` a un résultat,
`EnAttente` n'a rien — et c'est le type qui le garantit.

Lis `attente`, `cours N` ou `termine X`. Affiche :
- `en attente`
- `en cours a N%`
- `termine: X`

Exemples : `cours 50` → `en cours a 50%` ; `termine ok` → `termine: ok`.

## Livrable

- `Etat.cs`

## Contraintes

- Union scellée où chaque variante déclare exactement les données qui la concernent.

## Indices

- `sealed record EnAttente : Etat;` (sans paramètre), `sealed record EnCours(int Pourcent) : Etat;`,
  `sealed record Termine(string Resultat) : Etat;`.
- Dans le `switch`, `EnAttente => "en attente"` (pas de variable), `EnCours e => ... e.Pourcent ...`.
- Modéliser les états ainsi rend les transitions invalides **inexprimables**.
