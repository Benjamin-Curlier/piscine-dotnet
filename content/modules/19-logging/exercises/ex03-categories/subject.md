# Filtrer par catégorie (bonus)

> Exercice **bonus** (difficulté : difficile).

Chaque logger a une **catégorie** (souvent le nom de la classe). On peut régler un niveau
**différent par catégorie** : par exemple tout voir de `App`, mais seulement les avertissements
de `Db`.

## Objectif

Lis **quatre messages** sur l'entrée standard, un par ligne, dans cet ordre :

1. message `App` (Information) ;
2. message `Db` (Information) — **sera filtré** ;
3. message `Db` (Warning) ;
4. message `App` (Information).

Configure un **seul** `LoggerFactory` :

- provider fourni, niveau minimum **Information** ;
- un filtre : `builder.AddFilter("Db", LogLevel.Warning)`.

Crée deux loggers (`App` et `Db`) et émets, dans l'ordre (avec les messages lus) :

```csharp
app.LogInformation("{Message}", msgAppDemarrage);
db.LogInformation("{Message}", msgDbInfo);     // filtré : Db n'émet qu'à partir de Warning
db.LogWarning("{Message}", msgDbWarning);
app.LogInformation("{Message}", msgAppArret);
```

> ⚠️ Ne te contente pas de recopier les 4 lignes lues : le 2ᵉ message (Db en Information) doit
> **disparaître** grâce au filtre. C'est tout l'intérêt de l'exercice.

## Sortie attendue

Pour l'entrée `Démarrage` / `Requête SELECT *` / `Requête lente (1.2s)` / `Arrêt` :

```
App [Information] Démarrage
Db [Warning] Requête lente (1.2s)
App [Information] Arrêt
```

## Livrables

- `Categories.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
