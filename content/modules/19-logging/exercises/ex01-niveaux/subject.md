# Niveaux de log

Chaque message porte un **niveau de gravité** : `Trace`, `Debug`, `Information`, `Warning`,
`Error`, `Critical`. On choisit le **niveau minimum** émis ; tout ce qui est en dessous est ignoré.

## Objectif

Lis un nom de composant sur l'entrée. Avec un logger de catégorie `App` réglé au niveau minimum
**Information**, émets dans l'ordre :

- `Debug` → `Trace interne détaillée` *(ne doit pas apparaître)*
- `Information` → `<nom> prêt`
- `Warning` → `Mémoire faible`
- `Error` → `Échec du traitement`

`LogCapture.cs` t'est fourni (ne le modifie pas).

## Exemple

Entrée :

```
Cache
```

Sortie (le `Debug` est filtré) :

```
App [Information] Cache prêt
App [Warning] Mémoire faible
App [Error] Échec du traitement
```

## Livrables

- `Niveaux.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
