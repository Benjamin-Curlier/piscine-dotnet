# ex01-aaa — AAA et valeur frontière

## Mission

L'API `Acces.Classer(age)` renvoie `mineur` avant 18 ans et `majeur` à partir de 18 ans. Écris une
petite suite xUnit organisée en Arrange-Act-Assert qui protège précisément cette frontière.

## Livrable

- `AccesTests.cs`

## Contraintes

- au moins un cas juste avant la limite ;
- le cas égal à la limite ;
- les tests doivent décrire leur intention dans leur nom.

## Progression des indices

1. Une valeur très éloignée de la frontière ne prouve pas la règle exacte.
2. Les deux âges les plus informatifs sont 17 et 18.
3. Un `Assert.Equal("majeur", Acces.Classer(18))` tue l'erreur classique `<` devenue `<=`.
