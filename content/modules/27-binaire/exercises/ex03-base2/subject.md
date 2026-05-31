# ex03-base2 — Représentation binaire (BONUS)

## Objectif

Lis un entier **n** (positif ou nul). Affiche sa **représentation binaire**, sans zéros de tête.
Le cas particulier `0` s'affiche `0`.

Exemple : `5` → `101`, `10` → `1010`.

## Livrable

- `Base2.cs`

## Indices

- `Convert.ToString(n, 2)` renvoie directement la chaîne binaire de `n` (la base `2`), déjà sans
  zéros de tête, et `0` donne bien `"0"`.
- `Convert` vit dans l'espace de noms `System` : ajoute `using System;` en haut du fichier.
- Affiche avec `System.Console.WriteLine(Convert.ToString(n, 2));`.
