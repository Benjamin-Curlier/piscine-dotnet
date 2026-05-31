# Module 23 — Design patterns : patrons GoF essentiels

Un **design pattern** (patron de conception) est une **solution éprouvée** à un problème de
conception qui revient souvent. Ce n'est pas du code à copier-coller, mais un **modèle** : une
façon d'organiser des classes et des objets. Leur premier intérêt est un **vocabulaire commun** :
dire « ici on utilise une *Factory* » résume une intention que toute l'équipe comprend aussitôt.

Les 23 patrons popularisés par le livre du *Gang of Four* (GoF) se rangent en trois catégories :

- **Création** : comment fabriquer des objets sans coupler le code à leurs classes concrètes
  (ex. **Factory**, Singleton, Builder).
- **Structure** : comment assembler classes et objets en structures plus grandes
  (ex. **Decorator**, Adapter, Composite).
- **Comportement** : comment les objets communiquent et se répartissent les responsabilités
  (ex. **Strategy**, **Observer**, Command).

Ce module détaille les trois patrons les plus utiles à un débutant : **Strategy**, **Factory** et
**Observer**.

> ⚠️ **Ne sur-architecturez pas** (principe *YAGNI* — *You Aren't Gonna Need It*). Un patron
> résout un vrai problème ; l'appliquer « au cas où » ajoute de la complexité inutile. Apprenez à
> les reconnaître, mais n'en mettez que lorsqu'ils simplifient réellement le code.

## 1. Strategy {#strategy}

Le patron **Strategy** **encapsule des algorithmes interchangeables** derrière une même interface,
pour pouvoir changer de comportement à l'exécution sans toucher au code qui les appelle.

```
          IOperation (Appliquer)
          /                \
   Addition            Multiplication
   (a + b)               (a * b)
```

```csharp
interface IOperation
{
    int Appliquer(int a, int b);
}

class Addition : IOperation
{
    public int Appliquer(int a, int b) => a + b;
}

class Multiplication : IOperation
{
    public int Appliquer(int a, int b) => a * b;
}

// Le code d'appel choisit une stratégie, puis l'utilise sans connaître son détail :
IOperation strategie = new Addition();
System.Console.WriteLine(strategie.Appliquer(3, 4));   // 7
```

Pour ajouter une `Soustraction`, on écrit une nouvelle classe : le code d'appel ne change pas.

## 2. Factory {#factory}

Le patron **Factory** (fabrique) **délègue la création d'objets** à un endroit unique. Le reste du
programme demande « donne-moi l'objet du type X » sans faire `new` lui-même ni connaître la classe
concrète.

```
"chien" ─┐
         ├─►  AnimalFactory.Creer(type)  ─►  IAnimal  (Chien / Chat)
"chat"  ─┘
```

```csharp
interface IAnimal
{
    string Cri();
}

class Chien : IAnimal
{
    public string Cri() => "Wouf";
}

class Chat : IAnimal
{
    public string Cri() => "Miaou";
}

static class AnimalFactory
{
    public static IAnimal Creer(string type)
    {
        if (type == "chien")
        {
            return new Chien();
        }

        return new Chat();
    }
}

// Le code d'appel ne fait pas de "new Chien()" : il demande à la fabrique.
IAnimal animal = AnimalFactory.Creer("chat");
System.Console.WriteLine(animal.Cri());   // Miaou
```

La logique de choix est **centralisée** : si on ajoute un animal, on ne modifie que la fabrique.

## 3. Observer {#observer}

Le patron **Observer** permet à un **sujet** de **notifier automatiquement des dépendants**
(les *observateurs*) quand un événement survient, sans connaître leur type concret. C'est le
principe des abonnements : le sujet diffuse, chaque abonné réagit.

```
                 ┌─► ObservateurA ([A] ...)
   Sujet.Diffuser┤
                 └─► ObservateurB ([B] ...)
```

```csharp
using System.Collections.Generic;

interface IObservateur
{
    void Notifier(string message);
}

class ObservateurA : IObservateur
{
    public void Notifier(string message) => System.Console.WriteLine("[A] " + message);
}

class Sujet
{
    private readonly List<IObservateur> _observateurs = new List<IObservateur>();

    public void Abonner(IObservateur observateur) => _observateurs.Add(observateur);

    public void Diffuser(string message)
    {
        foreach (var observateur in _observateurs)   // ordre d'abonnement
        {
            observateur.Notifier(message);
        }
    }
}
```

Le `Sujet` ne dépend que du contrat `IObservateur` : on peut ajouter ou retirer des abonnés sans
le modifier. (En C#, les `event` du langage reposent sur cette même idée.)

> Rappel (module 06) : `List<>` exige `using System.Collections.Generic;`.

## 4. Un mot sur Decorator

Le patron **Decorator** (structure) **enveloppe** un objet dans un autre qui partage la même
interface, pour lui **ajouter un comportement** sans modifier sa classe. Exemple typique : un flux
de base qu'on enveloppe d'un flux compressé, puis chiffré. On ne le pratique pas ici, mais retenez
l'idée : empiler des responsabilités par composition plutôt que par héritage.

## Exercices du module

- **[ex00-strategy](#strategy)** : encapsuler addition et multiplication derrière `IOperation`.
- **[ex01-factory](#factory)** : une fabrique `AnimalFactory` qui crée `Chien`/`Chat`.
- **[ex02-observer](#observer)** : un `Sujet` qui diffuse un message à deux observateurs.

## Références externes

- Refactoring.Guru — *Design Patterns* (FR) : <https://refactoring.guru/fr/design-patterns>
- Refactoring.Guru — *Strategy* : <https://refactoring.guru/fr/design-patterns/strategy>
- Refactoring.Guru — *Factory Method* : <https://refactoring.guru/fr/design-patterns/factory-method>
- Refactoring.Guru — *Observer* : <https://refactoring.guru/fr/design-patterns/observer>
- Microsoft Learn — *Interfaces* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/types/interfaces>
- Vidéo (FR) — *Les design patterns expliqués simplement* :
  <https://www.youtube.com/results?search_query=design+patterns+expliqu%C3%A9s+fran%C3%A7ais>
