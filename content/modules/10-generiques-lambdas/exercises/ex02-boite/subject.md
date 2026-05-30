# ex02-boite — Une boîte générique

## Objectif

Écris une **classe générique** `Boite<T>` qui peut contenir n'importe quel type de valeur. Lis un
**mot** (chaîne) sur la première ligne, puis un **entier** sur la deuxième. Crée une
`Boite<string>` contenant le mot et une `Boite<int>` contenant l'entier. Affiche pour chacune :
`Boite contient: <valeur>`.

Exemple : `bonjour` puis `42` → `Boite contient: bonjour` puis `Boite contient: 42`.

## Livrable

- `Boite.cs`

## Indices

- Une classe générique se déclare avec un paramètre de type : `class Boite<T>`.
- Donne-lui une propriété `public T Contenu { get; }` et un constructeur qui la remplit.
- Ajoute une méthode `public string Decrire() => $"Boite contient: {Contenu}";`.
  L'interpolation appelle automatiquement `ToString()`, donc cela marche pour `string` comme `int`.
- Dans les instructions du haut : `new Boite<string>(mot)` et `new Boite<int>(nombre)`.
