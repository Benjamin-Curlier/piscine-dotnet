# Module 26 — Static, const, readonly & immutabilité

En C#, plusieurs mécanismes permettent d'**empêcher la modification** d'une valeur après son
initialisation. Ce module présente `const`, les membres `static`, les champs `readonly`,
les `record` et les `readonly struct`.

## 1. `const` — constante de compilation {#const}

Une **constante de compilation** est une valeur fixée au moment de la *compilation* : le
compilateur remplace chaque utilisation de la constante par sa valeur littérale.

```csharp
const int Facteur = 10;
const string Salutation = "Bonjour";
```

Règles importantes :

- Déclarée avec `const`, elle est **implicitement statique** (pas besoin d'écrire `static`).
- Elle ne peut contenir que des types primitifs (`int`, `double`, `string`, `bool`…).
- Sa valeur doit être connue **avant l'exécution** (pas d'appel de méthode, pas de `new`).
- Elle ne peut jamais être modifiée après sa déclaration.

```csharp
const int Max = 100;
// Max = 200;  // erreur de compilation
```

### `const` vs `static readonly`

| | `const` | `static readonly` |
|---|---|---|
| Moment d'affectation | compilation | exécution (constructeur statique ou initialisation) |
| Types autorisés | primitifs + `string` | n'importe quel type |
| Modifiable après | non | non |
| Implicitement statique | oui | non (faut écrire `static`) |

Utilisez `static readonly` quand la valeur est calculée ou provient d'un type complexe :

```csharp
static readonly System.DateTime Debut = System.DateTime.Now;
```

---

## 2. Membres `static` — partagés par le type {#static}

Un membre `static` appartient à la **classe entière**, pas à une instance particulière.
Toutes les instances partagent la même valeur.

```csharp
class Compteur
{
    public static int Total;      // partagé par toutes les instances

    public Compteur()
    {
        Total++;                  // chaque construction incrémente le total
    }
}

var a = new Compteur();
var b = new Compteur();
System.Console.WriteLine(Compteur.Total);   // 2
```

On accède à un membre statique via le **nom du type** (`Compteur.Total`), jamais via une
instance.

### Méthodes statiques

Une méthode `static` ne reçoit pas de `this` : elle ne peut accéder qu'à d'autres membres
statiques.

```csharp
class MathUtils
{
    public static int Carre(int n) => n * n;
}

System.Console.WriteLine(MathUtils.Carre(5));   // 25
```

---

## 3. `readonly` — champ assigné une seule fois {#readonly}

Un champ `readonly` peut être affecté uniquement lors de sa **déclaration** ou dans un
**constructeur**. Après cela, il ne peut plus changer.

```csharp
class Cercle
{
    private readonly int _rayon;

    public Cercle(int rayon)
    {
        _rayon = rayon;   // OK : dans le constructeur
    }

    public int Rayon => _rayon;
}
```

Différence avec `const` : `readonly` peut contenir n'importe quel type, et sa valeur peut
être calculée à l'exécution (par exemple lue depuis la configuration).

---

## 4. Immutabilité avec `record` et l'expression `with` {#record}

Un `record` est un type dont l'**égalité est fondée sur les valeurs** (et non sur les
références). Ses propriétés sont, par défaut, immuables (`init`-only).

```csharp
record Point(int X, int Y);

var p1 = new Point(1, 2);
var p2 = new Point(1, 2);
System.Console.WriteLine(p1 == p2);   // True (égalité par valeur)
```

### Expression `with`

Pour "modifier" un record immuable, on crée une **copie** avec les champs voulus changés :

```csharp
var p = new Point(3, 7);
var p2 = p with { X = 10 };   // p reste (3, 7) ; p2 est (10, 7)
System.Console.WriteLine($"({p2.X}, {p2.Y})");   // (10, 7)
```

L'original `p` est inchangé : l'immutabilité est préservée.

---

## 5. `readonly struct` — valeur immuable sur la pile {#readonly-struct}

Un `readonly struct` est une structure (`struct`) dont tous les champs sont en lecture seule.
La valeur vit sur la **pile** (stack), ce qui évite les allocations sur le tas.

```csharp
readonly struct Vecteur
{
    public int X { get; }
    public int Y { get; }

    public Vecteur(int x, int y) { X = x; Y = y; }

    public int Norme1 => System.Math.Abs(X) + System.Math.Abs(Y);
}
```

Ou avec la syntaxe de **constructeur primaire** (C# 12) :

```csharp
readonly struct Vecteur(int x, int y)
{
    public int Norme1 => System.Math.Abs(x) + System.Math.Abs(y);
}
```

Le compilateur garantit qu'aucune méthode du struct ne modifie ses champs.

### Pourquoi préférer l'immutabilité ?

- **Moins de bugs** : une valeur qui ne change pas ne peut pas être corrompue à distance.
- **Thread-safe** : plusieurs threads peuvent lire la même valeur sans verrou.
- **Raisonnement facilité** : le code est plus prévisible.

---

### Exercices du module

- **[ex00-const](#const)** : utiliser `const` pour multiplier une entrée par un facteur fixe.
- **[ex01-compteur-static](#static)** : compter les instances créées avec un champ `static`.
- **[ex02-record-with](#record)** : copier un record avec `with` et afficher le résultat.
- **[ex03-readonly-struct](#readonly-struct)** *(bonus)* : calculer la norme 1 d'un `readonly struct`.

---

## Références externes

- Microsoft Learn — *const* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/keywords/const>
- Microsoft Learn — *static* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/keywords/static>
- Microsoft Learn — *readonly* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/keywords/readonly>
- Microsoft Learn — *Records* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/builtin-types/record>
- Microsoft Learn — *readonly struct* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/builtin-types/struct#readonly-struct>
- Vidéo — *C# immutability and records* (Nick Chapsas) :
  <https://www.youtube.com/watch?v=9IxyoAYMTuQ>
