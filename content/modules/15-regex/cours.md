# Module 15 — Regex : expressions régulières

Une **expression régulière** (ou *regex*) est un petit motif qui décrit une **forme de texte** :
« uniquement des chiffres », « un mot suivi d'un nombre », « quelque chose qui ressemble à un
email »… On s'en sert pour **valider**, **chercher** ou **extraire** du texte sans écrire des
dizaines de `if`.

## 1. À quoi ça sert

Imagine que tu veuilles savoir si une chaîne ne contient **que** des chiffres. Sans regex, tu
parcourrais chaque caractère à la main. Avec une regex, tu décris le motif une fois et la classe
`Regex` fait le travail :

```csharp
using System.Text.RegularExpressions;

bool ok = Regex.IsMatch("12345", @"^\d+$");   // true
```

> Une regex est juste une **chaîne de caractères** interprétée comme un motif. En C# on l'écrit
> presque toujours en **verbatim string** `@"..."` (voir section 5).

## 2. La classe `Regex`

Tout passe par la classe `Regex`, fournie par l'espace de noms `System.Text.RegularExpressions`.
Il faut donc l'importer **explicitement** :

```csharp
using System.Text.RegularExpressions;
```

Trois opérations suffisent pour ce module :

| Méthode | Rôle | Renvoie |
|---|---|---|
| `Regex.IsMatch(texte, motif)` | Le texte correspond-il au motif ? | `bool` |
| `Regex.Matches(texte, motif)` | Toutes les occurrences trouvées | une collection de `Match` |
| `Regex.Match(texte, motif)` | La première occurrence | un `Match` |

### IsMatch — valider {#correspond}

`IsMatch` répond simplement *oui / non* :

```csharp
using System.Text.RegularExpressions;

System.Console.WriteLine(Regex.IsMatch("abc", @"^\d+$") ? "oui" : "non");   // non
System.Console.WriteLine(Regex.IsMatch("123", @"^\d+$") ? "oui" : "non");   // oui
```

### Matches — extraire {#extraire}

`Matches` trouve **toutes** les portions qui correspondent. Chaque résultat est un objet `Match`,
et sa propriété `.Value` donne le texte trouvé :

```csharp
using System.Text.RegularExpressions;

foreach (Match m in Regex.Matches("abc123def45", @"\d+"))
{
    System.Console.WriteLine(m.Value);   // 123, puis 45
}
```

## 3. Les métacaractères courants

Un motif mélange des caractères **littéraux** (qui se représentent eux-mêmes) et des
**métacaractères** (qui ont un sens spécial) :

| Symbole | Signifie |
|---|---|
| `\d` | un chiffre (0–9) |
| `\w` | un caractère « de mot » (lettre, chiffre ou `_`) |
| `\s` | un espace blanc (espace, tabulation, retour ligne) |
| `+` | une ou plusieurs fois l'élément précédent |
| `*` | zéro ou plusieurs fois |
| `^` | début de la chaîne |
| `$` | fin de la chaîne |
| `[abc]` | l'un des caractères listés (`a`, `b` ou `c`) |
| `[^abc]` | n'importe quel caractère **sauf** ceux listés |

Quelques exemples :

- `^\d+$` : du **début** à la **fin**, uniquement des chiffres → « que des chiffres ».
- `\d+` (sans `^` ni `$`) : un groupe de chiffres **où qu'il soit** dans le texte.
- `[^@\s]+` : un ou plusieurs caractères qui ne sont ni `@` ni un espace.

> **`^` et `$` sont essentiels pour valider.** Sans eux, `\d+` accepte `"45a"` (il y trouve `45`).
> Avec `^\d+$`, on exige que **toute** la chaîne soit des chiffres.

## 4. Groupes (mention)

On peut entourer une partie du motif de **parenthèses** pour la « capturer » et la récupérer
séparément. Par exemple `(\d+)-(\d+)` capture deux nombres autour d'un tiret, accessibles ensuite
via `m.Groups[1].Value` et `m.Groups[2].Value`. On ne s'en sert pas dans les exercices de ce
module, mais retiens que les parenthèses servent à **grouper**.

## 5. Verbatim strings `@"..."`

Les motifs regex sont remplis d'antislashs (`\d`, `\w`, `\s`…). Or, dans une chaîne C# classique,
`\` introduit une séquence d'échappement (`\n`, `\t`…). Pour éviter d'avoir à doubler chaque
antislash, on utilise une **verbatim string** préfixée par `@` : tout y est pris au pied de la
lettre.

```csharp
var motif1 = "\\d+";    // chaîne classique : antislash doublé, pénible
var motif2 = @"\d+";    // verbatim : exactement le motif voulu, lisible
```

**Toujours préférer `@"..."` pour un motif regex.**

## 6. Note sur la performance

Quand un même motif est utilisé **très souvent**, on peut le pré-compiler avec
`new Regex(@"\d+", RegexOptions.Compiled)` : le moteur produit du code optimisé une fois pour
toutes. Plus moderne encore, .NET propose les **source generators** (`[GeneratedRegex(...)]`) qui
construisent le moteur **à la compilation**, sans coût au démarrage. Pour ce module, les méthodes
statiques `Regex.IsMatch` / `Regex.Matches` suffisent largement ; garde simplement en tête que ces
options existent pour le code à fort débit.

### Exercices du module

- **[ex00-correspond](#correspond)** : valider qu'une ligne ne contient que des chiffres.
- **[ex01-extraire](#extraire)** : extraire tous les nombres d'une ligne.
- **[ex02-email](#email)** : reconnaître une adresse email simple.

#### correspond {#correspond}
Lis N puis N lignes ; affiche `oui` si la ligne ne contient que des chiffres (`^\d+$`), sinon `non`.

#### extraire {#extraire}
Lis une ligne ; affiche chaque groupe de chiffres (`\d+`) sur sa propre ligne.

#### email {#email}
Lis N puis N lignes ; affiche `valide` si la ligne ressemble à un email (`^[^@\s]+@[^@\s]+\.[^@\s]+$`), sinon `invalide`.

## Références externes

- Microsoft Learn — *Expressions régulières .NET* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/base-types/regular-expressions>
- Microsoft Learn — *Langage des expressions régulières (aide-mémoire)* :
  <https://learn.microsoft.com/fr-fr/dotnet/standard/base-types/regular-expression-language-quick-reference>
- **regex101.com** — un terrain de jeu en ligne pour tester et comprendre un motif pas à pas :
  <https://regex101.com/>
- Vidéo d'introduction (FreeCodeCamp, anglais sous-titrable) :
  <https://www.youtube.com/watch?v=909NfO1St0A>
