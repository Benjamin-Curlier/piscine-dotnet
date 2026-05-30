# Rush 0 — FizzBuzz

> **Un Rush est un projet de synthèse solo.** Pas de nouveau cours : tu réutilises tout ce que tu
> as appris dans les modules 01 à 04 (entrée/sortie, boucles, conditions, modulo).

## Le problème

Lis **un entier** `N` sur l'entrée standard, puis affiche les nombres de `1` à `N`, **un par
ligne**, en appliquant la règle **FizzBuzz** :

- multiple de **3** **et** de **5** (donc de 15) → affiche `FizzBuzz` ;
- multiple de **3** seulement → affiche `Fizz` ;
- multiple de **5** seulement → affiche `Buzz` ;
- sinon → affiche le **nombre** lui-même.

### Exemple (`N = 5`)

```
1
2
Fizz
4
Buzz
```

### Exemple (`N = 15`)

```
1
2
Fizz
4
Buzz
Fizz
7
8
Fizz
Buzz
11
Fizz
13
14
FizzBuzz
```

## Livrable

- `FizzBuzz.cs`

## Le piège classique

Si tu testes `% 3` et `% 5` **avant** le cas `% 15`, le `FizzBuzz` n'apparaîtra **jamais** (15 sera
déjà capté par `Fizz`). **Teste le cas le plus spécifique en premier** :

```text
si i % 15 == 0   -> "FizzBuzz"
sinon si i % 3   -> "Fizz"
sinon si i % 5   -> "Buzz"
sinon            -> i
```

## Rendu

Comme pour un exercice : travaille dans ton workspace, puis `git add` / `commit` / `push origin main`.
La moulinette corrige le Rush comme un livrable autonome.
