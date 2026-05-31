# Module 28 — Complexité (Big O) & algorithmes de tri

Écrire du code qui fonctionne, c'est bien. Écrire du code qui **passe à l'échelle** face à de
grands volumes de données, c'est mieux. La **complexité algorithmique** est l'outil qui permet
de raisonner sur cela — avant même d'exécuter le programme.

---

## 1. Notion de complexité Big O {#complexite}

La notation **O(…)** décrit comment le **temps d'exécution** (ou la mémoire) d'un algorithme
évolue en fonction de la taille **n** des données d'entrée, dans le cas le plus défavorable.

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

```csharp
// Tri fusion — implémentation manuelle (récursive)
static int[] TriFusion(int[] t)
{
    if (t.Length <= 1) return t;

    var milieu = t.Length / 2;
    var gauche = new int[milieu];
    var droite = new int[t.Length - milieu];

    System.Array.Copy(t, 0, gauche, 0, milieu);
    System.Array.Copy(t, milieu, droite, 0, droite.Length);

    gauche = TriFusion(gauche);
    droite = TriFusion(droite);

    return Fusionner(gauche, droite);
}

static int[] Fusionner(int[] g, int[] d)
{
    var resultat = new int[g.Length + d.Length];
    int i = 0, j = 0, k = 0;
    while (i < g.Length && j < d.Length)
    {
        if (g[i] <= d[j]) resultat[k++] = g[i++];
        else              resultat[k++] = d[j++];
    }
    while (i < g.Length) resultat[k++] = g[i++];
    while (j < d.Length) resultat[k++] = d[j++];
    return resultat;
}
```

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
