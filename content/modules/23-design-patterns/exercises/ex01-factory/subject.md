# ex01-factory — Fabrique d'animaux

## Objectif

Lis un entier **N**, puis **N** lignes contenant chacune un type d'animal : `chien` ou `chat`.
Pour chaque type, demande à une **fabrique** de créer l'objet `IAnimal` adéquat, puis affiche son
**cri** (un par ligne) : `chien` → `Wouf`, `chat` → `Miaou`.

Exemple : `2` puis `chien`, `chat` → `Wouf`, `Miaou`.

## Livrable

- `Factory.cs`

## Indices

- Déclare une interface `IAnimal` avec une méthode `string Cri()`.
- Crée `Chien` (renvoie `Wouf`) et `Chat` (renvoie `Miaou`), tous deux `: IAnimal`.
- Écris une classe **statique** `AnimalFactory` avec `static IAnimal Creer(string type)` qui renvoie
  un `new Chien()` ou un `new Chat()` selon le type. Le code d'appel ne fait plus de `new` lui-même :
  c'est le patron **Factory** qui centralise la création.
- `int.Parse(System.Console.ReadLine())` lit un entier.
