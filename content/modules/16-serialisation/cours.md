# Module 16 — Sérialisation : System.Text.Json

Jusqu'ici, les objets que tu crées vivent **en mémoire** : ils disparaissent à la fin du
programme. Pour les **sauvegarder dans un fichier** ou les **envoyer sur le réseau**, il faut les
transformer en texte. C'est la **sérialisation**. L'opération inverse — reconstruire un objet à
partir de ce texte — s'appelle la **désérialisation**.

Le format le plus répandu est **JSON** (*JavaScript Object Notation*) : lisible par un humain,
compris par presque tous les langages. En .NET, la bibliothèque standard pour cela est
**`System.Text.Json`**.

```csharp
using System.Text.Json;
```

> Comme toujours dans la piscine, n'oublie pas le `using` : ici `using System.Text.Json;` est
> **obligatoire** pour accéder à `JsonSerializer`.

## 1. Sérialiser un objet {#serialiser}

`JsonSerializer.Serialize(...)` prend un objet et renvoie sa représentation **JSON sous forme de
chaîne** :

```csharp
using System.Text.Json;

var p = new Personne { Nom = "Alice", Age = 30 };
var json = JsonSerializer.Serialize(p);
System.Console.WriteLine(json);   // {"Nom":"Alice","Age":30}

class Personne
{
    public string Nom { get; set; } = string.Empty;
    public int Age { get; set; }
}
```

Deux règles à retenir sur la sortie produite :

- les propriétés apparaissent **dans leur ordre de déclaration** (`Nom` avant `Age`) ;
- les noms gardent leur **casse d'origine** (PascalCase) : `Nom`, `Age`.

C'est pourquoi le JSON obtenu est exactement `{"Nom":"Alice","Age":30}`, sans espace.

## 2. Désérialiser : reconstruire un objet {#deserialiser}

`JsonSerializer.Deserialize<T>(...)` fait le chemin inverse : à partir d'une chaîne JSON, il
recrée un objet du type `T` :

```csharp
using System.Text.Json;

var ligne = "{\"Nom\":\"Alice\",\"Age\":30}";
var p = JsonSerializer.Deserialize<Personne>(ligne);
System.Console.WriteLine($"Nom: {p!.Nom}, Age: {p.Age}");   // Nom: Alice, Age: 30
```

Les **noms du JSON** doivent correspondre aux **noms des propriétés** : `"Nom"` remplit `Nom`,
`"Age"` remplit `Age`. Un nom inconnu est simplement ignoré ; une propriété absente garde sa
valeur par défaut.

> **L'opérateur `!`** : `Deserialize<T>` peut théoriquement renvoyer `null` (si le JSON vaut
> `null`). Le compilateur émet alors un avertissement. En écrivant `p!.Nom`, tu affirmes que `p`
> n'est pas `null` — l'avertissement disparaît.

## 3. Sérialiser une liste {#listes}

`JsonSerializer.Serialize` fonctionne aussi sur les **collections**. Une `List<T>` devient un
**tableau JSON**, entre crochets :

```csharp
using System.Text.Json;
using System.Collections.Generic;

var liste = new List<string> { "a", "b", "c" };
var json = JsonSerializer.Serialize(liste);
System.Console.WriteLine(json);   // ["a","b","c"]
```

> Rappel (module 06) : `List<>` exige `using System.Collections.Generic;`.

De la même façon, une `List<Personne>` produirait un tableau d'objets :
`[{"Nom":"Alice","Age":30},{"Nom":"Bob","Age":25}]`.

## 4. Options de sérialisation (survol)

Par défaut, le JSON est **compact** (aucun espace) et conserve le **PascalCase**. On peut changer
ce comportement avec un objet `JsonSerializerOptions` passé en second argument :

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,                                 // JSON aéré, sur plusieurs lignes
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase     // "nom", "age" au lieu de "Nom", "Age"
};
var json = JsonSerializer.Serialize(p, options);
```

- `WriteIndented = true` ajoute retours à la ligne et indentation (utile pour un fichier lisible).
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` met la première lettre en minuscule —
  c'est la convention habituelle des API web.

Pour des cas particuliers (formater une date autrement, convertir un type maison…), on peut écrire
un **converter personnalisé** (`JsonConverter<T>`). C'est un sujet avancé : retiens simplement que
le mécanisme est extensible.

> Dans ce module, on n'utilise **pas** d'options : la sortie attendue est le JSON compact en
> PascalCase, exactement ce que produit `Serialize(p)` sans second argument.

### Exercices du module

- **[ex00-serialiser](#serialiser)** : transformer une `Personne` en JSON.
- **[ex01-deserialiser](#deserialiser)** : reconstruire une `Personne` depuis du JSON.
- **[ex02-liste-json](#listes)** : sérialiser une `List<string>` en tableau JSON.

#### serialiser {#serialiser}
Lis un nom et un âge, construis une `Personne` et affiche `JsonSerializer.Serialize(p)`.

#### deserialiser {#deserialiser}
Lis une ligne JSON, fais `JsonSerializer.Deserialize<Personne>(ligne)`, affiche `Nom: X, Age: Y`.

#### listes {#listes}
Lis N puis N noms, remplis une `List<string>` et affiche le tableau JSON.

## Références externes

- Microsoft Learn — *Sérialisation et désérialisation JSON (System.Text.Json)* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/serialization/system-text-json/overview>
- Microsoft Learn — *Comment sérialiser et désérialiser du JSON* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/serialization/system-text-json/how-to>
- Vidéo — *JSON Serialization in .NET (System.Text.Json)*, dotnet (YouTube) :
  <https://www.youtube.com/watch?v=zb8De2P0jSE>
