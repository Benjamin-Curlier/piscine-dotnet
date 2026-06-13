# Module 06 — Collections

Les **tableaux** ont une taille fixe. Les **collections** de cette leçon grandissent et rétrécissent
à volonté, et offrent des opérations puissantes : `List`, `Dictionary`, et un premier contact avec
**LINQ**.

> Astuce : ces types vivent dans `System.Collections.Generic`. Tu peux écrire le nom complet
> (`System.Collections.Generic.List<int>`) ou ajouter `using System.Collections.Generic;` en haut
> du fichier. Pour LINQ, ajoute `using System.Linq;`.

## 1. `List<T>` — une suite ordonnée et redimensionnable

```csharp
var nombres = new System.Collections.Generic.List<int>();
nombres.Add(10);          // ajoute à la fin
nombres.Add(5);
System.Console.WriteLine(nombres.Count);   // 2
System.Console.WriteLine(nombres[0]);      // 10
nombres.Sort();                            // trie sur place : { 5, 10 }
```

On la parcourt comme un tableau (`for`, `foreach`). `string.Join(' ', nombres)` recompose une
chaîne : `"5 10"`.

## 2. `Dictionary<TKey, TValue>` — des paires clé → valeur

Un **dictionnaire** associe une **clé** à une **valeur**, avec un accès direct par la clé :

```csharp
var ages = new System.Collections.Generic.Dictionary<string, int>();
ages["Alice"] = 30;
ages["Bob"] = 25;
System.Console.WriteLine(ages["Alice"]);          // 30
System.Console.WriteLine(ages.ContainsKey("Eve")); // False
```

`GetValueOrDefault(cle)` renvoie la valeur, ou `0` (défaut du type) si la clé est absente — pratique
pour **compter** (il faut `using System.Collections.Generic;` pour cette méthode) :

```csharp
freq[mot] = freq.GetValueOrDefault(mot) + 1;   // incrémente le compteur du mot
```

## 3. Premier contact avec LINQ {#linq}

**LINQ** ajoute des opérations de requête sur n'importe quelle collection (avec `using System.Linq;`) :

```csharp
using System.Linq;

int[] valeurs = { 1, 2, 3, 4, 5, 6 };
var pairs    = valeurs.Where(x => x % 2 == 0);  // 2, 4, 6
var total    = valeurs.Where(x => x % 2 == 0).Sum();   // 12
var combien  = valeurs.Count(x => x > 3);       // 3
```

`Where` filtre, `Select` transforme, `Sum`/`Count`/`Max`/`Min` agrègent. On approfondira LINQ dans
un module dédié.

### Exercices du module

- **[ex00-tri-liste](#tri-liste)** : trier une liste de nombres.
- **[ex01-frequence](#frequence)** : compter les occurrences d'un mot avec un dictionnaire.
- **[ex02-somme-pairs](#somme-pairs)** : sommer les nombres pairs avec LINQ.
- **[ex03-top-k](#top-k)** : *(bonus, difficile)* afficher les K mots les plus fréquents.

#### tri-liste {#tri-liste}
Lis des nombres séparés par des espaces, affiche-les **triés** par ordre croissant (`List` + `Sort`).

#### frequence {#frequence}
Lis une ligne de mots, puis un mot cible ; affiche combien de fois la cible apparaît (`Dictionary`).

#### somme-pairs {#somme-pairs}
Lis des nombres, affiche la somme des **pairs** (`Where` + `Sum`).

#### top-k {#top-k}
*(Bonus, difficile)* Lis une ligne de mots, puis un entier `K` ; affiche les `K` mots les plus
fréquents (un par ligne), du plus fréquent au moins fréquent, **ex-aequo départagés par ordre
alphabétique**. Indice : compte dans un `Dictionary<string,int>`, puis
`OrderByDescending(fréquence).ThenBy(mot).Take(K)`. Le `ThenBy` garantit un ordre **déterministe**.

## Pour aller plus loin

- Microsoft Learn — *Collections* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/concepts/collections>
- Microsoft Learn — *Introduction à LINQ* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/linq/>
