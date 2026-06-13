# ex03-mot-frequent — Le mot le plus fréquent (bonus)

> **Bonus difficile, non bloquant.** Synthèse LINQ : `GroupBy` + tri composé + sélection.

## Énoncé

Lis des mots (**un par ligne**) jusqu'à la fin de l'entrée. Affiche le mot **le plus fréquent** et son
nombre d'occurrences, au format :

```
<mot> <nombre>
```

En cas d'**égalité** de fréquence, choisis le mot le plus petit dans l'ordre **alphabétique ordinal**
(la sortie doit être **déterministe**).

**Contrainte** : utilise **LINQ** (`GroupBy`, `OrderByDescending`, `ThenBy`, `First`).

## Exemple

```
Entrée :
pomme
poire
pomme

Sortie :
pomme 2
```

## Indications

- `GroupBy(m => m)` regroupe les mots identiques ; `groupe.Count()` donne la fréquence,
  `groupe.Key` le mot.
- Trie par `OrderByDescending(g => g.Count())` **puis** `ThenBy(g => g.Key, StringComparer.Ordinal)`
  pour départager les égalités de façon déterministe, et prends `.First()`.
