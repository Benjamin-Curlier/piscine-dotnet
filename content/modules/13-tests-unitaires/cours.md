# Module 13 — Tests unitaires

Jusqu'ici, pour savoir si ton code marchait, tu le lançais et tu **regardais** la sortie « à la
main ». Ça fonctionne pour un petit programme, mais dès qu'il grossit, c'est lent, fatigant, et on
finit par oublier de vérifier des cas. Un **test unitaire** automatise cette vérification : c'est un
bout de code qui exécute une portion de ton programme et **vérifie tout seul** que le résultat est
bien celui attendu.

## 1. Pourquoi tester

Un test, c'est une question posée au code : « pour cette entrée, est-ce que tu produis bien ce
résultat ? ». L'ordinateur répond à ta place, instantanément, autant de fois que tu veux.

- **Confiance** : tu modifies ton code et tu relances les tests ; s'ils passent encore, tu n'as
  rien cassé (on appelle ça éviter les **régressions**).
- **Rapidité** : des centaines de vérifications en quelques secondes, sans cliquer.
- **Documentation** : un bon test montre comment une méthode est censée se comporter.

L'idée centrale tient en une comparaison : on a un résultat **attendu** et un résultat **obtenu**,
et on vérifie qu'ils sont **égaux**. Si oui, le test **passe** ; sinon, il **échoue** et te dit
quoi.

## 2. xUnit : la boîte à outils des tests en .NET {#premier-test-xunit}

En .NET, on écrit le plus souvent les tests avec **xUnit**. Tu n'as pas à l'installer pour ce
module : retiens d'abord la forme. Un test est une **méthode** marquée par un **attribut**.

### `[Fact]` : un test pour un cas précis

Un `[Fact]` est un test sans paramètre : il vérifie **un** scénario.

```csharp
using Xunit;

public class CalculTests
{
    [Fact]
    public void Addition_DeuxPlusTrois_DonneCinq()
    {
        var resultat = 2 + 3;
        Assert.Equal(5, resultat);
    }
}
```

### `Assert` : poser la vérification

`Assert` regroupe les méthodes qui **affirment** ce qui doit être vrai. Si l'affirmation est
fausse, le test échoue.

```csharp
Assert.Equal(5, resultat);            // attendu == obtenu ?
Assert.True(age >= 0);                // la condition est-elle vraie ?
Assert.False(liste.Contains(-1));     // la condition est-elle fausse ?
```

> Convention importante : dans `Assert.Equal`, l'**attendu** vient en **premier**, l'**obtenu** en
> second. `Assert.Equal(5, resultat)` se lit « j'attends 5, et voici ce que j'ai obtenu ».

### `[Theory]` / `[InlineData]` : le même test pour plusieurs entrées

Quand un test doit être rejoué avec des données différentes, on utilise un `[Theory]` alimenté par
des `[InlineData]`. Chaque ligne devient un cas exécuté séparément.

```csharp
[Theory]
[InlineData(2, 3, 5)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Addition_DonneLaSomme(int a, int b, int attendu)
{
    Assert.Equal(attendu, a + b);
}
```

### `Assert.Throws` : vérifier qu'une exception part bien {#tester-les-exceptions}

Parfois, le bon comportement est de **lever une exception** (vu au module 09). On le vérifie ainsi :

```csharp
[Fact]
public void Division_ParZero_Leve()
{
    Assert.Throws<System.DivideByZeroException>(() => Diviser(1, 0));
}
```

## 3. Le patron Arrange-Act-Assert (AAA) {#arrange-act-assert}

Un bon test se lit en **trois temps**, toujours dans le même ordre :

1. **Arrange** (préparer) : on met en place les données et l'objet à tester.
2. **Act** (agir) : on appelle **la** méthode qu'on veut vérifier.
3. **Assert** (vérifier) : on compare le résultat obtenu au résultat attendu.

```csharp
[Fact]
public void Somme_DeuxNombres_DonneLeTotal()
{
    // Arrange
    var a = 2;
    var b = 3;

    // Act
    var somme = a + b;

    // Assert
    Assert.Equal(5, somme);
}
```

Séparer ces trois temps rend le test **lisible** : on voit d'un coup d'œil ce qui est préparé, ce
qui est exécuté, et ce qui est vérifié.

## 4. Bien nommer ses tests

Le nom d'un test doit raconter **quoi** est testé, **dans quelles conditions**, et le **résultat
attendu**. Une convention courante : `Methode_Condition_ResultatAttendu`.

```csharp
Addition_DeuxNombresPositifs_DonneLaSomme
Retrait_SoldeInsuffisant_Refuse
EstPair_NombreImpair_RetourneFaux
```

Quand un test échoue, son seul nom doit déjà te dire ce qui ne va pas — sans lire le corps.

## 5. Penser aux cas limites {#cas-limites}

Tester l'addition de `2 + 3` ne suffit pas : les bugs se cachent surtout aux **frontières**. Avant
de te dire « ça marche », passe en revue les **cas limites** :

- **Zéro** : `0` est-il traité correctement ? (ni positif, ni négatif !)
- **Le vide** : une liste vide, une chaîne `""`, zéro élément.
- **Le négatif** : les calculs tiennent-ils avec des valeurs négatives ?
- **`null`** : que se passe-t-il si une référence est absente ?
- **Les bornes** : le tout premier et le tout dernier élément, le plus petit / le plus grand.

> Le réflexe à acquérir : pour chaque fonction, demande-toi « quelle est l'entrée **bizarre** qui
> pourrait la faire échouer ? » — et écris un test pour elle. Le `0` est l'oubli classique : il
> n'est ni positif ni négatif, il mérite son propre cas.

## 6. Notion de couverture {#assertion}

La **couverture** (*code coverage*) mesure quelle part de ton code est réellement exécutée par tes
tests. Une couverture de 80 % signifie que 80 % des lignes ont été parcourues au moins une fois
pendant les tests.

C'est un indicateur utile mais **pas une garantie** : du code parcouru n'est pas forcément du code
bien vérifié. Mieux vaut quelques tests qui visent les bons **cas limites** qu'une foule de tests
qui ne posent aucune vraie question. La qualité des `Assert` compte plus que le pourcentage.

## 7. Dans cette piscine : tes tests affrontent des mutants

Les exercices exécutent les **vrais fichiers xUnit que tu écris**. Le moteur commence par lancer ta
suite contre l'implémentation correcte : tout doit être vert. Il introduit ensuite, un par un, de
petits bugs intentionnels. Un exercice est réussi lorsque tes tests détectent chacun de ces mutants.

Cette boucle évite les tests décoratifs : un `[Fact]` présent ne suffit pas, il doit défendre un
comportement précis. Tu travailleras successivement une opération simple, une frontière métier,
une exception, puis un objet avec état.

### Exercices du module

- **ex00-assertion** : écrire un premier `[Fact]` qui distingue addition et soustraction.
- **ex01-aaa** : organiser AAA autour de la frontière exacte de 18 ans.
- **ex02-cas-limites** : vérifier à la fois un résultat et une exception.
- **ex03-mutation** : couvrir bornes et effets de bord d'un objet mutable.

## La mutation : écrire des tests qui attrapent les bugs {#mutation}

Un test qui ne casse jamais ne sert à rien. Pour vérifier qu'une suite de tests est utile,
on « mute » le code correct : on y introduit un petit bug (par exemple `<=` devient `<`, ou
un débit `-= montant` devient `-= 0`). Un bon test doit alors **échouer** sur la version boguée :
on dit qu'il « tue » le mutant. Un mutant qui survit révèle un comportement que tes tests ne
vérifient pas — pense aux **cas limites** (la borne `montant == Solde`) et aux **effets de bord**
(le solde a-t-il bien diminué ?).

## Références externes

- Microsoft Learn — *Tests unitaires C# avec xUnit* :
  <https://learn.microsoft.com/fr-fr/dotnet/core/testing/unit-testing-csharp-with-xunit>
- Microsoft Learn — *Bonnes pratiques pour les tests unitaires* :
  <https://learn.microsoft.com/fr-fr/dotnet/core/testing/unit-testing-best-practices>
- Vidéo (anglais) — *xUnit Testing in .NET* (Nick Chapsas) :
  <https://www.youtube.com/watch?v=ZXdFisA_hOY>
