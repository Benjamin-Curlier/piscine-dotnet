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

Les comparaisons produisent toujours un `bool` :

| Opérateur | Signification | Exemple vrai |
|---|---|---|
| `==` | égal à | `age == 30` |
| `!=` | différent de | `nom != ""` |
| `<` / `>` | strictement inférieur / supérieur | `prix < 10` |
| `<=` / `>=` | inférieur / supérieur ou égal | `note >= 10` |

Pour combiner des conditions, utilise `&&` (**et**), `||` (**ou**) et `!` (**non**) :

```csharp
bool majeurEtActif = age >= 18 && actif;
bool remise = age < 18 || age >= 65;
bool inactif = !actif;
```

> `=` affecte une valeur ; `==` compare deux valeurs. Les confondre est une erreur classique.

### Conventions de nommage

Les noms ne changent pas le comportement du programme, mais une convention constante rend le code
immédiatement lisible :

- `PascalCase` pour les types, méthodes et propriétés : `CompteBancaire`, `CalculerTotal`, `Solde` ;
- `camelCase` pour les variables et paramètres : `prixTotal`, `nombreArticles` ;
- `_camelCase` pour les champs privés : `_solde`.

Choisis des noms qui décrivent le rôle (`nombreEssais`) plutôt que la forme (`n2`).

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

### Aperçu : `switch` et `enum`

Quand plusieurs branches comparent la même valeur, `switch` évite une longue cascade de `if` :

```csharp
var libelle = note switch
{
    >= 16 => "très bien",
    >= 10 => "admis",
    _ => "à revoir",
};
```

Quand une valeur ne peut prendre qu'un petit nombre de cas nommés, un `enum` remplace les nombres
ou chaînes « magiques » : `enum Etat { EnAttente, EnCours, Termine }`. Les modules 24 et 25
approfondissent ces deux outils ; tu peux déjà les reconnaître et les utiliser dans un cas simple.

### Exercices du module

- **[ex00-somme](#somme)** : additionner deux entiers lus sur l'entrée.
- **[ex01-parite](#parite)** : dire si un entier est pair ou impair.
- **[ex02-maximum](#maximum)** : afficher le plus grand de deux entiers.
- **[ex03-fizzbuzz](#fizzbuzz)** : *(bonus, difficile)* afficher 1 à N avec Fizz/Buzz/FizzBuzz.

#### somme {#somme}
Lis deux entiers (un par ligne), affiche leur somme.

#### parite {#parite}
Lis un entier, affiche `pair` ou `impair` (indice : `% 2`).

#### maximum {#maximum}
Lis deux entiers, affiche le plus grand (indice : comparaison `>` ou ternaire).

#### fizzbuzz {#fizzbuzz}
*(Bonus, difficile)* Lis N, affiche les entiers de 1 à N (un par ligne) : `FizzBuzz` si multiple de
3 **et** de 5, `Fizz` si multiple de 3, `Buzz` si multiple de 5, sinon le nombre. L'ordre des tests
est crucial : commence par la condition combinée (`% 15` ou `% 3 && % 5`).

## Bonne pratique git — commits atomiques

Fais **un commit par idée aboutie** : un exercice qui marche, un message clair
(`git commit -m "ex00-somme"`). Des petits commits cohérents valent mieux qu'un gros commit
fourre-tout — c'est plus facile à relire et à corriger.

## Pour aller plus loin

- Microsoft Learn — *Types de données C#* : <https://learn.microsoft.com/fr-fr/dotnet/csharp/>
- Conditions (`if`/`else`) : <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/statements/selection-statements>
