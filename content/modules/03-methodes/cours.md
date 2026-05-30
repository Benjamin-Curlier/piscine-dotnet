# Module 03 — Méthodes

Une **méthode** (ou fonction) est un bloc de code **nommé** que l'on peut **appeler** à volonté.
Elle évite de répéter du code et donne un nom clair à une idée.

## 1. Déclarer et appeler

```csharp
static int Carre(int n)
{
    return n * n;
}

var resultat = Carre(5);   // appel : resultat vaut 25
```

- `static` : la méthode n'a pas besoin d'objet pour être appelée (suffisant ici).
- `int` (avant le nom) : le **type de retour** — ce que la méthode renvoie.
- `Carre` : le **nom**.
- `(int n)` : les **paramètres** — les valeurs reçues en entrée.
- `return` : renvoie une valeur **et** termine la méthode.

## 2. Paramètres et retour

Une méthode peut recevoir **plusieurs paramètres** :

```csharp
static int Max(int x, int y)
{
    return x > y ? x : y;
}

var plusGrand = Max(3, 9);   // 9
```

Si une méthode ne renvoie rien, son type de retour est `void` :

```csharp
static void Saluer(string nom)
{
    System.Console.WriteLine($"Bonjour, {nom}!");
}
```

### Forme courte (expression-bodied)

Quand le corps tient en une expression, `=>` remplace les accolades et le `return` :

```csharp
static int Carre(int n) => n * n;
```

## 3. Portée des variables {#portee}

Une variable déclarée **dans** une méthode n'existe **que** dans cette méthode. Les paramètres
sont locaux eux aussi. Pour communiquer, on passe des **arguments** et on lit le **retour** — pas
de variable « partagée » par magie.

## 4. Récursion {#recursion}

Une méthode peut **s'appeler elle-même** : c'est la **récursion**. Il faut toujours un **cas de
base** qui arrête la descente, sinon l'appel ne s'arrête jamais.

```csharp
static long Factorielle(int n)
{
    if (n <= 1) return 1;          // cas de base
    return n * Factorielle(n - 1); // cas récursif
}
// Factorielle(5) = 5 * 4 * 3 * 2 * 1 = 120
```

> Dans un fichier en *top-level statements*, place tes méthodes **après** les instructions
> principales — elles restent appelables au-dessus (ce sont des *fonctions locales*).

### Exercices du module

- **[ex00-carre](#carre)** : écrire une méthode qui renvoie le carré d'un entier.
- **[ex01-max3](#max3)** : écrire une méthode qui renvoie le plus grand de trois entiers.
- **[ex02-factorielle](#factorielle)** : calculer une factorielle par récursion.

#### carre {#carre}
Lis un entier, affiche son carré — via une méthode `Carre(int)`.

#### max3 {#max3}
Lis trois entiers, affiche le plus grand — via une méthode `Max(int, int)` réutilisée.

#### factorielle {#factorielle}
Lis un entier `n`, affiche `n!` — par **récursion** (cas de base `n <= 1`).

## Pour aller plus loin

- Microsoft Learn — *Méthodes* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/classes-and-structs/methods>
- Fonctions locales :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/classes-and-structs/local-functions>
