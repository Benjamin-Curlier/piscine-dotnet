# Module 07 — POO 1 : Classes & objets

Jusqu'ici tu écrivais des suites d'instructions. La **programmation orientée objet** (POO) regroupe
des **données** et les **comportements** qui agissent dessus dans des **classes**.

## 1. Déclarer une classe, créer un objet

Une **classe** est un moule ; un **objet** est un exemplaire créé avec `new` :

```csharp
var p = new Personne();
p.Nom = "Alice";
System.Console.WriteLine(p.Nom);   // Alice

class Personne
{
    public string Nom { get; set; }
}
```

> Dans un fichier en *top-level statements*, place tes classes **après** les instructions principales.

## 2. Propriétés

Une **propriété** expose une donnée avec un accès contrôlé (`get`/`set`). La forme courte
`{ get; set; }` est une **propriété auto-implémentée** :

```csharp
class Rectangle
{
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
}
```

On peut initialiser un objet et ses propriétés d'un coup (*object initializer*) :

```csharp
var r = new Rectangle { Largeur = 3, Hauteur = 4 };
```

## 3. Méthodes d'objet

Une classe contient aussi des **méthodes** qui travaillent sur **ses propres** données :

```csharp
class Rectangle
{
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public int Aire() => Largeur * Hauteur;   // utilise les propriétés de l'objet
}
```

## 4. Encapsulation {#encapsulation}

**Encapsuler**, c'est cacher l'état interne et n'exposer que des opérations sûres. Un champ
**`private`** n'est accessible que depuis la classe ; un **constructeur** initialise l'objet ; les
méthodes publiques contrôlent les modifications :

```csharp
class CompteBancaire
{
    private int _solde;
    public CompteBancaire(int soldeInitial) => _solde = soldeInitial;

    public int Solde => _solde;                       // lecture seule
    public void Deposer(int montant) => _solde += montant;
    public void Retirer(int montant)
    {
        if (montant <= _solde) _solde -= montant;     // règle métier protégée
    }
}
```

De l'extérieur, impossible de mettre `_solde` à n'importe quoi : on passe par `Deposer`/`Retirer`.

### Exercices du module

- **[ex00-rectangle](#rectangle)** : une classe avec propriétés et une méthode `Aire()`.
- **[ex01-personne](#personne)** : une classe qui se présente via une méthode.
- **[ex02-compte](#compte)** : encapsuler un solde derrière des opérations sûres.
- **[ex03-pile](#pile)** : *(bonus, difficile)* concevoir une pile (LIFO) avec Empiler/Depiler.

#### rectangle {#rectangle}
Lis largeur et hauteur, crée un `Rectangle`, affiche son aire.

#### personne {#personne}
Lis un nom et un âge, crée une `Personne`, affiche sa présentation.

#### compte {#compte}
Lis un solde initial, un dépôt et un retrait ; affiche le solde final (retrait refusé si insuffisant).

#### pile {#pile}
*(Bonus, difficile)* Conçois ta propre classe **pile** (LIFO) avec `Empiler`, `Depiler`, `EstVide`
(encapsule une `List<int>`). Lis `N`, puis `N` entiers que tu empiles ; dépile tout et affiche chaque
valeur (un par ligne) — l'ordre de sortie est l'**inverse** de l'ordre d'entrée.

## Pour aller plus loin

- Microsoft Learn — *Classes* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/types/classes>
- Microsoft Learn — *Propriétés* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/classes-and-structs/properties>
