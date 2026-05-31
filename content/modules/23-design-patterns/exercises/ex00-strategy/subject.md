# ex00-strategy — Opérations interchangeables

## Objectif

Lis une **opération** (`add` ou `mul`) sur la première ligne, puis deux entiers **a** et **b**
(une ligne chacun). Selon l'opération, applique la **stratégie** correspondante et affiche le
résultat.

- `add` → addition : affiche `a + b`.
- `mul` → multiplication : affiche `a * b`.

Exemple : `add` puis `3` et `4` → `7`. `mul` puis `3` et `4` → `12`.

## Livrable

- `Strategy.cs`

## Indices

- Déclare une interface `IOperation` avec une méthode `int Appliquer(int a, int b)`.
- Crée deux classes : `Addition` (renvoie `a + b`) et `Multiplication` (renvoie `a * b`).
- Lis l'opération, choisis l'objet `IOperation` adéquat, puis appelle `Appliquer(a, b)` : le code
  d'appel ne connaît plus le détail de chaque calcul, c'est tout l'intérêt du patron **Strategy**.
- `int.Parse(System.Console.ReadLine())` lit un entier.
