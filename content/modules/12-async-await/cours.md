# Module 12 — Async/await : programmation asynchrone

Jusqu'ici, ton code s'exécutait **ligne après ligne** : chaque instruction attendait que la
précédente soit terminée. C'est le mode **synchrone**. Mais que se passe-t-il quand une
opération est **lente** — lire un fichier, interroger le réseau, une base de données ? En
synchrone, le programme **se bloque** et ne fait rien d'autre pendant l'attente.

La programmation **asynchrone** permet de lancer ces opérations lentes **sans bloquer** : le
programme peut continuer ou attendre proprement, puis reprendre quand le résultat est prêt.

## 1. Synchrone vs asynchrone

```csharp
// Synchrone : on attend, bloqué, jusqu'à la fin.
int x = CalculLent();

// Asynchrone : on lance, et on récupère le résultat quand il est prêt.
int x = await CalculLentAsync();
```

Visuellement, le synchrone enchaîne les blocages ; l'asynchrone libère le programme pendant
l'attente. Pour un débutant, retiens surtout la **syntaxe** : `async` + `await` + `Task`.

## 2. `Task` et `Task<T>`

Une opération asynchrone ne renvoie pas tout de suite sa valeur : elle renvoie une **promesse**
de résultat, représentée par le type **`Task`** :

- **`Task`** : une opération asynchrone qui ne renvoie **aucune valeur** (comme un `void`).
- **`Task<T>`** : une opération asynchrone qui renvoie **une valeur de type `T`**. Par exemple
  `Task<int>` promet un `int`.

Ces types vivent dans `System.Threading.Tasks`, donc il faut **toujours** écrire :

```csharp
using System.Threading.Tasks;
```

## 3. `async` et `await` {#await}

Deux mots-clés vont ensemble :

- **`async`** marque une méthode comme asynchrone. Elle peut alors utiliser `await` à l'intérieur.
- **`await`** met en pause la méthode jusqu'à ce que la `Task` attendue soit terminée, puis
  **récupère sa valeur** (pour un `Task<T>`).

```csharp
using System.Threading.Tasks;

static async Task<int> DoublerAsync(int x)
{
    await Task.Delay(1);   // simule une attente (1 milliseconde)
    return x * 2;          // une fois l'attente finie, on renvoie la valeur
}

var resultat = await DoublerAsync(5);   // resultat vaut 10
System.Console.WriteLine(resultat);
```

Points clés :

- Une méthode `async Task<int>` se déclare avec `Task<int>` comme type de retour, mais à
  l'intérieur on fait `return` d'un `int` ordinaire.
- `await DoublerAsync(5)` **déballe** le `Task<int>` et te donne l'`int`.

### Top-level await

Dans un programme à **instructions de haut niveau** (top-level statements), tu peux utiliser
`await` directement, sans écrire de `Main` : le compilateur génère pour toi un programme
asynchrone. C'est ce qu'on fait dans les exercices.

## 4. `await Task.Delay`

`Task.Delay(ms)` renvoie une `Task` qui se termine après le nombre de **millisecondes** indiqué.
C'est l'outil idéal pour **simuler** une opération lente sans rien installer :

```csharp
await Task.Delay(1);   // attend ~1 ms, sans bloquer
```

Ne le confonds pas avec `Thread.Sleep`, qui lui **bloque** vraiment le thread.

## 5. Séquentiel : attendre une par une {#sequentiel}

Si tu fais `await` à **chaque tour** d'une boucle, chaque tâche est lancée **puis attendue**
avant de passer à la suivante. Les résultats sortent naturellement **dans l'ordre** :

```csharp
using System.Threading.Tasks;

for (var i = 0; i < n; i++)
{
    var carre = await CarreAsync(valeur);   // on attend celle-ci avant la suivante
    System.Console.WriteLine(carre);
}
```

C'est simple et l'ordre est garanti, mais on attend les tâches **l'une après l'autre**.

## 6. Concurrent : `Task.WhenAll` {#whenall}

Pour **lancer plusieurs tâches en même temps**, on les démarre **sans `await`** (on garde la
`Task`), puis on les attend **toutes ensemble** avec `Task.WhenAll` :

```csharp
using System.Threading.Tasks;

var tasks = new Task<int>[n];
for (var i = 0; i < n; i++)
{
    tasks[i] = CarreAsync(valeur);   // lancée, mais PAS attendue ici
}

var resultats = await Task.WhenAll(tasks);   // attend que toutes finissent
```

`Task.WhenAll(tasks)` (sur des `Task<int>`) renvoie un `int[]` **dans le même ordre** que le
tableau de tâches : `resultats[0]` correspond à `tasks[0]`, etc. L'ordre de **fin** des tâches
n'a donc pas d'importance pour l'affichage.

> Différence clé : en séquentiel, on attend chaque tâche avant de lancer la suivante ; avec
> `Task.WhenAll`, toutes partent d'abord, puis on attend l'ensemble.

## 7. Annulation : `CancellationToken` (pour info)

Les méthodes asynchrones acceptent souvent un **`CancellationToken`**, un jeton qui permet de
**demander l'annulation** d'une opération longue (par exemple un délai d'attente dépassé).
Tu n'en as pas besoin dans ce module, mais sache que `Task.Delay`, les appels réseau, etc.
en acceptent un. On l'étudiera plus tard.

### Exercices du module

- **[ex00-attente](#await)** : une méthode `async Task<int>` avec `await Task.Delay`, attendue.
- **[ex01-sequentiel](#sequentiel)** : `await` dans une boucle, tâches attendues une par une.
- **[ex02-whenall](#whenall)** : lancer N tâches puis `await Task.WhenAll`.

## Références externes

- Microsoft Learn — *Programmation asynchrone avec async et await* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/asynchronous-programming/>
- Microsoft Learn — *Modèle asynchrone basé sur des tâches (TAP)* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap>
- Vidéo — Nick Chapsas, *Async/await in C#* :
  <https://www.youtube.com/watch?v=il9gl8MH17s>
- Vidéo — Tim Corey, *C# Async/Await for beginners* :
  <https://www.youtube.com/watch?v=2moh18sh5p4>
