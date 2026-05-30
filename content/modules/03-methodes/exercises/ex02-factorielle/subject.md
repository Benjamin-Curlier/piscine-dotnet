# ex02-factorielle — Factorielle

## Objectif

Lis **un entier** `n` (positif) sur l'entrée standard, puis affiche sa **factorielle**
`n! = n × (n-1) × … × 1`, avec la convention `0! = 1`.

Le but est d'écrire une méthode **récursive** : elle s'appelle elle-même.

Exemples : `5` → `120` · `0` → `1` · `6` → `720`.

## Livrable

- `Factorielle.cs`

## Indices

- **Cas de base** : si `n <= 1`, renvoie `1` (sinon la récursion ne s'arrête jamais).
- **Cas récursif** : sinon, renvoie `n * Factorielle(n - 1)`.
- Utilise `long` pour le retour : la factorielle grandit très vite.
