# Module 36 — Clean Architecture (couches & dépendances)

Au fil des modules tu as appris l'**injection de dépendances**, les **interfaces** et la **POO**.
Ce module les assemble en une **architecture** : comment **organiser** une application pour qu'elle
reste testable et modifiable quand elle grossit.

L'idée centrale tient en une phrase : **les détails dépendent de la logique métier, jamais
l'inverse.** Une base de données, un fichier, un framework web sont des *détails* — ils doivent
pouvoir changer sans toucher au cœur métier.

---

## 1. Les couches {#couches}

On découpe l'application en couches concentriques, du cœur vers l'extérieur :

- **Domain** (le cœur) : les **entités** et les **règles métier**. Du C# pur, **aucune dépendance**
  technique. Ici vivent aussi les **ports** : des interfaces qui décrivent *ce dont le métier a
  besoin* (ex. « savoir persister une tâche ») sans dire *comment*.
- **Application** : les **cas d'usage** qui orchestrent le domaine. Cette couche dépend du Domain
  (ses entités et ses ports), **mais pas** des détails techniques.
- **Infrastructure** : les **adaptateurs** concrets — implémentations des ports (base de données,
  mémoire, réseau). Elle dépend du Domain (elle implémente ses interfaces).
- **Composition root** : le point d'entrée (`Program`) qui **câble** le tout : il choisit les
  implémentations concrètes et les injecte. C'est le **seul** endroit qui connaît `Infrastructure`.

## 2. La règle de dépendance {#regle-de-dependance}

> **Les dépendances pointent toujours vers l'intérieur.** Une couche ne connaît que les couches
> plus internes qu'elle.

Concrètement :

- **Domain** ne référence **ni Application ni Infrastructure**.
- **Application** ne référence **que Domain** (jamais Infrastructure).
- **Infrastructure** peut référencer Domain (elle implémente ses ports).
- **Program** (composition root) peut tout référencer, car il câble.

Comment Application utilise-t-elle une base de données sans en dépendre ? Par **inversion de
dépendance** : le Domain définit un **port** (interface `IDepotTaches`), Application travaille avec
ce port, et Infrastructure en fournit une **implémentation** (`DepotMemoire`). Au démarrage, la
composition root injecte l'implémentation concrète. Application ne voit que l'abstraction.

```csharp
// Domain : le port (abstraction)
public interface IDepotTaches { Tache Ajouter(string titre); /* … */ }

// Application : dépend du PORT, pas de l'implémentation
public sealed class GestionTaches
{
    private readonly IDepotTaches _depot;
    public GestionTaches(IDepotTaches depot) => _depot = depot;
}

// Infrastructure : l'adaptateur concret
public sealed class DepotMemoire : IDepotTaches { /* … */ }

// Program : la composition root câble le tout
var gestion = new GestionTaches(new DepotMemoire());
```

## 3. Ports & adaptateurs {#ports-adaptateurs}

Cette inversion s'appelle aussi **ports & adaptateurs** (architecture hexagonale) :

- un **port** = une interface définie par le métier (ce dont il a besoin) ;
- un **adaptateur** = une implémentation technique branchée sur ce port.

Le bénéfice : tu peux remplacer `DepotMemoire` par `DepotSql` **sans toucher** à Application ni
Domain. Tu peux aussi tester Application avec un faux dépôt en mémoire. Le métier est **isolé** des
détails.

## 4. Pourquoi se donner cette peine ? {#pourquoi}

- **Testabilité** : le cœur métier se teste sans base de données ni réseau.
- **Évolutivité** : changer un détail technique n'impacte pas le métier.
- **Lisibilité** : chaque couche a une responsabilité claire ; les dépendances sont explicites.

Sur un petit projet, c'est parfois trop ; mais dès qu'une application doit durer et grandir, la
règle de dépendance évite que tout se transforme en plat de spaghettis.

## Références externes

- Microsoft Learn — *Common web application architectures* (Clean Architecture).
- Robert C. Martin — *The Clean Architecture* (blog, 2012).
- *Hexagonal Architecture* (Alistair Cockburn) — ports & adaptateurs.
