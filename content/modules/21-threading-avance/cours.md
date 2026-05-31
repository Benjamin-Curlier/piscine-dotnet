# Module 21 — Threading avancé

Au module 12, `async`/`await` permettait à un programme de **ne pas bloquer** pendant une attente
(réseau, disque). Ici, on va plus loin : faire travailler **plusieurs cœurs en même temps**
(*parallélisme*) et partager des données entre eux **sans se marcher dessus**.

## 1. Rappel : Task & async (module 12)

Une `Task` représente un travail qui se termine plus tard. `await` attend ce résultat sans bloquer
le thread. Dans une piscine, on peut même `await` directement au niveau des instructions
principales (top-level), comme dans un `Main` asynchrone.

```csharp
using System.Threading.Tasks;

await Task.Delay(10);   // attend 10 ms sans bloquer
System.Console.WriteLine("fini");
```

Le parallélisme ajoute une difficulté nouvelle : plusieurs threads qui touchent **la même
variable** au même instant.

## 2. Le problème des accès concurrents (race condition)

`compteur++` ressemble à une seule opération, mais c'est en réalité **trois** étapes : lire la
valeur, ajouter 1, réécrire. Si deux threads font ces étapes **entrelacées**, ils lisent la même
valeur de départ et l'un écrase l'autre : un incrément est **perdu**.

```text
Thread A lit 5      Thread B lit 5
Thread A écrit 6    Thread B écrit 6   // on voulait 7 !
```

C'est une **race condition** : le résultat dépend du *timing*, donc il est imprévisible. La
solution : **synchroniser** les accès.

## 3. Parallel.For / Parallel.ForEach {#parallele}

`Parallel.For` répartit automatiquement une boucle sur les cœurs disponibles. Sa signature
ressemble à un `for` : début **inclus**, fin **exclue**.

```csharp
using System.Threading.Tasks;

System.Threading.Tasks.Parallel.For(0, 5, i =>
{
    System.Console.WriteLine(i);   // 0..4, mais dans un ordre QUELCONQUE
});
```

`Parallel.ForEach` fait la même chose sur une collection. **Attention** : l'ordre d'exécution
n'est **pas garanti**. Pour obtenir un résultat déterministe, il faut soit une opération
indépendante de l'ordre (comme une somme), soit une synchronisation, soit une structure FIFO
(voir la section Channel).

## 4. Synchronisation : lock {#lock}

Le mot-clé **`lock`** garantit qu'**un seul thread à la fois** entre dans un bloc. On verrouille
sur un objet dédié, partagé par tous les threads.

```csharp
using System.Threading.Tasks;

var verrou = new object();
int compteur = 0;

System.Threading.Tasks.Parallel.For(0, 1000, _ =>
{
    lock (verrou)
    {
        compteur++;   // protégé : aucun incrément perdu
    }
});

System.Console.WriteLine(compteur);   // 1000, toujours
```

Garde la section verrouillée **la plus courte possible** : c'est un goulot d'étranglement (les
autres threads attendent leur tour).

## 5. Interlocked : opérations atomiques {#parallele-interlocked}

Pour de simples opérations sur un nombre, `lock` est parfois excessif. La classe **`Interlocked`**
offre des opérations **atomiques** (indivisibles), plus rapides :

```csharp
using System.Threading;

long total = 0;
System.Threading.Interlocked.Add(ref total, 5);   // += 5, atomique
```

`Interlocked.Add`, `Interlocked.Increment`, `Interlocked.Decrement`… s'exécutent d'un seul bloc :
aucun thread ne peut s'intercaler. Idéal pour un accumulateur partagé dans un `Parallel.For`.
Comme une somme ne dépend pas de l'ordre, le résultat reste **déterministe**.

## 6. Producteur / consommateur : Channel<T> {#channel}

Un **`Channel<T>`** est une file de messages **thread-safe** : un côté **écrit** (producteur), un
côté **lit** (consommateur). C'est le moyen propre de faire communiquer des tâches concurrentes,
sans `lock` manuel.

```csharp
using System.Threading.Channels;
using System.Threading.Tasks;

var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();

// Producteur : écrit puis signale la fin
await channel.Writer.WriteAsync(2);
await channel.Writer.WriteAsync(3);
channel.Writer.Complete();

// Consommateur : lit dans l'ordre d'écriture (FIFO), s'arrête à la complétion
await foreach (var x in channel.Reader.ReadAllAsync())
{
    System.Console.WriteLine(x * 2);   // 4, puis 6
}
```

Points clés :

- `Channel.CreateUnbounded<int>()` crée un canal de capacité illimitée.
- `Writer.WriteAsync(x)` ajoute une valeur ; `Writer.Complete()` indique « plus rien à venir ».
- `Reader.ReadAllAsync()` se parcourt avec `await foreach` et se termine **automatiquement** quand
  le canal est complété.
- Le canal est **FIFO** : l'ordre de lecture = l'ordre d'écriture, donc la sortie est
  **déterministe** même si producteur et consommateur tournent en parallèle.

## 7. Conseil : préférer les abstractions de haut niveau

On peut créer des threads bruts (`new Thread(...)`), mais c'est verbeux et source de bugs.
Préfère toujours les outils de haut niveau : `Task`/`async`, `Parallel.For`, `Interlocked`,
`Channel<T>`. Ils sont plus sûrs, plus lisibles, et gérés efficacement par le runtime.

### Exercices du module

- **[ex00-parallele-somme](#parallele)** : `Parallel.For` + `Interlocked.Add` pour une somme parallèle.
- **[ex01-lock](#lock)** : protéger un compteur partagé avec `lock`.
- **[ex02-channel](#channel)** : pattern producteur/consommateur avec `Channel<int>`.

## Références externes

- Microsoft Learn — *Programmation parallèle (Parallel.For / ForEach)* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/parallel-programming/data-parallelism-task-parallel-library>
- Microsoft Learn — *System.Threading.Channels* :
  <https://learn.microsoft.com/fr-fr/dotnet/core/extensions/channels>
- Microsoft Learn — *Interlocked* :
  <https://learn.microsoft.com/fr-fr/dotnet/api/system.threading.interlocked>
- Vidéo — *Producer/consumer avec Channels en C#* (Nick Chapsas) :
  <https://www.youtube.com/watch?v=gT06qvQLtJ0>
