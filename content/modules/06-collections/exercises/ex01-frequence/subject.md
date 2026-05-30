# ex01-frequence — Fréquence d'un mot

## Objectif

Lis **deux lignes** :

1. une liste de **mots** séparés par des espaces ;
2. un **mot cible**.

Affiche **combien de fois** le mot cible apparaît dans la liste (`0` s'il est absent).

Exemples : `a b a c a` puis `a` → `3` · `chat chien chat` puis `chien` → `1` · `x y z` puis `w` → `0`.

## Livrable

- `Frequence.cs`

## Indices

- Ajoute `using System.Collections.Generic;` en haut (nécessaire pour `GetValueOrDefault`).
- Parcours les mots et remplis un `Dictionary<string, int>` : `freq[mot] = freq.GetValueOrDefault(mot) + 1;`.
- Lis ensuite la cible sur la deuxième ligne et affiche `freq.GetValueOrDefault(cible)`.
