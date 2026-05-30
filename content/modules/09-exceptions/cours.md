# Module 09 — Exceptions : gestion des erreurs

Jusqu'ici, nos programmes supposaient que tout se passe bien. Mais une division par zéro, une saisie
qui n'est pas un nombre, un âge absurde… ce sont des **erreurs à l'exécution**. En C#, ces erreurs
prennent la forme d'**exceptions**. Ce module montre comment les **attraper**, les **lever**, et
réagir proprement au lieu de laisser le programme planter.

## 1. Qu'est-ce qu'une exception ?

Une **exception** est un objet décrivant une erreur survenue pendant l'exécution. Quand une opération
échoue, le programme **lève** (`throw`) une exception : l'exécution normale s'arrête net et remonte à
la recherche de quelqu'un capable de **gérer** l'erreur. Si personne ne la gère, le programme
**plante** et affiche un message d'erreur.

```csharp
var resultat = 10 / 0;   // lève une DivideByZeroException : le programme s'arrête
```

L'idée n'est pas d'éviter toute exception, mais de **les attraper** là où on sait quoi faire.

## 2. Le bloc `try` / `catch`

Pour gérer une exception, on entoure le code risqué d'un bloc **`try`**, suivi d'un bloc **`catch`**
qui s'exécute **seulement si** une exception survient :

```csharp
try
{
    var resultat = 10 / diviseur;   // code risqué
    System.Console.WriteLine(resultat);
}
catch
{
    System.Console.WriteLine("Quelque chose a mal tourné");
}
```

Si le `try` réussit, le `catch` est ignoré. S'il échoue, on saute directement dans le `catch` :
le programme **continue** au lieu de planter.

## 3. Attraper un type précis {#division}

Plutôt qu'un `catch` attrape-tout, on précise **le type** d'exception attendu. C'est plus clair et
plus sûr : on ne masque pas des erreurs qu'on n'avait pas prévues.

```csharp
using System;

try
{
    System.Console.WriteLine(a / b);
}
catch (DivideByZeroException)
{
    System.Console.WriteLine("Erreur: division par zero");
}
```

Ici, seule une **division par zéro** déclenche le `catch`. Une autre erreur remonterait normalement.

> Les types d'exceptions vivent dans l'espace de noms `System`. Comme le grader n'a pas d'`using`
> implicite, pense à ajouter `using System;` en haut dès que tu nommes un type d'exception.

## 4. Une erreur de conversion : `FormatException` {#parse}

`int.Parse("abc")` ne peut pas transformer `"abc"` en nombre : il lève une **`FormatException`**. On
peut donc valider une saisie ligne par ligne sans planter :

```csharp
using System;

try
{
    var nombre = int.Parse(ligne);
    System.Console.WriteLine(nombre);
}
catch (FormatException)
{
    System.Console.WriteLine("invalide");
}
```

Chaque tour de boucle est protégé : une ligne fautive affiche `invalide`, les suivantes continuent
d'être traitées normalement.

## 5. Lever une exception avec `throw` {#age}

On peut aussi **lever soi-même** une exception quand une règle métier est violée, avec le mot-clé
**`throw`**. Pour signaler qu'un argument est hors des bornes autorisées, on utilise
**`ArgumentOutOfRangeException`** :

```csharp
using System;

static void Valider(int age)
{
    if (age < 0 || age > 150)
    {
        throw new ArgumentOutOfRangeException(nameof(age));
    }
}
```

`nameof(age)` fournit le nom du paramètre fautif (`"age"`) — pratique pour le message d'erreur.
L'appelant entoure alors l'appel d'un `try/catch` :

```csharp
try
{
    Valider(age);
    System.Console.WriteLine($"Age: {age}");
}
catch (ArgumentOutOfRangeException)
{
    System.Console.WriteLine("Age invalide");
}
```

## 6. `finally` (pour info)

Un bloc **`finally`** s'exécute **toujours**, qu'il y ait eu une exception ou non. On y range le
nettoyage (fermer un fichier, libérer une ressource) :

```csharp
try
{
    // travail
}
catch (FormatException)
{
    // gestion de l'erreur
}
finally
{
    // exécuté dans tous les cas
}
```

## 7. Quelques exceptions courantes

- **`DivideByZeroException`** : division entière par zéro.
- **`FormatException`** : conversion impossible (`int.Parse` d'un texte non numérique).
- **`ArgumentOutOfRangeException`** : un argument est en dehors des valeurs autorisées.
- **`NullReferenceException`** : on utilise une variable qui vaut `null` (« la fameuse erreur à un
  milliard de dollars »).

## 8. Bonne pratique : ne pas avaler les exceptions

Le pire `catch` est celui qui **ne fait rien** :

```csharp
try { /* ... */ }
catch { }   // À ÉVITER : l'erreur disparaît en silence
```

Une erreur silencieuse rend les bugs très difficiles à trouver. Attrape **le type précis** que tu
sais gérer, et **fais quelque chose d'utile** : afficher un message clair, retenter, ou laisser
remonter ce que tu ne sais pas traiter.

### Exercices du module

- **[ex00-division-securisee](#division)** : attraper une `DivideByZeroException`.
- **[ex01-parse-robuste](#parse)** : attraper une `FormatException` ligne par ligne.
- **[ex02-age-valide](#age)** : lever une `ArgumentOutOfRangeException` avec `throw`, puis l'attraper.

## Références externes

- Microsoft Learn — *Exceptions et gestion des exceptions* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/exceptions/>
- Microsoft Learn — *try-catch* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/statements/exception-handling-statements#the-try-catch-statement>
- Vidéo — Nick Chapsas, *Why You Shouldn't Throw Generic Exceptions* :
  <https://www.youtube.com/watch?v=pSc2gctRf60>
