# Module 08 — POO 2 : Héritage & polymorphisme

Au module précédent, une classe regroupait données et comportements. Ici, des classes
**partagent** et **spécialisent** ces comportements grâce à l'**héritage**, et un même appel
produit des résultats différents selon l'objet : c'est le **polymorphisme**.

## 1. Héritage

Une classe **dérivée** réutilise tout ce qu'offre sa classe **de base**, et ajoute le sien.
On écrit `class Dérivée : Base` :

```csharp
class Animal
{
    public string Nom { get; set; } = string.Empty;
}

class Chien : Animal   // un Chien EST un Animal : il a aussi Nom
{
}
```

## 2. Redéfinir : `virtual` / `override`

Une méthode marquée **`virtual`** dans la base peut être **redéfinie** dans une dérivée avec
**`override`** :

```csharp
class Animal
{
    public virtual string Cri() => "...";
}

class Chien : Animal
{
    public override string Cri() => "Wouf";   // remplace le cri par défaut
}

class Chat : Animal
{
    public override string Cri() => "Miaou";
}
```

## 3. Polymorphisme

Un `Chien` et un `Chat` sont tous deux des `Animal`. On peut donc les ranger ensemble et appeler
`Cri()` sur chacun : **c'est la version de l'objet réel qui s'exécute**, pas celle de la base.

```csharp
using System.Collections.Generic;

var animaux = new List<Animal> { new Chien(), new Chat() };
foreach (var animal in animaux)
{
    System.Console.WriteLine(animal.Cri());   // Wouf, puis Miaou
}
```

> Rappel (module 06) : `List<>` exige `using System.Collections.Generic;`.

## 4. Interfaces {#interface}

Une **interface** décrit un **contrat** : une liste de méthodes, sans code. Une classe qui
**implémente** l'interface s'engage à les fournir. Par convention, son nom commence par `I` :

```csharp
interface IForme
{
    int Aire();
}

class Carre : IForme
{
    public int Cote { get; set; }
    public int Aire() => Cote * Cote;
}
```

Comme pour l'héritage, on traite des objets variés à travers leur contrat commun
(`List<IForme>`, puis `forme.Aire()`).

## 5. Classe abstraite {#abstraite}

Une classe **`abstract`** ne peut pas être instanciée directement : elle sert de base commune.
Une méthode **`abstract`** n'a pas de corps — chaque dérivée **doit** la fournir :

```csharp
abstract class Employe
{
    public abstract int SalaireMensuel();   // pas de corps : à définir
}

class EmployeFixe : Employe
{
    private readonly int _salaire;
    public EmployeFixe(int salaire) => _salaire = salaire;
    public override int SalaireMensuel() => _salaire;
}
```

`new Employe()` est interdit ; on instancie une dérivée concrète comme `EmployeFixe`.

### Exercices du module

- **[ex00-animal](#animal)** : héritage et redéfinition `virtual`/`override`, polymorphisme.
- **[ex01-forme](#forme)** : une interface `IForme` implémentée par plusieurs formes.
- **[ex02-employe](#employe)** : une classe abstraite avec méthode abstraite.

#### animal {#animal}
Lis des types d'animaux, crée le bon objet (`Chien`/`Chat`), affiche le cri de chacun.

#### forme {#forme}
Lis les dimensions d'un carré et d'un rectangle, affiche l'aire de chaque forme via `IForme`.

#### employe {#employe}
Lis un salaire fixe et le couple (base, commission) d'un commercial ; affiche la masse salariale.

## Pour aller plus loin

- Microsoft Learn — *Héritage* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/object-oriented/inheritance>
- Microsoft Learn — *Polymorphisme* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/object-oriented/polymorphism>
- Microsoft Learn — *Interfaces* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/types/interfaces>
