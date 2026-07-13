# Filtrer par catégorie (bonus)

> Exercice **bonus** (difficulté : difficile).

Chaque logger a une **catégorie** (souvent le nom de la classe). On peut régler un niveau
**différent par catégorie** : par exemple tout voir de `App`, mais seulement les avertissements
de `Db`.

## Objectif

Le `Main` (création des loggers `App`/`Db` et émission des logs ci-dessous) t'est **fourni**. À toi
de compléter **`Journalisation.CreerFabrique()`** : configure un **seul** `LoggerFactory` :

- provider fourni, niveau minimum **Information** ;
- un filtre : `builder.AddFilter("Db", LogLevel.Warning)`.

Les émissions déjà écrites dans le `Main` :

```csharp
app.LogInformation("Démarrage");
db.LogInformation("Requête SELECT *");   // filtré : Db n'émet qu'à partir de Warning
db.LogWarning("Requête lente (1.2s)");
app.LogInformation("Arrêt");
```

Aucune entrée standard. La correction interroge directement `IsEnabled` par catégorie : le filtre
doit être **réel**, pas simulé par la sortie.

## Sortie attendue

```
App [Information] Démarrage
Db [Warning] Requête lente (1.2s)
App [Information] Arrêt
```

## Livrables

- `Categories.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
