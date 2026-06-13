# Module 04 — Tableaux & chaînes

Jusqu'ici tu manipulais des valeurs isolées. On apprend maintenant à gérer des **collections de
taille fixe** (les **tableaux**) et à travailler le texte (les **chaînes**).

## 1. Les tableaux

Un **tableau** (`array`) range plusieurs valeurs **du même type**, accessibles par un **indice**
qui commence à `0` :

```csharp
int[] notes = { 12, 15, 9, 18 };
System.Console.WriteLine(notes[0]);      // 12  (premier élément)
System.Console.WriteLine(notes.Length);  // 4   (nombre d'éléments)
```

On le parcourt avec `for` (via l'indice) ou `foreach` (chaque élément) :

```csharp
var somme = 0;
foreach (var note in notes)
{
    somme += note;
}
```

## 2. Les chaînes de caractères

Une `string` est une suite de **caractères** (`char`). On peut connaître sa longueur, accéder à un
caractère, et la parcourir :

```csharp
string mot = "Piscine";
System.Console.WriteLine(mot.Length);   // 7
System.Console.WriteLine(mot[0]);       // 'P'

foreach (char c in mot)
{
    System.Console.WriteLine(c);        // une lettre par ligne
}
```

### Découper et recomposer

`Split` transforme une chaîne en **tableau** selon un séparateur ; `ToCharArray` donne le tableau
des caractères :

```csharp
string ligne = "10 20 30";
string[] morceaux = ligne.Split(' ');   // { "10", "20", "30" }

char[] lettres = "abc".ToCharArray();    // { 'a', 'b', 'c' }
System.Array.Reverse(lettres);           // { 'c', 'b', 'a' }
string inverse = new string(lettres);    // "cba"
```

Quelques méthodes utiles : `mot.ToLower()`, `mot.ToUpper()`, `mot.Contains("is")`,
`mot.Trim()` (enlève les espaces aux extrémités).

> ⚠️ `Split(' ')` peut produire des morceaux **vides** s'il y a deux espaces. Pour les ignorer :
> `ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)`.

### Exercices du module

- **[ex00-somme-tableau](#somme-tableau)** : additionner une liste de nombres lus sur une ligne.
- **[ex01-inverser](#inverser)** : afficher une chaîne à l'envers.
- **[ex02-voyelles](#voyelles)** : compter les voyelles d'un mot.
- **[ex03-anagramme](#anagramme)** : *(bonus, difficile)* dire si deux mots sont anagrammes.

#### somme-tableau {#somme-tableau}
Lis une ligne de nombres séparés par des espaces, affiche leur somme (`Split` → tableau → boucle).

#### inverser {#inverser}
Lis une chaîne, affiche-la à l'envers (`ToCharArray` + `Array.Reverse`).

#### voyelles {#voyelles}
Lis un mot, affiche combien il contient de voyelles (`a e i o u`), majuscules comprises.

#### anagramme {#anagramme}
*(Bonus, difficile)* Lis deux mots (un par ligne), affiche `oui` s'ils sont anagrammes (mêmes
lettres, ordre différent — insensible à la casse), `non` sinon. Deux mots identiques ne sont **pas**
des anagrammes. Indice : passe en minuscules, trie les caractères (`OrderBy`), compare avec
`SequenceEqual` (`using System.Linq;`).

## Bonne pratique git — le `.gitignore`

Certains fichiers ne doivent **jamais** être versionnés : binaires de build (`bin/`, `obj/`),
secrets, fichiers temporaires. Un fichier **`.gitignore`** à la racine liste ces motifs, et git les
ignore automatiquement :

```gitignore
bin/
obj/
*.log
```

Un dépôt propre ne contient que les **sources**, pas les artefacts régénérables.

## Pour aller plus loin

- Microsoft Learn — *Tableaux* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/arrays/>
- Microsoft Learn — *Chaînes* :
  <https://learn.microsoft.com/fr-fr/dotnet/csharp/programming-guide/strings/>
