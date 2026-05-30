# ex02-frequence-tri — Fréquence triée

## Objectif

Lis **une ligne** de mots séparés par des espaces. Compte combien de fois chaque mot apparaît,
puis affiche **une ligne par mot** au format `mot: count`.

Le classement se fait :

1. par **fréquence décroissante** (le mot le plus fréquent en premier) ;
2. en cas d'**égalité**, par **ordre alphabétique**.

Exemple : `pomme poire pomme banane poire pomme` →

```
pomme: 3
poire: 2
banane: 1
```

## Livrable

- `Frequence.cs`

## Indices

- Ajoute `using System.Linq;` en haut.
- Regroupe les mots avec `.GroupBy(m => m)` : chaque groupe a une clé `g.Key` et un `g.Count()`.
- Projette avec `.Select(g => new { Mot = g.Key, Compte = g.Count() })` (type anonyme).
- Trie avec `.OrderByDescending(x => x.Compte).ThenBy(x => x.Mot)`.
- Parcours le résultat et affiche `$"{g.Mot}: {g.Compte}"`.
