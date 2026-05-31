# Module 17 — Réflexion & attributs

Jusqu'ici, ton code manipulait des types qu'il connaissait à l'avance : tu écrivais
`produit.Prix` et le compilateur savait que `Prix` existait. La **réflexion** permet au contraire
d'**inspecter un type pendant l'exécution** : lister ses propriétés, lire le nom de leurs types,
trouver les **attributs** posés dessus — sans les avoir écrits en dur. C'est le mécanisme derrière
beaucoup d'outils (sérialiseurs JSON, frameworks de test, conteneurs d'injection).

## 1. `typeof` et `GetType`

Pour réfléchir, il faut d'abord obtenir l'objet `Type` qui **décrit** une classe :

```csharp
var t1 = typeof(Produit);        // à partir du nom du type (connu à la compilation)
var produit = new Produit();
var t2 = produit.GetType();      // à partir d'un objet existant
```

Les deux renvoient le même `Type`. On utilise `typeof(...)` quand on connaît le type par son nom,
et `GetType()` quand on n'a qu'un objet sous la main.

## 2. La classe `Type`

Un `Type` répond à des questions sur la classe : son nom (`Name`), ses propriétés, ses méthodes,
ses attributs... C'est le point d'entrée de toute la réflexion. Les outils de réflexion vivent
dans l'espace de noms `System.Reflection`, qu'il faut importer explicitement :

```csharp
using System.Reflection;
```

## 3. Lire une propriété : `GetProperty` / `PropertyType` {#type-propriete}

`GetProperty(nom)` renvoie un `PropertyInfo` décrivant **une** propriété. Comme la propriété
demandée peut ne pas exister, le résultat est **nullable** : on ajoute `!` quand on est sûr de
son existence.

```csharp
using System.Reflection;

var propriete = typeof(Produit).GetProperty("Prix")!;   // PropertyInfo
System.Console.WriteLine(propriete.PropertyType.Name);  // Double
```

- `PropertyType` est le `Type` de la propriété (ici `double`).
- `.Name` en donne le **nom court** : `String`, `Double`, `Int32`...

## 4. Lister les propriétés : `GetProperties` {#lister}

`GetProperties()` renvoie un **tableau** de `PropertyInfo` (toutes les propriétés publiques) :

```csharp
using System.Linq;
using System.Reflection;

var noms = typeof(Produit).GetProperties()
    .Select(p => p.Name)
    .OrderBy(n => n);          // tri alphabétique pour un résultat déterministe

foreach (var nom in noms)
{
    System.Console.WriteLine(nom);
}
```

> ⚠️ L'ordre dans lequel la réflexion renvoie les propriétés **n'est pas garanti**. Si tu veux
> une sortie stable (utile pour comparer un résultat), trie-la toi-même avec `OrderBy`.

## 5. Écrire un attribut personnalisé {#attribut}

Un **attribut** est une étiquette posée sur un élément de code (classe, propriété, méthode...).
Tu en as déjà croisé : `[Obsolete]`, `[Fact]` en test... On en crée un en héritant de `Attribute` :

```csharp
using System;

class EtiquetteAttribute : Attribute
{
    public string Texte { get; }
    public EtiquetteAttribute(string texte) => Texte = texte;
}
```

Par **convention**, le nom se termine par `Attribute`, mais à l'usage on écrit la version courte :

```csharp
[Etiquette("Coucou")]
class MaClasse { }
```

> En survol : on peut restreindre **où** un attribut s'applique avec `AttributeUsage`, par exemple
> `[AttributeUsage(AttributeTargets.Class)]` pour n'autoriser que les classes. Optionnel ici.

## 6. Lire un attribut : `GetCustomAttribute<T>` {#lire-attribut}

À l'exécution, on retrouve l'attribut posé sur un type par réflexion :

```csharp
using System;
using System.Reflection;

var etiquette = typeof(MaClasse).GetCustomAttribute<EtiquetteAttribute>()!;
System.Console.WriteLine(etiquette.Texte);   // Coucou
```

`GetCustomAttribute<T>()` renvoie l'instance de l'attribut (ou `null` s'il est absent, d'où le `!`
quand on sait qu'il est présent). On lit ensuite ses propriétés comme sur n'importe quel objet.

## 7. Une note sur la performance

La réflexion est **puissante mais coûteuse** : inspecter un type à l'exécution est bien plus lent
qu'un accès direct comme `produit.Prix`. C'est très bien pour de l'outillage qui s'exécute
ponctuellement (chargement, configuration), mais à éviter dans une boucle critique exécutée des
millions de fois.

### Exercices du module

- **[ex00-type-propriete](#type-propriete)** : lire le type d'une propriété à partir de son nom.
- **[ex01-lister-proprietes](#lister)** : lister toutes les propriétés d'une classe, triées.
- **[ex02-attribut](#attribut)** : définir, appliquer et lire un attribut personnalisé.

## Références externes

- Microsoft Learn — *Réflexion (Reflection)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/concepts/reflection>
- Microsoft Learn — *Attributs* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/advanced-topics/reflection-and-attributes/>
- Microsoft Learn — *Créer des attributs personnalisés* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/attributes/writing-custom-attributes>
- Vidéo — *C# Reflection explained* (dotnet, YouTube) :
  <https://www.youtube.com/watch?v=tjC4Fp7CFGE>
