# Module 11 — LINQ : Requêtes sur les collections

Aux modules précédents, tu parcourais une `List<int>` ou un `Dictionary<string, int>` à la
main, avec des `foreach` et des `if`. **LINQ** (*Language Integrated Query*) permet d'exprimer
ces traitements comme des **requêtes** : filtrer, transformer, trier, regrouper, calculer des
totaux — en quelques lignes lisibles.

## 1. Le `using` obligatoire

Toutes les méthodes LINQ vivent dans l'espace de noms `System.Linq`. Sans ce `using`, le
compilateur ne trouve ni `Where`, ni `Select`, ni `Sum`. **Pense toujours à l'ajouter** :

```csharp
using System.Linq;
```

LINQ s'applique à tout ce qui est énumérable : un tableau `int[]`, une `List<T>`, le résultat
d'un `Split(...)`, etc.

## 2. Filtrer et projeter : `Where` et `Select` {#where-select}

- **`Where`** garde les éléments qui satisfont une condition (un filtre).
- **`Select`** transforme chaque élément (une projection).

```csharp
using System.Linq;

int[] nombres = { 1, 2, 3, 4, 5, 6 };

var pairs = nombres.Where(x => x % 2 == 0);   // 2, 4, 6
var doubles = pairs.Select(x => x * 2);       // 4, 8, 12
```

La partie `x => ...` est une **expression lambda** : « pour chaque `x`, renvoie... ». On peut
enchaîner les appels :

```csharp
var resultat = nombres.Where(x => x % 2 == 0).Select(x => x * 2);   // 4, 8, 12
```

Pour afficher le tout sur une seule ligne, `string.Join` (pas de `using` nécessaire) :

```csharp
System.Console.WriteLine(string.Join(" ", resultat));   // 4 8 12
```

## 3. Calculer des totaux : les agrégats {#aggregation}

LINQ fournit des méthodes qui réduisent une collection à **une seule valeur** :

| Méthode     | Résultat                         |
| ----------- | -------------------------------- |
| `Count()`   | nombre d'éléments                |
| `Sum()`     | somme                            |
| `Min()`     | plus petit élément               |
| `Max()`     | plus grand élément               |
| `Average()` | moyenne (un `double`)            |

```csharp
using System.Linq;

int[] notes = { 1, 2, 3, 4 };

int somme = notes.Sum();              // 10
int mini = notes.Min();               // 1
int maxi = notes.Max();               // 4
double moyenne = notes.Average();     // 2.5
int moyenneEntiere = (int)notes.Average();   // 2 (tronquée par le cast)
```

Attention : `Average()` renvoie un `double`. Pour obtenir un entier **tronqué**, on caste avec
`(int)`, qui coupe la partie décimale (`2.5` devient `2`).

## 4. Trier : `OrderBy`, `OrderByDescending`, `ThenBy`

- **`OrderBy`** trie par ordre croissant selon une clé.
- **`OrderByDescending`** trie par ordre décroissant.
- **`ThenBy`** (ou `ThenByDescending`) départage les ex æquo selon une seconde clé.

```csharp
using System.Linq;

string[] mots = { "poire", "pomme", "kiwi", "ananas" };

var parLongueur = mots
    .OrderByDescending(m => m.Length)   // d'abord les plus longs
    .ThenBy(m => m);                    // à longueur égale, ordre alphabétique
```

## 5. Regrouper : `GroupBy` {#groupby}

**`GroupBy`** rassemble les éléments qui partagent une même clé. Chaque groupe expose sa clé
(`g.Key`) et son contenu (sur lequel on peut appeler `Count()`, `Sum()`, etc.). On combine
souvent `GroupBy` avec un `Select` pour produire un résultat plus simple :

```csharp
using System.Linq;

string[] mots = { "pomme", "poire", "pomme", "banane", "poire", "pomme" };

var compte = mots
    .GroupBy(m => m)                                  // un groupe par mot distinct
    .Select(g => new { Mot = g.Key, Compte = g.Count() })   // type anonyme
    .OrderByDescending(x => x.Compte)                 // les plus fréquents d'abord
    .ThenBy(x => x.Mot);                              // ex æquo : ordre alphabétique

foreach (var x in compte)
{
    System.Console.WriteLine($"{x.Mot}: {x.Compte}");
}
// pomme: 3
// poire: 2
// banane: 1
```

`new { Mot = ..., Compte = ... }` crée un **type anonyme** : un petit objet temporaire avec deux
propriétés, bien pratique pour transporter le résultat d'un `GroupBy`.

## 6. Exécution différée

Une requête LINQ comme `nombres.Where(...)` ne calcule **rien** au moment où on l'écrit : elle
décrit seulement le travail à faire. Les éléments ne sont produits qu'au moment où on les
**parcourt** (un `foreach`, un `string.Join`, etc.). C'est l'**exécution différée** (*deferred
execution*).

Pour figer le résultat dans une vraie collection, on appelle **`ToList()`** ou **`ToArray()`** :

```csharp
using System.Linq;

var pairs = nombres.Where(x => x % 2 == 0).ToList();   // calculé tout de suite
```

C'est utile quand on veut réutiliser le résultat plusieurs fois, ou appeler `.Length` / `.Count`.

## 7. Deux syntaxes équivalentes

LINQ s'écrit de deux façons. La **syntaxe méthode** (celle utilisée dans ce module) :

```csharp
var pairs = nombres.Where(x => x % 2 == 0).Select(x => x * 2);
```

Et la **syntaxe requête**, plus proche du SQL :

```csharp
var pairs = from x in nombres
            where x % 2 == 0
            select x * 2;
```

Les deux produisent le même résultat. La syntaxe méthode est la plus courante et la plus
souple ; c'est elle que nous privilégions.

### Exercices du module

- **[ex00-pairs-doubles](#where-select)** : filtrer (`Where`) puis projeter (`Select`).
- **[ex01-stats](#aggregation)** : agrégats `Sum` / `Min` / `Max` / `Average`.
- **[ex02-frequence-tri](#groupby)** : `GroupBy`, puis tri par fréquence décroissante.

## Références externes

- Microsoft Learn — *LINQ (Language Integrated Query)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/linq/>
- Microsoft Learn — *Vue d'ensemble des opérateurs de requête standard* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/linq/standard-query-operators/>
- Vidéo — Nick Chapsas, *LINQ Basics* : <https://www.youtube.com/watch?v=mp_W4_p5Zfk>
- Vidéo — Tim Corey, *Intro to LINQ in C#* : <https://www.youtube.com/watch?v=mmDg_5jBQ-o>
