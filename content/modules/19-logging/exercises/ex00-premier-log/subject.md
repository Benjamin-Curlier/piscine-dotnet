# Premier log

Au lieu d'appeler `Console.WriteLine` partout, une application sérieuse **journalise** : elle
émet des messages typés (niveau, catégorie) qu'on peut filtrer et rediriger.

## Objectif

Lis un message sur l'entrée standard, puis journalise-le en niveau **Information** avec un logger
de catégorie `App`.

Le fichier `LogCapture.cs` t'est **fourni** : il contient un `ILoggerProvider` qui écrit chaque log
sur la sortie standard au format `Catégorie [Niveau] message`. **Ne le modifie pas** — utilise-le.

## Marche à suivre

1. Crée une fabrique : `LoggerFactory.Create(builder => { ... })` (avec `using var`).
   - `builder.AddProvider(new CaptureLoggerProvider());`
   - `builder.SetMinimumLevel(LogLevel.Information);`
2. Obtiens un logger : `var logger = factory.CreateLogger("App");`
3. Journalise : `logger.LogInformation(message);`

## Exemple

Entrée :

```
Service démarré
```

Sortie :

```
App [Information] Service démarré
```

## Livrables

- `PremierLog.cs` (ton programme)
- `LogCapture.cs` (fourni, à rendre tel quel)
