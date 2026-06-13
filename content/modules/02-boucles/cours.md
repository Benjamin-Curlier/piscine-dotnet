# Module 02 — Boucles

Tu sais déclarer des variables et écrire des conditions. Pour **répéter** une action sans copier
le code dix fois, on utilise des **boucles**.

## 1. La boucle `while`

`while` répète un bloc **tant que** sa condition est vraie :

```csharp
int i = 1;
while (i <= 3)
{
    System.Console.WriteLine(i);   // 1, puis 2, puis 3
    i++;                           // sans cette ligne, la boucle ne s'arrête jamais !
}
```

Le `i++` (incrément) fait avancer la boucle vers sa fin. **Oublie-le et tu boucles à l'infini.**

## 2. La boucle `for`

`for` regroupe les trois temps d'une boucle sur une seule ligne :
**initialisation ; condition ; incrément**.

```csharp
for (var i = 1; i <= 3; i++)
{
    System.Console.WriteLine(i);   // 1, 2, 3
}
```

C'est la forme idéale quand tu sais **combien de fois** répéter, ou quand tu as besoin d'un
**compteur**. On peut aussi compter à l'envers :

```csharp
for (var i = 3; i >= 1; i--)
{
    System.Console.WriteLine(i);   // 3, 2, 1
}
```

## 3. Accumuler une valeur {#accumulateur}

Pour **cumuler** un résultat, on déclare une variable **avant** la boucle et on l'enrichit à
chaque tour avec `+=` :

```csharp
var somme = 0;
for (var i = 1; i <= 5; i++)
{
    somme += i;        // équivaut à : somme = somme + i
}
System.Console.WriteLine(somme);   // 15  (1+2+3+4+5)
```

## 4. La boucle `foreach` (aperçu)

Quand on parcourt une **collection** (tableau, liste), `foreach` prend chaque élément l'un après
l'autre, sans gérer d'index :

```csharp
foreach (var mot in new[] { "a", "b", "c" })
{
    System.Console.WriteLine(mot);
}
```

On la travaillera vraiment avec les **tableaux**, dans un module suivant.

### Exercices du module

- **[ex00-compte-rebours](#compte-rebours)** : afficher un compte à rebours de N à 1.
- **[ex01-table](#table)** : afficher la table de multiplication d'un nombre.
- **[ex02-somme-n](#somme-n)** : additionner tous les entiers de 1 à N.
- **[ex03-fibonacci](#fibonacci)** : *(bonus, difficile)* afficher le N-ième terme de Fibonacci.

#### compte-rebours {#compte-rebours}
Lis un entier `N`, affiche `N`, `N-1`, … jusqu'à `1` (un par ligne). Indice : `for` décroissant.

#### table {#table}
Lis un entier `N`, affiche `1 x N = …` jusqu'à `10 x N = …` (un par ligne). Indice : `for` de 1 à 10.

#### somme-n {#somme-n}
Lis un entier `N`, affiche la somme `1 + 2 + … + N`. Indice : un **accumulateur**.

#### fibonacci {#fibonacci}
*(Bonus, difficile)* Lis `N` et affiche le N-ième terme de la suite de Fibonacci (indexée à 0) :
`F(0)=0`, `F(1)=1`, `F(n)=F(n-1)+F(n-2)`. Indice : deux variables qui « avancent » à chaque tour
(`tmp = a + b ; a = b ; b = tmp`), pas besoin de récursion.

## Bonne pratique git — committer souvent

Chaque exercice qui marche mérite son commit. Si tu casses tout en attaquant le suivant, tu peux
revenir au dernier état qui fonctionnait. Un historique de petits commits clairs est un filet de
sécurité.

## Pour aller plus loin

- Microsoft Learn — *Instructions d'itération (`for`, `while`, `foreach`)* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/statements/iteration-statements>
- Opérateurs d'affectation composée (`+=`, `-=`) :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/language-reference/operators/assignment-operator>
