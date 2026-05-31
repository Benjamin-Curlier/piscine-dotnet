# ex01-dependance — Injection par constructeur

## Objectif

Lis un **entier** `n`. Tu as deux services :

- `Multiplieur` : sait doubler un nombre (`Doubler(x)` renvoie `x * 2`).
- `Traitement` : reçoit un `Multiplieur` **dans son constructeur** et expose `Traiter(n)` qui renvoie
  `multiplieur.Doubler(n)`.

Enregistre les deux services, résous `Traitement` (le conteneur **injecte** automatiquement le
`Multiplieur`), puis affiche `Traiter(n)`.

Exemple : `5` → `10`.

## Livrable

- `Dependance.cs`

## Indices

- Ajoute `using Microsoft.Extensions.DependencyInjection;` en haut.
- `Multiplieur` : `public int Doubler(int x) => x * 2;`.
- `Traitement` déclare sa dépendance dans son constructeur :
  `public Traitement(Multiplieur m) => _m = m;` puis `public int Traiter(int n) => _m.Doubler(n);`.
- Enregistre-les : `services.AddSingleton<Multiplieur>(); services.AddSingleton<Traitement>();`.
- Construis le provider et résous `Traitement` : tu n'écris **jamais** `new Traitement(...)`, le
  conteneur s'en charge.
- `int.Parse(System.Console.ReadLine())` lit l'entier ; instructions d'abord, classes après.
