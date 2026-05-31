# ex00-const — Multiplication par une constante

## Objectif

Déclare une **constante de compilation** `Facteur` égale à `10`.

Lis un entier **n** sur l'entrée standard, puis affiche `n * Facteur`.

Exemple : entrée `5` → sortie `50`.

## Livrable

- `Const.cs`

## Indices

- Utilise `const int Facteur = 10;` en haut du programme (avant tout code exécutable).
- Une constante est **implicitement statique** : pas besoin d'écrire `static`.
- Lis l'entier avec `int.Parse(System.Console.ReadLine())`.
- Affiche le résultat avec `System.Console.WriteLine(n * Facteur)`.
