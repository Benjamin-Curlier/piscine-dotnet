# Module 33 — Discriminated unions (hiérarchies scellées)

Beaucoup de domaines se décrivent naturellement par « **soit** ceci, **soit** cela » : une forme
est *soit* un cercle *soit* un rectangle ; un résultat est *soit* un succès *soit* une erreur ; un
nœud d'arbre est *soit* une feuille *soit* une opération. C'est un **type somme**, aussi appelé
**discriminated union**. C# n'a pas (encore) de mot-clé dédié, mais on l'exprime très bien avec une
**hiérarchie scellée** de `record`.

---

## 1. Le principe {#union}

Un type de base **abstrait** et un ensemble **fermé** de variantes :

```csharp
abstract record Forme;
sealed record Cercle(int Rayon) : Forme;
sealed record Rectangle(int Largeur, int Hauteur) : Forme;
sealed record Carre(int Cote) : Forme;
```

- `abstract` : on ne crée jamais une `Forme` « générique », seulement une variante précise.
- `record` : constructeur positionnel, égalité par valeur et **déconstruction** offerts.
- `sealed` : personne ne peut dériver une variante → l'ensemble des cas est **fermé**.

Une valeur `Forme` est donc forcément l'un de ces trois cas, jamais autre chose.

---

## 2. Filtrer par variante : le pattern matching {#pratique}

On distingue les cas avec un `switch` sur le **type** :

```csharp
var aire = forme switch
{
    Cercle c => 3 * c.Rayon * c.Rayon,
    Rectangle r => r.Largeur * r.Hauteur,
    Carre ca => ca.Cote * ca.Cote,
    _ => 0
};
```

Comme la hiérarchie est scellée, le compilateur **sait** que les cas listés couvrent tout (il
avertit si on en oublie un). C'est l'**exhaustivité** : ajouter une variante plus tard fait
ressortir tous les `switch` à compléter — une sécurité que les chaînes de `if` n'offrent pas.

---

## 3. Rendre l'échec explicite {#resultat}

Plutôt qu'une exception ou un `null`, une union peut modéliser le succès **et** l'erreur :

```csharp
abstract record Resultat;
sealed record Succes(int Valeur) : Resultat;
sealed record Erreur(string Message) : Resultat;
```

L'appelant ne peut pas « oublier » de gérer l'erreur : elle est une variante du type, traitée dans
le `switch`. C'est l'esprit des types `Result` de Rust ou `Either` de F#/Haskell.

---

## 4. Des états aux données propres {#etat}

Avec une union, **chaque état déclare exactement ses données** :

```csharp
abstract record Etat;
sealed record EnAttente : Etat;             // aucune donnée
sealed record EnCours(int Pourcent) : Etat; // un pourcentage
sealed record Termine(string Resultat) : Etat;
```

Fini la grosse classe avec dix champs dont la moitié sont `null` selon l'état : les
**combinaisons invalides deviennent inexprimables**.

---

## 5. Déconstruction {#json}

Le pattern matching peut **extraire** les données d'un record dans la foulée :

```csharp
var rendu = v switch
{
    Nombre(var n) => n.ToString(),
    Texte(var t) => "\"" + t + "\"",
    Booleen(var b) => b ? "true" : "false",
    _ => ""
};
```

`Variante(var x)` lie directement le champ à une variable — plus concis que `((Nombre)v).N`.

---

## 6. Unions récursives {#recursive}

Une variante peut contenir d'autres valeurs du même type — parfait pour les **arbres** :

```csharp
abstract record Expr;
sealed record Nombre(int Valeur) : Expr;
sealed record Operation(string Symbole, Expr Gauche, Expr Droite) : Expr;
```

On évalue par un `switch` **récursif** : une `Operation` évalue ses deux sous-arbres, une feuille
renvoie sa valeur. C'est ainsi que fonctionnent compilateurs et interpréteurs.

---

## 7. En pratique

- Union = `abstract record` + `sealed record` par variante.
- Préfère le `switch` exhaustif aux cascades de `if`/`is` : le compilateur t'aide.
- Modélise l'**impossible comme inexprimable** : moins de cas nuls, moins de bugs.

### Exercices du module

- **ex00-forme** — union de formes & aire.
- **ex01-resultat** — succès ou erreur sans exception.
- **ex02-json** — valeur JSON (nombre/texte/booléen).
- **ex03-etat** — machine à états aux données propres.
- **ex04-expr** *(bonus)* — arbre d'expression (union récursive).

## Références externes

- [Records (doc Microsoft)](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record)
- [Pattern matching (doc Microsoft)](https://learn.microsoft.com/dotnet/csharp/fundamentals/functional/pattern-matching)
