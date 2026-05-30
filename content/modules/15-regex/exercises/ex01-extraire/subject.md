# ex01-extraire — Extraire les nombres

## Objectif

Lis **une seule ligne** de texte. Trouve tous les **groupes de chiffres** qu'elle contient et
affiche chaque nombre sur sa **propre ligne**, dans l'ordre d'apparition.

Exemple : `abc123def45` → `123` puis `45`.

## Livrable

- `Extraire.cs`

## Indices

- Importe `using System.Text.RegularExpressions;`.
- Le motif `@"\d+"` (sans `^` ni `$`) capture chaque suite de chiffres **où qu'elle soit**.
- `Regex.Matches(ligne, @"\d+")` renvoie toutes les occurrences ; parcours-les avec un `foreach`.
- Chaque occurrence est un `Match` ; sa propriété `.Value` donne le texte trouvé :
  `foreach (Match m in ...) System.Console.WriteLine(m.Value);`.
