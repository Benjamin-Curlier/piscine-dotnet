# ex01-aaa — Arrange-Act-Assert

## Objectif

Un test bien écrit se déroule en trois temps : **Arrange** (préparer les données), **Act** (appeler
la fonction à tester) et **Assert** (vérifier le résultat). Tu vas suivre ce patron pour tester une
addition.

Lis trois entiers : `a`, `b`, puis l'**attendu**. Calcule `somme = a + b` (c'est l'**Act**), puis
compare `somme` à l'attendu (c'est l'**Assert**) : affiche `PASS` si elles sont égales, sinon
`FAIL`.

Exemple : `2`, `3`, `5` → `somme` vaut `5`, qui égale l'attendu → `PASS`.
Exemple : `2`, `2`, `5` → `somme` vaut `4`, différent de `5` → `FAIL`.

## Livrable

- `Aaa.cs`

## Indices

- **Arrange** : lis `a`, `b` et `attendu` avec `int.Parse(System.Console.ReadLine())`.
- **Act** : calcule `var somme = a + b;`.
- **Assert** : `if (somme == attendu)` → `PASS`, sinon `FAIL`.
- Garde les trois temps bien séparés dans ton code : c'est ce qui rend un test lisible.
