# Filtrer par catégorie (bonus)

> Exercice **bonus** (difficulté : difficile).

Chaque logger a une **catégorie** (souvent le nom de la classe). On peut régler un niveau
**différent par catégorie** : par exemple tout voir de `App`, mais seulement les avertissements
de `Db`.

## Objectif

Configure un **seul** `LoggerFactory` :

- provider fourni, niveau minimum **Information** ;
- un filtre : `builder.AddFilter("Db", LogLevel.Warning)`.

Crée deux loggers (`App` et `Db`) et émets, dans l'ordre :

```csharp
app.LogInformation("Démarrage");
db.LogInformation("Requête SELECT *");   // filtré : Db n'émet qu'à partir de Warning
db.LogWarning("Requête lente (1.2s)");
app.LogInformation("Arrêt");
```

Aucune entrée standard.

## Sortie attendue

```
App [Information] Démarrage
Db [Warning] Requête lente (1.2s)
App [Information] Arrêt
```

## Livrables

- `Categories.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
