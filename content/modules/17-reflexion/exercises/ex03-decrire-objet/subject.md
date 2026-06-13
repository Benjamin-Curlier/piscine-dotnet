# ex03-decrire-objet — Décrire un objet par réflexion (bonus)

> **Bonus difficile, non bloquant.** Synthèse réflexion : énumérer les propriétés et lire leurs
> valeurs sans connaître la classe à l'avance.

## Énoncé

Lis trois lignes : **Nom** (texte), **Age** (entier), **Actif** (`true`/`false`). Construis un objet
`Personne { Nom, Age, Actif }`.

Puis, **par réflexion** (sans écrire les noms de propriétés à la main), affiche chaque propriété au
format `Nom=valeur`, **triées par nom** dans l'ordre ordinal — donc `Actif`, puis `Age`, puis `Nom`.

## Exemple

```
Entrée :
Alice
30
true

Sortie :
Actif=True
Age=30
Nom=Alice
```

## Indications

- `personne.GetType().GetProperties()` renvoie les `PropertyInfo` ; l'ordre n'est **pas** garanti,
  d'où le tri explicite `.OrderBy(p => p.Name, StringComparer.Ordinal)` pour une sortie déterministe.
- `prop.GetValue(personne)` lit la valeur ; un `bool` s'affiche `True`/`False`.
