# Module 01 — Bases C#

Tu sais lancer la piscine et rendre en git. On attaque maintenant les **fondations** : types,
variables, lecture de l'entrée, opérateurs et conditions.

## 1. Types et variables

Une **variable** range une valeur d'un certain **type** :

```csharp
int age = 30;            // entier
double prix = 9.99;      // nombre à virgule
string nom = "Alice";    // chaîne de caractères
bool actif = true;       // booléen (vrai / faux)
```

Le mot-clé `var` laisse le compilateur **déduire** le type d'après la valeur :

```csharp
var age = 30;            // déduit int
var nom = "Alice";       // déduit string
```

## 2. Lire et convertir l'entrée

`Console.ReadLine()` renvoie toujours une **chaîne**. Pour obtenir un nombre, il faut la
**convertir** :

```csharp
string ligne = System.Console.ReadLine();
int n = int.Parse(ligne);                 // "42" -> 42
// ou, plus court :
var m = int.Parse(System.Console.ReadLine());
```

## 3. Opérateurs

```csharp
a + b      // addition          a - b   // soustraction
a * b      // multiplication    a / b   // division (entière entre deux int !)
a % b      // modulo : reste de la division (7 % 2 == 1)
```

Comparaisons (donnent un `bool`) : `==` (égal), `!=` (différent), `<`, `>`, `<=`, `>=`.

## 4. Conditions {#conditions}

```csharp
if (n % 2 == 0)
{
    System.Console.WriteLine("pair");
}
else
{
    System.Console.WriteLine("impair");
}
```

Forme courte, l'**opérateur ternaire** `condition ? valeurSiVrai : valeurSiFaux` :

```csharp
System.Console.WriteLine(n % 2 == 0 ? "pair" : "impair");
```

### Exercices du module

- **[ex00-somme](#somme)** : additionner deux entiers lus sur l'entrée.
- **[ex01-parite](#parite)** : dire si un entier est pair ou impair.
- **[ex02-maximum](#maximum)** : afficher le plus grand de deux entiers.

#### somme {#somme}
Lis deux entiers (un par ligne), affiche leur somme.

#### parite {#parite}
Lis un entier, affiche `pair` ou `impair` (indice : `% 2`).

#### maximum {#maximum}
Lis deux entiers, affiche le plus grand (indice : comparaison `>` ou ternaire).

## Bonne pratique git — commits atomiques

Fais **un commit par idée aboutie** : un exercice qui marche, un message clair
(`git commit -m "ex00-somme"`). Des petits commits cohérents valent mieux qu'un gros commit
fourre-tout — c'est plus facile à relire et à corriger.

## Pour aller plus loin

- Microsoft Learn — *Types de données C#* : <https://learn.microsoft.com/fr-fr/dotnet/csharp/>
- Conditions (`if`/`else`) : <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/statements/selection-statements>
