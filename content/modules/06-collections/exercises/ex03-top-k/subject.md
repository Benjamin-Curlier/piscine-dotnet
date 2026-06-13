# ex03-top-k — Top K des mots fréquents (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis une **ligne de mots** séparés par des espaces, puis un entier `K`. Affiche les `K` mots les plus
fréquents, **un par ligne**, du plus fréquent au moins fréquent.

En cas d'**égalité de fréquence**, les mots sont départagés par **ordre alphabétique croissant**.
Si `K` dépasse le nombre de mots distincts, affiche tous les mots distincts.

Exemple : pour `a b a c a b` puis `2`, le programme affiche `a` puis `b`.

## Livrable

- `TopK.cs`

## Indices

- Compte les occurrences dans un `Dictionary<string,int>` (`freq[mot] = freq.GetValueOrDefault(mot) + 1`).
- Trie avec LINQ : `.OrderByDescending(p => p.Value).ThenBy(p => p.Key)` (nécessite `using System.Linq;`).
- Le `ThenBy` sur la clé garantit un ordre **déterministe** quand deux mots ont la même fréquence.
- `.Take(K)` limite au nombre demandé.
