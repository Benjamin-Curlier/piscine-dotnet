# ex00-assertion — Premier vrai test xUnit

## Mission

Tu ne produis plus une sortie console : tu écris une suite de tests contre cette API fournie :

```csharp
public static class Calculatrice
{
    public static int Additionner(int a, int b);
}
```

Le starter vérifie seulement `0 + 0`, un exemple trop faible. Remplace-le ou complète-le par des
tests `[Fact]` qui échoueraient si l'addition était accidentellement remplacée par une soustraction.

## Livrable

- `CalculatriceTests.cs`

## Progression des indices

1. Respecte Arrange-Act-Assert et donne un nom qui décrit le scénario.
2. `Assert.Equal(attendu, Calculatrice.Additionner(a, b))` compare le contrat au résultat.
3. Évite uniquement des zéros : addition et soustraction y donnent toutes deux zéro.
