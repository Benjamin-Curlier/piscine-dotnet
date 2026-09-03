# Module 28 — Complexité (Big O) & algorithmes de tri

Écrire du code qui fonctionne, c'est bien. Écrire du code qui **passe à l'échelle** face à de
grands volumes de données, c'est mieux. La **complexité algorithmique** est l'outil qui permet
de raisonner sur cela — avant même d'exécuter le programme.

---

## 1. Notion de complexité Big O {#complexite}

La notation **O(…)** donne une **borne asymptotique supérieure** sur l'évolution du temps
d'exécution (ou de la mémoire) en fonction de la taille **n**. Il faut préciser quelle situation on
mesure : meilleur cas, cas moyen ou pire cas. Par convention, on annonce souvent le pire cas, mais
ce n'est pas la définition de Big O.

| Notation | Nom | Intuition |
|---|---|---|
| **O(1)** | Constante | Toujours le même coût, quelle que soit la taille. Exemple : lire `t[0]`. |
| **O(log n)** | Logarithmique | Chaque étape divise le problème par deux. Exemple : recherche binaire. |
| **O(n)** | Linéaire | On parcourt tous les éléments une fois. Exemple : trouver le max dans un tableau. |
| **O(n log n)** | Quasi-linéaire | Efficace pour les tris. Exemple : tri fusion, quicksort moyen. |
| **O(n²)** | Quadratique | Deux boucles imbriquées sur n éléments. Exemple : tri à bulles, tri par insertion. |

### Pourquoi c'est important ?

Pour **n = 10 000** éléments :

- O(n) → 10 000 opérations — imperceptible.
- O(n²) → 100 000 000 opérations — peut prendre plusieurs secondes.
- O(n log n) → ~130 000 opérations — très rapide.

On ne compte pas les constantes ni les termes dominés : O(3n + 5) = O(n).

---

## 2. Tri à bulles {#tri-bulle}

**Idée** : parcourir le tableau en répétant les comparaisons de paires voisines. Si deux voisins
sont dans le mauvais ordre, on les échange. On répète jusqu'à ce qu'aucun échange ne soit
nécessaire — le plus grand « bulle » vers la fin à chaque passe.

```csharp
// Tri à bulles — implémentation manuelle
var n = int.Parse(System.Console.ReadLine());
var t = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);

for (var i = 0; i < n - 1; i++)
{
    for (var j = 0; j < n - 1 - i; j++)
    {
        if (t[j] > t[j + 1])
        {
            var tmp = t[j];
            t[j]     = t[j + 1];
            t[j + 1] = tmp;
        }
    }
}

System.Console.WriteLine(string.Join(" ", t));
```

**Complexité** : O(n²) dans le pire cas. Simple à comprendre, mais lent pour de grands tableaux.

---

## 3. Tri par insertion {#tri-insertion}

**Idée** : construire le tableau trié élément par élément. On prend le prochain élément non trié
et on l'« insère » à la bonne position dans la partie déjà triée, en décalant les autres vers la
droite.

```csharp
// Tri par insertion — implémentation manuelle
var n = int.Parse(System.Console.ReadLine());
var t = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);

for (var i = 1; i < n; i++)
{
    var cle = t[i];
    var j   = i - 1;
    while (j >= 0 && t[j] > cle)
    {
        t[j + 1] = t[j];
        j--;
    }
    t[j + 1] = cle;
}

System.Console.WriteLine(string.Join(" ", t));
```

**Complexité** : O(n²) dans le pire cas ; O(n) si le tableau est déjà trié (très bon cas moyen
sur des données presque ordonnées).

---

## 4. Recherche binaire {#recherche-binaire}

**Pré-requis** : le tableau doit être **trié** par ordre croissant.

**Idée** : comparer la cible avec l'élément du **milieu**. Si c'est lui, on a trouvé. Sinon, on
sait si la cible est dans la moitié gauche ou droite, et on recommence sur la moitié concernée.
À chaque étape, l'espace de recherche est **divisé par deux**.

```csharp
// Recherche binaire — implémentation manuelle
var n      = int.Parse(System.Console.ReadLine());
var t      = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);
var cible  = int.Parse(System.Console.ReadLine());

var gauche = 0;
var droite = n - 1;
var indice = -1;

while (gauche <= droite)
{
    var milieu = (gauche + droite) / 2;
    if (t[milieu] == cible)
    {
        indice = milieu;
        break;
    }
    else if (t[milieu] < cible)
    {
        gauche = milieu + 1;
    }
    else
    {
        droite = milieu - 1;
    }
}

System.Console.WriteLine(indice);
```

**Complexité** : O(log n) — pour un million d'éléments, au plus 20 comparaisons.

---

## 5. Tri fusion {#tri-fusion}

**Idée** : diviser-pour-régner. On divise le tableau en deux moitiés, on trie chacune
**récursivement**, puis on **fusionne** les deux moitiés triées en un seul tableau trié.

```text
TRIER(tableau)
  si le tableau contient au plus un élément : il est déjà trié
  le séparer en une moitié gauche et une moitié droite
  gaucheTriée ← TRIER(gauche)
  droiteTriée ← TRIER(droite)
  fusionner gaucheTriée et droiteTriée

FUSIONNER(gauche, droite)
  comparer les deux premiers éléments encore disponibles
  déplacer le plus petit vers le résultat
  recommencer, puis copier les éléments restants
```

Exemple de fusion : `[2, 7]` et `[1, 5, 8]` donnent successivement
`[1]`, `[1, 2]`, `[1, 2, 5]`, `[1, 2, 5, 7]`, puis `[1, 2, 5, 7, 8]`.
Le cours fournit ainsi la stratégie ; l'exercice te demande de la traduire en C# et de gérer les
indices correctement.

**Complexité** : O(n log n) dans tous les cas — bien meilleur que O(n²) pour de grands tableaux.

---

## 6. En pratique {#pratique}

En production on n'implémente pas ces algorithmes à la main : on utilise `Array.Sort()` (introsort,
O(n log n)) ou LINQ `.OrderBy()`. Comprendre les algorithmes sous-jacents permet de **choisir
la bonne structure**, d'**analyser les goulots d'étranglement** et de réussir les entretiens
techniques.

### Exercices du module

- **[ex00-tri-bulle](#tri-bulle)** : implémenter le tri à bulles à la main.
- **[ex01-tri-insertion](#tri-insertion)** : implémenter le tri par insertion à la main.
- **[ex02-recherche-binaire](#recherche-binaire)** : recherche binaire sur un tableau trié.
- **[ex03-tri-fusion](#tri-fusion)** *(bonus)* : tri fusion récursif à la main.

---

## Références externes

- Microsoft Learn — *Vue d'ensemble des algorithmes* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/collections/algorithm-complexity>
- Big-O Cheat Sheet :
  <https://www.bigocheatsheet.com/>
- Visualgo — animations des algorithmes de tri :
  <https://visualgo.net/fr/sorting>
