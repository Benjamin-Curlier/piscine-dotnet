# Module 24 — Switch & pattern matching

Tester une valeur et choisir un comportement selon sa forme est partout en programmation.
C# 14 offre pour cela le **pattern matching** : une famille de **motifs** qui décrivent
*à quoi ressemble* une valeur, combinés à une **switch expression** concise qui renvoie un
résultat. Fini les longues cascades de `if/else`.

## 1. `switch` instruction vs switch expression

Le `switch` **instruction** (classique) exécute des blocs et ne renvoie rien :

```csharp
switch (n)
{
    case 1:
        System.Console.WriteLine("un");
        break;
    default:
        System.Console.WriteLine("autre");
        break;
}
```

La **switch expression** (moderne) **renvoie une valeur**. Elle s'écrit `valeur switch { ... }`,
chaque branche utilisant une **flèche `=>`**, et le motif joker **`_`** couvre « tout le reste » :

```csharp
var mot = n switch
{
    1 => "un",
    2 => "deux",
    _ => "autre",   // _ : cas par défaut
};
```

Plus courte, sans `break`, et elle force à produire une valeur dans chaque branche.

## 2. Les motifs (patterns)

Un **motif** décrit la forme attendue d'une valeur. La branche choisie est **la première qui
correspond** : l'ordre est donc important (du plus précis au plus général).

### Motif constant {#jour}

Le plus simple : on compare à une constante (`1`, `"chien"`, `true`).

```csharp
var nom = n switch
{
    1 => "lundi",
    7 => "dimanche",
    _ => "inconnu",
};
```

> C'est l'exercice **[ex00-jour](#jour)** : un entier `1`-`7` → le nom du jour, sinon `inconnu`.

### Motif relationnel : `>=`, `<`, ... {#mention}

Un **motif relationnel** compare avec `<`, `<=`, `>`, `>=`. Idéal pour des **plages** de valeurs.
Comme la première branche qui correspond gagne, on classe **du seuil le plus haut au plus bas** :

```csharp
var mention = note switch
{
    >= 90 => "excellent",
    >= 50 => "passable",   // donc : entre 50 et 89
    _ => "insuffisant",    // donc : moins de 50
};
```

> C'est l'exercice **[ex01-mention](#mention)**.

### Motif de type : `is`

Le mot-clé **`is`** teste le type d'une valeur, et peut **capturer** le résultat dans une variable :

```csharp
object o = "salut";
if (o is string s)
{
    System.Console.WriteLine(s.Length);   // s est un string utilisable ici
}
```

On retrouve le motif de type dans un `switch` : `obj switch { string s => ..., int i => ..., _ => ... }`.

### Motif de propriété : `{ X: 0 }` {#point}

Un **motif de propriété** teste la valeur des propriétés d'un objet. Très lisible sur un
**record** (vu au module sur les records) :

```csharp
var classe = p switch
{
    { X: 0, Y: 0 } => "origine",   // X vaut 0 ET Y vaut 0
    { X: 0 } => "axe Y",           // X vaut 0 (Y quelconque)
    { Y: 0 } => "axe X",
    _ => "quelconque",
};

record Point(int X, int Y);
```

Le cas le **plus précis** (`{ X: 0, Y: 0 }`) doit venir **avant** `{ X: 0 }`, sinon ce dernier
attraperait aussi l'origine.

> C'est l'exercice **[ex02-point](#point)**.

### Motif de liste : `[..]` et la tranche `..` {#liste}

Un **motif de liste** décrit la forme d'un tableau ou d'une collection. On place les éléments
entre crochets ; la **tranche `..`** représente « zéro, un ou plusieurs éléments » :

```csharp
var r = t switch
{
    [] => "vide",                              // aucun élément
    [var x] => "un seul",                      // exactement un élément
    [var f, .., var l] => $"premier={f} dernier={l}",   // un premier, un dernier, et le milieu
    _ => "?",
};
```

Comme `..` accepte un milieu vide, `[var f, .., var l]` correspond aussi à un tableau de
**deux** éléments. On peut capturer la tranche elle-même : `[var f, .. var milieu]`.

> C'est l'exercice **bonus [ex03-liste](#liste)**.

## 3. La clause `when`

Quand un motif ne suffit pas, **`when`** ajoute une condition booléenne supplémentaire :

```csharp
var signe = n switch
{
    0 => "nul",
    > 0 when n % 2 == 0 => "positif pair",
    > 0 => "positif impair",
    _ => "negatif",
};
```

## 4. Exhaustivité

Une switch expression doit pouvoir traiter **toutes** les valeurs possibles. Si le compilateur
estime qu'un cas n'est pas couvert, il **avertit** ; à l'exécution, une valeur non couverte lève
une exception. Le motif **`_`** garantit l'exhaustivité en couvrant « tout le reste » : termine
donc tes switch expressions par `_ => ...` tant que tu n'es pas certain d'avoir tout listé.

### Exercices du module

- **[ex00-jour](#jour)** *(facile)* : motif constant + switch expression + `_`.
- **[ex01-mention](#mention)** *(moyen)* : motifs relationnels (`>=`) et ordre des branches.
- **[ex02-point](#point)** *(moyen)* : motifs de propriété sur un `record`.
- **[ex03-liste](#liste)** *(bonus, difficile)* : motifs de liste avec la tranche `..`.

## Références externes

- Microsoft Learn — *Pattern matching* (motifs et `is`) :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/functional/pattern-matching>
- Microsoft Learn — *Patterns (référence du langage)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/operators/patterns>
- Microsoft Learn — *L'expression `switch`* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/operators/switch-expression>
- Vidéo — *Pattern matching in C#* (.NET, YouTube) :
  <https://www.youtube.com/watch?v=v_xCLkPmU4o>
