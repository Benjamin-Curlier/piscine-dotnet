# ex01-lock — Compteur protégé par lock

## Objectif

Lis un entier **N**. Incrémente un **compteur** partagé **N fois**, le travail étant réparti sur
plusieurs threads avec `Parallel.For`. Protège chaque incrément avec le mot-clé **`lock`** pour
éviter les *race conditions*. Affiche le **compteur final** (qui vaut exactement `N`).

Exemple : `1000` → `1000`.

## Livrable

- `Lock.cs`

## Indices

- Lis l'entier : `var n = int.Parse(System.Console.ReadLine());`.
- `compteur++` n'est **pas** atomique : lecture, +1, écriture. Si deux threads le font en même
  temps, des incréments se perdent et le total est trop faible.
- Crée un objet verrou : `var verrou = new object();`.
- Dans `Parallel.For(0, n, _ => { ... })`, entoure l'incrément d'un `lock (verrou) { compteur++; }` :
  un seul thread à la fois entre dans le bloc.
- Le paramètre s'appelle `_` car on ne s'en sert pas (on incrémente toujours de 1).
- `using System.Threading.Tasks;` est nécessaire pour `Parallel`.
