# ex03-stats-json — Statistiques JSON (bonus)

> **Bonus difficile, non bloquant.** Synthèse sérialisation : désérialiser une entrée JSON,
> re-sérialiser un résultat.

## Énoncé

L'entrée est **une ligne** contenant un **tableau JSON d'entiers**, par exemple `[3,1,4,1,5]`.

Désérialise ce tableau, calcule le **minimum**, le **maximum** et la **somme**, puis affiche un
**objet JSON** sérialisé avec les propriétés `Min`, `Max`, `Somme` (noms par défaut, PascalCase) :

```
Entrée :
[3,1,4,1,5]

Sortie :
{"Min":1,"Max":5,"Somme":14}
```

## Indications

- `int[] nombres = JsonSerializer.Deserialize<int[]>(json);`
- Crée une petite classe `Stats { Min, Max, Somme }`, remplis-la (`nombres.Min()`, `.Max()`,
  `.Sum()`), puis `JsonSerializer.Serialize(stats)`.
- La sérialisation par défaut conserve l'ordre de déclaration des propriétés et les noms exacts —
  la sortie est donc déterministe.
