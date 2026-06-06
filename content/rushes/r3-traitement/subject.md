# Rush 3 — Traitement de commandes (Worker)

> **Un Rush est un projet de synthèse solo.** Pas de nouveau cours : tu réutilises l'**hôte
> générique** (M20), l'**injection de dépendances** (M18), le **logging** (M19) et les **Channels**.

## Le contexte

Tu écris un **service de traitement** façon « worker » : des commandes arrivent dans une **file**,
ton service les consomme une à une, décide de les **accepter** ou de les **rejeter**, journalise
chaque décision, puis dresse un **bilan** avant de s'arrêter.

La file est un **`Channel<T>` en mémoire** : ici elle *simule* une arrivée réseau. (Brancher une
vraie socket serait non déterministe — c'est une pratique locale hors notation.)

## Le problème

Lis **un entier** `N` sur l'entrée standard, puis lis `N` lignes au format :

```
nom montant
```

`nom` et `montant` sont séparés par **un espace** ; `montant` est un **entier** (éventuellement
négatif ou nul).

Construis un **hôte générique** (`Host.CreateApplicationBuilder`) et un `BackgroundService` qui :

1. **pousse** chaque commande dans un `Channel.CreateUnbounded<Commande>()`, puis `Writer.Complete()` ;
2. **consomme** la file via `Reader.ReadAllAsync` (ordre **FIFO déterministe**) :
   - si `montant > 0` → **acceptée** :
     `logger.LogInformation("Commande acceptée : {Nom} ({Montant})", ...)` ;
   - sinon → **rejetée** : `logger.LogWarning("Commande rejetée : {Nom}", ...)` ;
3. journalise enfin le **bilan** :
   `LogInformation("Bilan : {Acceptees} acceptée(s), {Rejetees} rejetée(s), total {Total}", ...)`
   (le `total` est la somme des montants **acceptés**) ;
4. appelle `IHostApplicationLifetime.StopApplication()` pour terminer.

La décision d'acceptation doit être prise par un **service `Validateur` injecté** (DI), pas en dur
dans le worker.

### Journalisation déterministe

Le provider console par défaut écrit en arrière-plan (sortie non déterministe). Un fichier
**`LogCapture.cs` est FOURNI** : il écrit chaque log de façon synchrone au format
`Catégorie [Niveau] message`. Configure l'hôte ainsi :

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);
```

La **catégorie** affichée est le nom de ta classe worker : si tu la nommes `Traitement`, les lignes
commencent par `Traitement [Information] ...` ou `Traitement [Warning] ...`.

### Exemple

Entrée :

```
3
alice 10
bob -5
carol 20
```

Sortie :

```
Traitement [Information] Commande acceptée : alice (10)
Traitement [Warning] Commande rejetée : bob
Traitement [Information] Commande acceptée : carol (20)
Traitement [Information] Bilan : 2 acceptée(s), 1 rejetée(s), total 30
```

`alice` et `carol` (montants positifs) sont acceptées, `bob` (montant négatif) est rejetée ; le total
des montants acceptés vaut `10 + 20 = 30`.

## Livrables

- `Traitement.cs` (ton programme et tes types)
- `LogCapture.cs` (FOURNI — ne le modifie pas)

## Conseils

- **Instructions top-level d'abord**, puis les types (`record`, classes) **après**.
- Pousse toutes les commandes **avant** `Writer.Complete()`, puis consomme : la file vide la
  file dans l'ordre d'insertion (FIFO).
- Le `Validateur` est un simple service (`AddSingleton<Validateur>()`) avec une méthode
  `EstAcceptee(Commande)` ; injecte-le dans le worker.
- N'oublie pas `StopApplication()` : sinon l'hôte tourne indéfiniment.

## Rendu

Comme pour un exercice : travaille dans ton workspace, puis `git add` / `commit` / `push origin main`.
La moulinette corrige le Rush comme un livrable autonome.
