# ex03-somme-parallele-carres — Somme des carrés en parallèle (bonus)

> **Bonus difficile, non bloquant.** Synthèse threading avancé : `Parallel.For` + accumulation
> atomique sans race condition.

## Énoncé

Lis un entier **N**. Calcule la somme des carrés `1² + 2² + … + N²` **en parallèle**
(`Parallel.For`), en accumulant dans un total partagé **sans race condition** (`Interlocked.Add`
sur un `long`). Affiche le total.

Le résultat doit être **déterministe** malgré le parallélisme.

## Exemple

```
Entrée :
3

Sortie :
14
```

(`1² + 2² + 3² = 1 + 4 + 9 = 14`)

## Indications

- `Parallel.For(1, n + 1, i => { ... })` exécute le corps pour `i` de 1 à N (la borne haute est
  **exclue**, d'où `n + 1`).
- Plusieurs threads écrivent dans `total` en même temps : `total += ...` créerait une **race
  condition**. Utilise `Interlocked.Add(ref total, (long)i * i)` (opération atomique).
- Un `long` évite tout dépassement pour de grands N.
