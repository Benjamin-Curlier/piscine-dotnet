# ex02-decorator — Decorator

## Objectif

Implémente le patron **Decorator** : ajouter des comportements à un objet en l'**enveloppant**
dans d'autres objets de même interface, plutôt qu'en multipliant les sous-classes.

On part d'un `Café` (coût `2`). Lis une ligne de garnitures séparées par des espaces, et enveloppe
la boisson au fur et à mesure. Garnitures : `lait` (+1), `sucre` (+1), `chocolat` (+2). Affiche la
description complète puis le coût, séparés par ` : `.

Exemples :
- `lait sucre` → `Café, lait, sucre : 4`
- *(ligne vide)* → `Café : 2`

## Livrable

- `Boisson.cs`

## Contraintes

- Chaque décorateur **enveloppe** une `IBoisson` et s'appuie sur elle (`Description()`, `Cout()`).
- Applique les garnitures **dans l'ordre** de lecture.

## Indices

- `interface IBoisson { string Description(); int Cout(); }` ; `Cafe` l'implémente directement.
- Une classe abstraite `Decorateur : IBoisson` garde une référence vers la boisson enveloppée.
- `Lait`/`Sucre`/`Chocolat` héritent de `Decorateur` : leur `Description()` = `enveloppe.Description() + ", ..."`
  et leur `Cout()` = `enveloppe.Cout() + n`.
- Repars de la boisson précédente à chaque token : `boisson = new Lait(boisson);`.
