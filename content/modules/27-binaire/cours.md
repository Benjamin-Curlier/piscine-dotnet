# Module 27 — Opérations binaires

Un entier n'est pas un nombre « abstrait » pour la machine : c'est une suite de **bits** (des `0`
et des `1`). Manipuler directement ces bits — les comparer, les déplacer, les tester — est rapide
et sert partout : drapeaux d'options, masques de permissions, optimisations. Ce module montre les
**opérateurs bit à bit** de C#.

## 1. La représentation binaire d'un entier

En base 2, chaque chiffre (chaque **bit**) pèse une puissance de 2. Par exemple `13` :

```
13 = 8 + 4 + 0 + 1 = 1101 (en binaire)
       2^3 2^2 2^1 2^0
```

Le bit le plus à droite est le **bit de poids faible** (`2^0`), celui le plus à gauche le **bit de
poids fort**. Un `int` en C# tient sur 32 bits.

## 2. Opérateurs bit à bit : `&` `|` `^` `~` {#et-ou-xor}

Ces opérateurs comparent deux nombres **bit par bit** :

| Opérateur | Nom        | Résultat du bit                          |
|-----------|------------|------------------------------------------|
| `&`       | ET         | `1` si les **deux** bits sont à 1        |
| `|`       | OU         | `1` si **au moins un** bit est à 1       |
| `^`       | OU exclusif (XOR) | `1` si les bits sont **différents** |
| `~`       | NON (complément) | inverse chaque bit (opérateur unaire) |

```csharp
//  6 = 110
//  3 = 011
System.Console.WriteLine(6 & 3);   // 010 = 2
System.Console.WriteLine(6 | 3);   // 111 = 7
System.Console.WriteLine(6 ^ 3);   // 101 = 5
```

> Ne confonds pas `&` (bit à bit) avec `&&` (ET **logique** sur des `bool`), ni `|` avec `||`.

## 3. Décalages : `<<` et `>>` {#decalage}

Le décalage déplace tous les bits d'un nombre de plusieurs positions :

- `n << k` décale vers la **gauche** de `k` bits (des `0` arrivent à droite) ;
- `n >> k` décale vers la **droite** de `k` bits (les bits de droite « tombent »).

```csharp
System.Console.WriteLine(1 << 4);   // 1 devient 10000 = 16
System.Console.WriteLine(8 >> 1);   // 1000 devient 100 = 4
```

Effet à retenir : décaler à gauche de 1 bit **multiplie par 2**, décaler à droite de 1 bit
**divise par 2** (division entière). Décaler de `k` bits revient donc à multiplier/diviser par
`2^k`.

## 4. Masques : tester ou positionner un bit {#compte-bits}

Un **masque** est un nombre choisi pour cibler certains bits. Le masque le plus simple est `1`,
qui isole le bit de poids faible :

```csharp
var n = 6;            // 110
var dernierBit = n & 1;   // 0  (le bit de droite de 6 est à 0)
```

En combinant masque et décalage, on parcourt les bits un à un. Pour **compter** les bits à 1, on
regarde le dernier bit, puis on décale d'un cran, en boucle :

```csharp
var n = 13;           // 1101
var compte = 0;
while (n > 0)
{
    compte += n & 1;  // 1 si le bit courant est à 1
    n >>= 1;          // on passe au bit suivant
}
System.Console.WriteLine(compte);   // 3
```

D'autres masques classiques : `n | (1 << k)` **positionne** le bit `k` à 1, et `n & (1 << k)`
**teste** si le bit `k` est à 1.

## 5. Conversions en base 2 {#base2}

La classe `Convert` (dans `System`) traduit entre un entier et sa chaîne dans une base donnée :

```csharp
using System;

System.Console.WriteLine(Convert.ToString(13, 2));   // "1101" (sans zéros de tête ; 0 -> "0")
System.Console.WriteLine(Convert.ToInt32("1101", 2)); // 13
```

`Convert.ToString(n, 2)` produit la représentation binaire **sans zéros de tête**, et
`Convert.ToInt32(s, 2)` fait l'inverse. Comme `Convert` vit dans `System`, ajoute `using System;`
en haut dès que tu l'utilises.

### Exercices du module

- **[ex00-et-ou-xor](#et-ou-xor)** : appliquer `&`, `|`, `^` sur deux entiers.
- **[ex01-decalage](#decalage)** : décalages `<<` et `>>` et leur effet ×2/÷2.
- **[ex02-compte-bits](#compte-bits)** : compter à la main les bits à 1 (masque + décalage).
- **[ex03-base2](#base2)** *(bonus)* : afficher la représentation binaire avec `Convert.ToString`.

#### et-ou-xor {#et-ou-xor}
Lis deux entiers `a` et `b`, affiche `a & b`, `a | b` puis `a ^ b`.

#### decalage {#decalage}
Lis `n` et `k`, affiche `n << k` puis `n >> k`.

#### compte-bits {#compte-bits}
Lis un entier `n` (≥ 0), affiche son nombre de bits à 1 sans utiliser `BitOperations`.

#### base2 {#base2}
Lis un entier `n` (≥ 0), affiche sa représentation binaire (`Convert.ToString(n, 2)`).

## Références externes

- Microsoft Learn — *Opérateurs au niveau du bit et de décalage* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/operators/bitwise-and-shift-operators>
- Microsoft Learn — *Convert.ToString (base)* :
  <https://learn.microsoft.com/fr-fr/dotnet/api/system.convert.tostring>
- Vidéo — *Bitwise Operators (Computerphile)* :
  <https://www.youtube.com/watch?v=hT8t1xN-Q3w>
