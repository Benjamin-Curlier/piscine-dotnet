# ex00-animal — Le cri des animaux

## Objectif

Lis un entier **N**, puis **N** lignes contenant chacune un type d'animal : `chien` ou `chat`.
Pour chaque ligne, crée l'objet correspondant et affiche son **cri** (un par ligne) :
`chien` → `Wouf`, `chat` → `Miaou`.

Exemple : `3` puis `chien`, `chat`, `chien` → `Wouf`, `Miaou`, `Wouf`.

## Livrable

- `Animal.cs`

## Indices

- Déclare une classe de base `Animal` avec `public virtual string Cri()`.
- `Chien` et `Chat` héritent de `Animal` (`: Animal`) et font `override` de `Cri()`.
- Range les objets créés dans une `List<Animal>`, puis appelle `Cri()` sur chacun : c'est le
  **polymorphisme** qui choisit la bonne version.
- `List<>` nécessite `using System.Collections.Generic;`.
