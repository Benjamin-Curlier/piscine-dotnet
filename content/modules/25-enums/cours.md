# Module 25 — Enums

Une **énumération** (`enum`) donne un nom lisible à un ensemble fini de valeurs : les jours de la
semaine, des états, des permissions… Au lieu de manipuler des entiers nus (`0`, `1`, `2`), on
écrit `Couleur.Rouge`. Le code devient plus clair, et le compilateur vérifie qu'on reste dans le
jeu de valeurs prévu.

## 1. Déclarer un enum

Un `enum` liste ses membres. Chacun reçoit une **valeur entière sous-jacente**, attribuée
automatiquement à partir de `0` :

```csharp
enum Couleur
{
    Rouge,   // 0
    Vert,    // 1
    Bleu,    // 2
}

var c = Couleur.Vert;
```

Le type sous-jacent est `int` par défaut. On peut aussi fixer les valeurs à la main (voir la
section `[Flags]`).

## 2. Cast enum ↔ int

Un membre d'enum **n'est pas** un `int` : la conversion est explicite dans les deux sens.

```csharp
Couleur c = Couleur.Bleu;
int n = (int)c;             // 2  : enum -> int

Couleur d = (Couleur)1;     // Vert : int -> enum
```

Caster un `int` qui ne correspond à aucun membre ne lève **pas** d'erreur : on obtient une valeur
d'enum « inconnue ». C'est pourquoi on valide souvent l'entrée avant.

## 3. `Enum.Parse` et `ToString`

Pour passer d'un **texte** à un membre d'enum, on utilise `Enum.Parse<T>` :

```csharp
using System;

var c = Enum.Parse<Couleur>("Vert");   // Couleur.Vert
```

`Enum.Parse` vit dans l'espace `System`, d'où le `using System;`. Dans l'autre sens, `ToString()`
(appelé implicitement par `WriteLine`) redonne le **nom** du membre :

```csharp
System.Console.WriteLine(Couleur.Vert);            // Vert
System.Console.WriteLine(Couleur.Vert.ToString()); // Vert
```

## 4. `switch` sur un enum

Un enum se prête bien à une **switch expression** : on associe une valeur de sortie à chaque
membre. Le motif `_` couvre les cas restants.

```csharp
var hex = couleur switch
{
    Couleur.Rouge => "#FF0000",
    Couleur.Vert  => "#00FF00",
    Couleur.Bleu  => "#0000FF",
    _             => "#000000",
};
```

> Rappel (module 24) : une switch expression renvoie une valeur ; chaque bras a la forme
> `motif => résultat`.

## 5. `[Flags]` : combiner des valeurs

Certains enums représentent un **ensemble d'options** qu'on veut cumuler (des permissions, par
exemple). On marque alors l'enum `[Flags]` et on choisit des valeurs **puissances de 2**, pour que
chaque membre occupe un bit distinct :

```csharp
[Flags]
enum Permissions
{
    Lecture = 1,    // bit 0
    Ecriture = 2,   // bit 1
    Execution = 4,  // bit 2
}
```

On **combine** avec l'opérateur OU binaire `|`. La valeur entière obtenue est la somme des bits :

```csharp
var p = Permissions.Lecture | Permissions.Ecriture;
System.Console.WriteLine((int)p);   // 3  (1 + 2)
```

Pour accumuler dans une boucle, on utilise `|=` :

```csharp
Permissions resultat = default;          // aucune option (0)
resultat |= Permissions.Lecture;         // ajoute Lecture
resultat |= Permissions.Execution;       // ajoute Execution
System.Console.WriteLine((int)resultat); // 5  (1 + 4)
```

## 6. Tester un drapeau : `HasFlag` et `&`

Pour savoir si une option est **présente** dans une valeur combinée, on appelle `HasFlag` :

```csharp
var combinee = Permissions.Lecture | Permissions.Ecriture; // 3
bool aLecture = combinee.HasFlag(Permissions.Lecture);     // true
bool aExec = combinee.HasFlag(Permissions.Execution);      // false
```

`HasFlag(p)` équivaut au test au niveau du bit `(combinee & p) == p` : l'opérateur ET binaire `&`
ne garde que les bits communs.

```csharp
bool aLecture = (combinee & Permissions.Lecture) == Permissions.Lecture; // true
```

### Exercices du module

- **[ex00-valeur](#valeur)** : lire une `Couleur` et afficher sa valeur entière (cast `(int)`).
- **[ex01-couleur-hex](#hex)** : convertir une `Couleur` en code hexadécimal via une switch expression.
- **[ex02-flags](#flags)** : combiner des `Permissions` `[Flags]` avec `|`.
- **[ex03-hasflag](#hasflag)** *(bonus)* : tester la présence d'un drapeau avec `HasFlag`.

#### valeur {#valeur}
Lis un nom de couleur (`Rouge`/`Vert`/`Bleu`), parse-le en `Couleur`, affiche `(int)` de la valeur.

#### hex {#hex}
Lis un nom de couleur, parse-le en `Couleur`, renvoie son code hexadécimal via un `switch`.

#### flags {#flags}
Lis `N` puis `N` noms de `Permissions` ; accumule avec `|=` ; affiche la valeur entière combinée.

#### hasflag {#hasflag}
Lis un entier combiné et un nom de `Permission` ; affiche `oui`/`non` selon `HasFlag`.

## Références externes

- Microsoft Learn — *Types énumération (enum)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/builtin-types/enum>
- Microsoft Learn — *Types d'énumération comme indicateurs de bits ([Flags])* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/builtin-types/enum#enumeration-types-as-bit-flags>
- Vidéo — Nick Chapsas, *The right way to use enums in C#* :
  <https://www.youtube.com/watch?v=mPRWQfHpvLc>
