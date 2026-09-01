# ex02-cas-limites — Tester résultat et exception

## Mission

L'API `Division.Calculer(a, b)` renvoie le quotient entier, mais doit lever
`ArgumentException` lorsque `b` vaut zéro. Protège les deux chemins avec xUnit.

## Livrable

- `DivisionTests.cs`

## Progression des indices

1. Garde un test nominal lisible, par exemple 6 divisé par 3.
2. Une exception fait partie du contrat : elle se teste, elle ne se masque pas.
3. Utilise `Assert.Throws<ArgumentException>(() => Division.Calculer(6, 0));`.
