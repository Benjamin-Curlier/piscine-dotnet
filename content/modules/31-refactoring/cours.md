# Module 31 — Smelly code & refactoring

Un code qui *marche* n'est pas forcément un code *sain*. Avec le temps, des « mauvaises odeurs »
(**code smells**) s'accumulent : duplication, fonctions interminables, nombres magiques, conditions
en escalier… Elles ne provoquent pas de bug immédiat, mais rendent le code difficile à lire, à
modifier et à tester. Le **refactoring** est l'art d'améliorer la structure du code **sans changer
son comportement**.

> **Règle d'or du refactoring** : on ne refactore *jamais* sans filet. Ce filet, ce sont les
> **tests**. Tant qu'ils restent verts, on sait que le comportement est préservé. Dans ce module,
> les cas `io` jouent ce rôle : ton code de départ les passe déjà — ton refactoring doit les garder
> verts.

---

## 1. Reconnaître les odeurs

| Smell | Symptôme | Remède (ce module) |
|---|---|---|
| **Nombre magique** | `if (prix > 100)` — c'est quoi 100 ? | constante nommée (§2) |
| **Duplication** | le même bloc copié-collé | extraire une méthode (§3) |
| **Conditions imbriquées** | `if` dans `if` dans `if` | clauses-gardes (§4) |
| **Cascade de `if`** | `if/else if/else if…` sur une valeur | `switch` expression (§5) |
| **God function** | une fonction qui « fait tout » | décomposition (§6) |

---

## 2. Nombres magiques → constantes nommées {#constantes}

Une valeur littérale isolée n'explique pas ce qu'elle représente. Lui donner un nom documente
l'intention et centralise la modification.

```csharp
// Avant
var ttc = net + net * 20 / 100;
// Après
const int TauxTvaPourcent = 20;
var ttc = net + net * TauxTvaPourcent / 100;
```

---

## 3. Duplication → extraire une méthode {#extraire}

Dès qu'un bloc se répète, donne-lui un nom. **« Don't Repeat Yourself » (DRY)** : une logique ne
devrait exister qu'à un seul endroit, pour ne la corriger qu'une fois.

```csharp
// Avant : deux blocs identiques pour |x - y|
// Après
static int Distance(int x, int y) => System.Math.Abs(x - y);
```

---

## 4. Conditions imbriquées → clauses-gardes {#guard}

Le « code en escalier » (`if` dans `if`) noie le cas nominal sous les exclusions. Les
**clauses-gardes** traitent les cas d'échec d'abord, avec des **retours anticipés**.

```csharp
// Avant
if (age >= 18) { if (solde >= 0) { ... } }
// Après
if (age < 18) { return false; }
if (solde < 0) { return false; }
return true;
```

Moins d'indentation, conditions d'échec explicites, cas nominal visible à la fin.

---

## 5. Cascade de `if` → `switch` expression {#switch}

Quand on branche sur les valeurs d'une même variable, un `switch` expression est plus court et plus
sûr (le compilateur aide à couvrir les cas).

```csharp
// Avant : if (op == "add") ... else if (op == "sub") ...
// Après
var resultat = op switch
{
    "add" => a + b,
    "sub" => a - b,
    "mul" => a * b,
    _ => 0
};
```

---

## 6. God function → décomposition {#decomposer}

Une fonction qui parse, calcule **et** formate fait trop de choses. Découpe-la : chaque méthode a
**une seule responsabilité** (principe SRP). Le flux principal devient une suite d'étapes lisibles.

```csharp
var (nom, notes) = Parser(ligne);
var moyenne = Moyenne(notes);
System.Console.WriteLine(Formater(nom, moyenne));
```

Chaque morceau est court, nommé, et testable isolément.

---

## 7. En pratique {#pratique}

- Refactore **par petits pas**, en relançant les tests après chacun (`piscine check <exo>`).
- Ne mélange jamais refactoring et ajout de fonctionnalité dans le même mouvement : d'abord l'un,
  puis l'autre.
- Le but n'est pas la perfection mais la **lisibilité** : du code qu'un collègue (ou toi dans six
  mois) comprend sans effort.

### Exercices du module

- **ex00-constantes** — nombres magiques → constantes nommées.
- **ex01-extraire-methode** — duplication → méthode.
- **ex02-guard-clauses** — conditions imbriquées → clauses-gardes.
- **ex03-switch** — cascade de `if` → `switch` expression.
- **ex04-decomposer** *(bonus)* — god function → décomposition.

## Références externes

- [Refactoring.Guru — Code Smells](https://refactoring.guru/refactoring/smells)
- [Martin Fowler — Refactoring](https://refactoring.com/)
