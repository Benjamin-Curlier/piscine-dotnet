# ex00-assertion — L'assertion

## Objectif

Un test, au fond, c'est une **comparaison** : un résultat **attendu** face à un résultat
**obtenu**. C'est exactement ce que fait `Assert.Equal(attendu, obtenu)` en xUnit.

Lis deux entiers : d'abord l'**attendu**, puis l'**obtenu**. Affiche `OK` s'ils sont **égaux**,
sinon `KO`.

Exemple : `5` puis `5` → `OK`. `5` puis `6` → `KO`.

## Livrable

- `Assertion.cs`

## Indices

- Lis chaque entier avec `int.Parse(System.Console.ReadLine())`.
- Compare les deux valeurs avec `==`.
- Affiche le résultat avec `System.Console.WriteLine`.
- N'oublie pas le cas `0` / `0` : deux zéros sont bien **égaux**, donc `OK`.
