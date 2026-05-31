# ex03-builder — Builder

## Objectif

Implémente le patron **Builder** : construire un objet complexe **pas à pas** via une interface
**fluide** (chaînable), au lieu d'un constructeur géant.

Lis une ligne d'ingrédients extra séparés par des espaces. Le burger part toujours de
`pain, steak` ; ajoute chaque extra. Affiche `Burger : <ingrédients> (<N> ingrédients)`.

Exemples :
- `fromage bacon` → `Burger : pain, steak, fromage, bacon (4 ingrédients)`
- *(ligne vide)* → `Burger : pain, steak (2 ingrédients)`

## Livrable

- `Burger.cs`

## Contraintes

- La méthode d'ajout **renvoie le builder** (`return this;`) pour permettre le chaînage.
- L'objet final est produit par une méthode `Construire()`.

## Indices

- `BurgerBuilder` garde une `List<string>` initialisée à `{ "pain", "steak" }`
  (nécessite `using System.Collections.Generic;`).
- `public BurgerBuilder Avec(string ingredient) { _ingredients.Add(ingredient); return this; }`.
- `Construire()` assemble la chaîne avec `string.Join(", ", _ingredients)` et `_ingredients.Count`.
