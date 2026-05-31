# Module 32 — Garbage collection & gestion des ressources

En C#, tu n'écris (presque) jamais de `free` ni de `delete` : la mémoire est gérée pour toi par le
**ramasse-miettes** (*garbage collector*, GC). Mais toute ressource n'est pas de la mémoire — un
fichier ouvert, une connexion réseau, un verrou doivent être **libérés explicitement** et au bon
moment. Ce module explique le GC (en lecture) puis se concentre sur ce que tu contrôles vraiment :
`IDisposable` et `using`.

---

## 1. Comment fonctionne le GC (à lire)

Le GC suit les objets **accessibles** depuis tes variables (les *racines*). Quand un objet n'est
plus référencé par personne, il devient candidat à la collecte ; sa mémoire est récupérée
automatiquement, plus tard, à un moment **non déterministe**.

Le GC .NET est **générationnel** : il classe les objets par âge pour collecter efficacement.

| Génération | Contient | Collecte |
|---|---|---|
| **Gen 0** | objets récents, court terme | très fréquente, rapide |
| **Gen 1** | survivants de Gen 0 | intermédiaire |
| **Gen 2** | objets de longue durée | rare, plus coûteuse |

L'idée : *la plupart des objets meurent jeunes*. Collecter surtout Gen 0 est donc bon marché et
suffit la plupart du temps.

> ⚠️ On ne **force** pas la collecte (`GC.Collect()`) en code normal : c'est presque toujours
> contre-productif. Laisse le GC faire son travail.

### Pourquoi le GC ne suffit pas

Le GC libère la **mémoire managée**, mais il ignore *quand*. Pour un fichier ou une connexion, on
veut une libération **immédiate et déterministe** dès qu'on a fini — pas « un jour, peut-être ».
C'est là qu'intervient `IDisposable`. (Les **finaliseurs**, `~MaClasse()`, existent en filet de
sécurité mais s'exécutent au gré du GC, donc de façon non déterministe : on ne s'y fie pas pour
des sorties observables.)

---

## 2. IDisposable et le bloc `using` {#using}

Une classe qui détient une ressource implémente `System.IDisposable` et libère dans `Dispose()`.
Le bloc `using` garantit l'appel à `Dispose()` à la sortie de portée.

```csharp
using (var f = new Fichier(nom))
{
    // ... utilisation ...
}   // Dispose() appelé ici, automatiquement

sealed class Fichier : System.IDisposable
{
    public void Dispose() => /* libérer */;
}
```

---

## 3. Ordre de libération : LIFO {#lifo}

Les **using declarations** (`using var x = ...;`) libèrent en ordre **inverse** de déclaration à la
fin de la portée — comme une pile. La dernière ressource ouverte est la première fermée.

```csharp
using var a = new Ressource("A");
using var b = new Ressource("B");   // fermée AVANT a
```

---

## 4. Dispose idempotent {#idempotent}

`Dispose()` peut être appelé plusieurs fois (manuellement *et* par un `using`). Il doit donc être
**idempotent** : seul le premier appel agit.

```csharp
private bool _ferme;
public void Dispose()
{
    if (_ferme) { return; }
    _ferme = true;
    // libérer une seule fois
}
```

---

## 5. Libération garantie malgré une exception {#exception}

Le vrai gain de `using` sur un `Dispose()` manuel : la ressource est libérée **même si une
exception survient**. Le `Dispose()` s'exécute pendant le déroulement de la pile, avant tout
`catch` situé plus haut.

```csharp
try
{
    using var t = new Transaction(nom);
    throw new System.Exception("boom");
}   // Dispose() ici, PENDANT la remontée de l'exception
catch { /* s'exécute après la libération */ }
```

---

## 6. Posséder, c'est libérer {#composite}

Quand un objet **détient** d'autres `IDisposable`, il est responsable de leur libération : il
implémente lui-même `IDisposable` et libère ses ressources (en ordre inverse) dans son `Dispose()`.

```csharp
sealed class Groupe : System.IDisposable
{
    private readonly List<Ressource> _ressources = new();
    public void Dispose()
    {
        for (var i = _ressources.Count - 1; i >= 0; i--)
            _ressources[i].Dispose();
    }
}
```

---

## 7. En pratique {#pratique}

- Tout ce qui implémente `IDisposable` → mets-le dans un `using`.
- N'appelle pas `GC.Collect()` ; ne te repose pas sur les finaliseurs pour la logique.
- « Qui possède une ressource la libère » : propage `IDisposable` jusqu'au propriétaire.

### Exercices du module

- **ex00-using** — libérer une ressource avec `using`.
- **ex01-ordre-lifo** — ordre de libération inverse.
- **ex02-dispose-idempotent** — `Dispose()` appelable plusieurs fois.
- **ex03-using-exception** — libération garantie malgré une exception.
- **ex04-composite** *(bonus)* — un objet qui libère les ressources qu'il détient.

## Références externes

- [Nettoyage des ressources non managées (doc Microsoft)](https://learn.microsoft.com/dotnet/standard/garbage-collection/unmanaged)
- [Fondamentaux du garbage collection (doc Microsoft)](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals)
