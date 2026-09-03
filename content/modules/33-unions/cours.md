# Module 33 — Types somme avec des hiérarchies de records

Beaucoup de domaines se décrivent naturellement par « **soit** ceci, **soit** cela » : une forme
est *soit* un cercle *soit* un rectangle ; un résultat est *soit* un succès *soit* une erreur ; un
nœud d'arbre est *soit* une feuille *soit* une opération. C'est un **type somme**, aussi appelé
**discriminated union**. Dans la version de C# ciblée par ce parcours, il n'existe pas de type somme
natif. On en construit une **approximation conventionnelle** avec une classe de base abstraite et
des variantes sous forme de `record`.

---

## 1. Le principe {#union}

Un type de base **abstrait** et les variantes prévues par le modèle :

```csharp
abstract record Forme;
sealed record Cercle(int Rayon) : Forme;
sealed record Rectangle(int Largeur, int Hauteur) : Forme;
sealed record Carre(int Cote) : Forme;
```

- `abstract` : on ne crée jamais une `Forme` « générique », seulement une variante précise.
- `record` : constructeur positionnel, égalité par valeur et **déconstruction** offerts.
- `sealed` : personne ne peut spécialiser davantage `Cercle`, `Rectangle` ou `Carre`.

Attention : cela ne ferme pas la classe de base. Un autre fichier pourrait encore déclarer un
nouveau `record Triangle : Forme`. Le caractère « fermé » du modèle repose donc ici sur une
**convention de conception** et sur le contrôle du code qui déclare les variantes.

---

## 2. Filtrer par variante : le pattern matching {#pratique}

On distingue les cas avec un `switch` sur le **type** :

```csharp
var aire = forme switch
{
    Cercle c => 3 * c.Rayon * c.Rayon,
    Rectangle r => r.Largeur * r.Hauteur,
    Carre ca => ca.Cote * ca.Cote,
    _ => throw new ArgumentOutOfRangeException(nameof(forme))
};
```

Le cas de repli reste nécessaire : en C# 14, le compilateur ne sait pas que ces trois variantes sont
les seules autorisées. Le `throw` rend une variante inattendue visible au lieu de fabriquer une aire
fausse. Lorsqu'on ajoute une variante, il faut rechercher et compléter les `switch` concernés ; les
tests doivent couvrir cette évolution.

---

## 3. Rendre l'échec explicite {#resultat}

Plutôt qu'une exception ou un `null`, une union peut modéliser le succès **et** l'erreur :

```csharp
abstract record Resultat;
sealed record Succes(int Valeur) : Resultat;
sealed record Erreur(string Message) : Resultat;
```

L'erreur devient une donnée explicite que l'appelant peut traiter dans le `switch`, plutôt qu'un
`null` ambigu. Le compilateur exige néanmoins toujours un cas de repli pour cette hiérarchie
ouverte. C'est l'esprit des types `Result` de Rust ou `Either` de F#/Haskell.

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

L'union permet aussi de contrôler les **transitions**. Une commande `progres 50` n'est valable que
pour un `EnCours`; un état `Termine` ne peut plus revenir en arrière. Un `switch` sur l'état courant
et la commande rend ces règles visibles au même endroit — c'est l'objet de l'ex03.

---

## 5. Déconstruction : rendre une valeur JSON {#json}

Le pattern matching peut **extraire** les données d'un record dans la foulée. On l'illustre sur une
petite union « valeur JSON » (`Nombre` / `Texte` / `Booleen`), qu'on transforme en sa
représentation textuelle :

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

- Approximation d'une union = `abstract record` + un `sealed record` par variante prévue.
- Préfère un `switch` lisible avec un cas de repli explicite aux cascades de `if`/`is`.
- Modélise l'**impossible comme inexprimable** : moins de cas nuls, moins de bugs.

### Exercices du module

- **ex00-forme** — union de formes & aire.
- **ex01-resultat** — succès ou erreur sans exception.
- **ex02-json** — valeur JSON (nombre/texte/booléen).
- **ex03-etat** — machine à états et transitions autorisées.
- **ex04-expr** *(bonus)* — arbre d'expression (union récursive).

## Références externes

- [Records (doc Microsoft)](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record)
- [Pattern matching (doc Microsoft)](https://learn.microsoft.com/dotnet/csharp/fundamentals/functional/pattern-matching)
