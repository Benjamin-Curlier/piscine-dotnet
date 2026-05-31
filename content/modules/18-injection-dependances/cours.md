# Module 18 — Injection de dépendances

Jusqu'ici, quand une classe avait besoin d'un service, elle le **créait elle-même** avec `new`.
C'est simple, mais cela **soude** les classes entre elles : difficile à faire évoluer, difficile
à tester. L'**injection de dépendances** (DI) renverse ce principe : une classe **déclare** ce
dont elle a besoin, et c'est un **conteneur** qui le lui **fournit**. On utilise ici le conteneur
standard de .NET : `Microsoft.Extensions.DependencyInjection`.

## 1. Pourquoi la DI ?

Comparons deux façons d'utiliser un service `Multiplieur` dans un service `Traitement` :

```csharp
// SANS DI : Traitement fabrique sa dépendance lui-même (couplage fort)
class Traitement
{
    private readonly Multiplieur _m = new Multiplieur();   // soudé à CETTE classe
}

// AVEC DI : Traitement reçoit sa dépendance de l'extérieur (couplage faible)
class Traitement
{
    private readonly Multiplieur _m;
    public Traitement(Multiplieur m) => _m = m;   // on me la donne
}
```

Deux bénéfices majeurs :

- **Couplage faible** : `Traitement` ne décide plus *comment* `Multiplieur` est construit. On peut
  changer la construction (ou remplacer par une autre implémentation) sans toucher `Traitement`.
- **Testabilité** : dans un test, on injecte une version factice de la dépendance. La classe n'est
  pas prisonnière du `new` qu'elle a écrit en dur.

## 2. `IServiceCollection` et `ServiceProvider` {#resolution}

Le conteneur se construit en deux temps :

1. On **décrit** les services dans une `ServiceCollection` (« voici ce qui existe »).
2. On **construit le provider** avec `BuildServiceProvider()` (« prépare-toi à les fournir »).
3. On **résout** un service avec `GetRequiredService<T>()` (« donne-moi une instance »).

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IGreeter, Greeter>();   // « IGreeter, c'est Greeter »
var provider = services.BuildServiceProvider();

var greeter = provider.GetRequiredService<IGreeter>();   // une instance de Greeter
System.Console.WriteLine(greeter.Saluer("Alice"));

interface IGreeter
{
    string Saluer(string nom);
}

class Greeter : IGreeter
{
    public string Saluer(string nom) => $"Bonjour, {nom}!";
}
```

`AddSingleton<IGreeter, Greeter>()` enregistre une **correspondance** : « quand on me demande
`IGreeter`, fournis un `Greeter` ». Le code qui consomme ne dépend alors que du **contrat**
`IGreeter`, jamais de la classe concrète.

> `GetRequiredService<T>()` lève une exception claire si `T` n'est pas enregistré. C'est ce qu'on
> veut pendant l'apprentissage : une erreur explicite plutôt qu'un `null` silencieux.

## 3. Injection par constructeur {#dependance}

C'est le cœur de la DI : un service déclare ses besoins dans son **constructeur**, et le conteneur
les **remplit automatiquement** au moment de la résolution.

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<Multiplieur>();    // la dépendance
services.AddSingleton<Traitement>();     // le service qui en a besoin
var provider = services.BuildServiceProvider();

var traitement = provider.GetRequiredService<Traitement>();
System.Console.WriteLine(traitement.Traiter(5));   // 10

class Multiplieur
{
    public int Doubler(int x) => x * 2;
}

class Traitement
{
    private readonly Multiplieur _m;
    public Traitement(Multiplieur m) => _m = m;   // le conteneur injecte le Multiplieur
    public int Traiter(int n) => _m.Doubler(n);
}
```

On n'a **jamais écrit `new Traitement(...)`**. Le conteneur a vu que `Traitement` réclamait un
`Multiplieur`, l'a construit (il était enregistré), puis l'a passé au constructeur. C'est la
**résolution en chaîne** : pour fournir `Traitement`, le conteneur fournit d'abord ses dépendances.

## 4. Les trois durées de vie {#durees-vie}

Quand on enregistre un service, on choisit **combien de temps** une instance vit. Trois choix :

| Méthode | Durée de vie | Instance |
| --- | --- | --- |
| `AddSingleton<T>()` | toute la vie de l'application | **une seule**, partagée |
| `AddScoped<T>()` | la durée d'un *scope* (ex. une requête web) | une par scope |
| `AddTransient<T>()` | aucune mémoire | **une nouvelle** à chaque résolution |

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<CompteurSingleton>();   // même instance partout
services.AddTransient<CompteurTransient>();   // une instance neuve à chaque fois
var provider = services.BuildServiceProvider();

// Singleton : la même instance → le compteur continue
System.Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer()); // 1
System.Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer()); // 2

// Transient : une instance neuve → le compteur repart de zéro
System.Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer()); // 1
System.Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer()); // 1

class CompteurSingleton
{
    private int _n;
    public int Incrementer() => ++_n;
}

class CompteurTransient
{
    private int _n;
    public int Incrementer() => ++_n;
}
```

Le **singleton** garde son état entre les résolutions (le `_n` survit) ; le **transient** redémarre
à chaque fois car c'est un objet tout neuf. `Scoped` est entre les deux : on le rencontre surtout
en web (une instance par requête HTTP), inutile dans nos petits programmes en console.

> Règle simple : **singleton** pour un service sans état ou un cache partagé, **transient** pour un
> service léger qu'on veut frais à chaque usage, **scoped** pour « une fois par requête ».

### Exercices du module

- **[ex00-resolution](#resolution)** : enregistrer, construire le provider, résoudre un service.
- **[ex01-dependance](#dependance)** : injecter un service dans un autre par le constructeur.
- **[ex02-durees-vie](#durees-vie)** : observer la différence entre singleton et transient.

#### resolution {#resolution-exo}
Enregistre `IGreeter` → `Greeter` en singleton, résous `IGreeter`, affiche `Saluer(nom)`.

#### dependance {#dependance-exo}
`Multiplieur` est injecté dans `Traitement` par son constructeur ; affiche `Traiter(n)`.

#### durees-vie {#durees-vie-exo}
Résous deux fois un singleton (état conservé) et deux fois un transient (état remis à zéro).

## Références externes

- Microsoft Learn — *Injection de dépendances dans .NET* :
  <https://learn.microsoft.com/fr-fr/dotnet/core/extensions/dependency-injection>
- Microsoft Learn — *Durées de vie des services* :
  <https://learn.microsoft.com/fr-fr/dotnet/core/extensions/dependency-injection#service-lifetimes>
- Nick Chapsas — *Dependency Injection in .NET explained* (vidéo, anglais) :
  <https://www.youtube.com/watch?v=oxqDDpvDc4g>
