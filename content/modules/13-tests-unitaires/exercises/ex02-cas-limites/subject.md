# ex02-cas-limites — Les cas limites

## Objectif

Les bugs se cachent aux **frontières**. Ici, la frontière est le `0` : il n'est ni positif, ni
négatif, c'est le **cas limite** qu'on oublie le plus souvent. Tu vas écrire un classement qui ne
le rate pas.

Lis un entier **N**, puis **N** entiers. Pour chacun, affiche (un par ligne) :

- `positif` s'il est strictement supérieur à 0,
- `negatif` s'il est strictement inférieur à 0,
- `zero` s'il vaut exactement 0.

Exemple : `3` puis `5`, `-2`, `0` → `positif`, `negatif`, `zero`.

## Livrable

- `CasLimites.cs`

## Indices

- Lis `N`, puis fais une boucle `for` de `N` tours.
- Dans la boucle : lis l'entier, puis teste `> 0`, `< 0`, sinon `zero`.
- Le piège classique : ne traite **pas** `0` comme un négatif. Pense à l'ordre de tes `if` et
  garde un cas dédié pour le `zero`.
