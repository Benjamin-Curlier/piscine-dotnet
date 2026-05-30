# Module 10 — Génériques & lambdas

Jusqu'ici, le code traitait des types précis. Ce module introduit deux outils qui rendent le code
**réutilisable** et **plus expressif** : les **génériques** (écrire une fois pour plusieurs types)
et les **lambdas** (passer une fonction comme une valeur).

## 1. Pourquoi les génériques {#generiques}

Tu connais déjà `List<int>`, `List<string>` (module 06). Le `<...>` indique le **type des
éléments** : une même classe `List<T>` fonctionne pour `int`, `string`, ou n'importe quel type.
Sans les génériques, il faudrait réécrire une `ListeDeInt`, une `ListeDeString`, etc. Les
génériques évitent cette duplication : on écrit **un** code, valable pour **plusieurs** types.

Le `T` est un **paramètre de type** : un nom de remplacement choisi au moment de l'utilisation.

## 2. Écrire une classe générique {#boite}

On déclare le paramètre de type entre chevrons juste après le nom de la classe :

```csharp
class Boite<T>
{
    public T Contenu { get; }
    public Boite(T contenu) => Contenu = contenu;
    public string Decrire() => $"Boite contient: {Contenu}";
}
```

À l'usage, on remplace `T` par un type concret :

```csharp
var b1 = new Boite<string>("bonjour");   // T = string
var b2 = new Boite<int>(42);             // T = int
System.Console.WriteLine(b1.Decrire());  // Boite contient: bonjour
System.Console.WriteLine(b2.Decrire());  // Boite contient: 42
```

> L'interpolation `$"...{Contenu}..."` appelle automatiquement `ToString()` sur la valeur, quel
> que soit son type.

Une **méthode** peut aussi être générique (`T Premier<T>(...)`), mais on s'en tient ici aux
classes génériques.

## 3. Les délégués et `Func` / `Action`

Une **lambda** est une mini-fonction sans nom. Pour la **ranger dans une variable**, il faut un
type qui décrit « une fonction ». C'est le rôle des **délégués** standard, situés dans `System` :

- **`Func<...>`** : une fonction qui **renvoie** une valeur. Le **dernier** type entre chevrons est
  le type de retour ; les précédents sont les paramètres.
  - `Func<int, int>` : prend un `int`, renvoie un `int`.
  - `Func<int, bool>` : prend un `int`, renvoie un `bool` (un **prédicat** : une question oui/non).
  - `Func<int, int, int>` : prend deux `int`, renvoie un `int`.
- **`Action<...>`** : une fonction qui **ne renvoie rien** (`void`), seulement des paramètres.
  - `Action<string>` : prend un `string`, ne renvoie rien.

```csharp
using System;
```

> `Func<>` et `Action<>` vivent dans `System` : pense au `using System;`.

## 4. La syntaxe lambda {#lambdas}

Une lambda s'écrit `paramètres => expression`. Le `=>` se lit « va vers » ou « donne ».

```csharp
using System;

Func<int, int> carre = x => x * x;          // x donne x * x
Func<int, bool> estPair = x => x % 2 == 0;  // x donne vrai si x est pair
Action<string> saluer = nom => System.Console.WriteLine($"Salut {nom}");
```

On **appelle** une lambda comme une méthode classique :

```csharp
System.Console.WriteLine(carre(5));    // 25
System.Console.WriteLine(estPair(4));  // True
saluer("Ada");                         // Salut Ada
```

### Plusieurs paramètres

Avec deux paramètres ou plus, on **parenthèse** la liste :

```csharp
Func<int, int, int> somme = (a, b) => a + b;
System.Console.WriteLine(somme(3, 4));  // 7
```

Sans paramètre, on met des parenthèses vides : `() => 42`.

## 5. Lien avec `List.Sort` et les prédicats

Beaucoup de méthodes acceptent une fonction en argument. C'est pratique pour décrire un
**comportement** à la volée. Par exemple, trier une liste selon une règle, ou filtrer avec un
prédicat :

```csharp
using System;
using System.Collections.Generic;

var nombres = new List<int> { 3, 1, 2 };
nombres.Sort((a, b) => a - b);                 // tri croissant via une lambda
var pairs = nombres.FindAll(x => x % 2 == 0);  // garde les pairs (prédicat)
```

La lambda passée à `Sort` ou `FindAll` est exactement un `Func<>` comme ceux vus plus haut.

### Exercices du module

- **[ex00-applique](#applique)** : ranger une lambda `Func<int,int>` et l'appliquer à des valeurs.
- **[ex01-filtre](#filtre)** : utiliser un prédicat `Func<int,bool>` pour filtrer.
- **[ex02-boite](#boite-ex)** : écrire et utiliser une classe générique `Boite<T>`.

#### applique {#applique}
Lis N entiers, déclare `Func<int, int> carre = x => x * x;`, affiche le carré de chacun.

#### filtre {#filtre}
Lis N entiers, déclare `Func<int, bool> estPair = x => x % 2 == 0;`, n'affiche que les pairs.

#### boite {#boite-ex}
Range un mot dans une `Boite<string>` et un nombre dans une `Boite<int>`, puis décris chacune.

## Références externes

- Microsoft Learn — *Génériques (C#)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/types/generics>
- Microsoft Learn — *Délégués* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/delegates/>
- Microsoft Learn — *Expressions lambda* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/operators/lambda-expressions>
- Vidéo — Nick Chapsas, *C# Generics* : <https://www.youtube.com/watch?v=K1Lu4oa6mZQ>
- Vidéo — Tim Corey, *C# Lambda Expressions* : <https://www.youtube.com/watch?v=R8Blt5c-Vi4>
