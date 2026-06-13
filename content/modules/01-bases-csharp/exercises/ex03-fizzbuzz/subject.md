# ex03-fizzbuzz — FizzBuzz (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis un entier `N`, puis affiche les entiers de `1` à `N` (un par ligne) en appliquant ces règles :

- multiple de **3 et de 5** → `FizzBuzz`
- multiple de **3** seulement → `Fizz`
- multiple de **5** seulement → `Buzz`
- sinon → le nombre lui-même

Exemple : pour `5`, le programme affiche `1`, `2`, `Fizz`, `4`, `Buzz`.

## Livrable

- `FizzBuzz.cs`

## Indices

- L'**ordre des tests** est crucial : commence par `i % 15 == 0` (ou `i % 3 == 0 && i % 5 == 0`),
  sinon un multiple de 15 sera capté par le test sur 3 avant d'arriver à la condition combinée.
- Une boucle `for (var i = 1; i <= n; i++)` parcourt la plage.
- Affiche soit le mot (`Fizz`, `Buzz`, `FizzBuzz`), soit `i`.
