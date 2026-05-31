# ex00-using — using & Dispose

## Objectif

En .NET, la mémoire est gérée automatiquement par le **ramasse-miettes** (garbage collector).
Mais certaines ressources (fichiers, connexions, sockets) doivent être **libérées explicitement**
et au bon moment : c'est le rôle de `IDisposable` et du bloc `using`.

Lis un nom de ressource. Écris une classe qui affiche `ouvre <nom>` à sa création et `ferme <nom>`
quand elle est libérée. Utilise-la dans un `using` et affiche `utilise <nom>` à l'intérieur.

Exemple : `data` →
```
ouvre data
utilise data
ferme data
```

## Livrable

- `Fichier.cs`

## Contraintes

- La classe implémente `System.IDisposable`.
- La libération passe par un bloc `using` (pas un appel manuel à `Dispose`).

## Indices

- `sealed class Fichier : System.IDisposable { ... public void Dispose() => ...; }`.
- `using (var f = new Fichier(nom)) { ... }` : `Dispose()` est appelé automatiquement à la sortie
  du bloc, même en cas d'exception.
