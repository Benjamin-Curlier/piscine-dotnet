# Spec — Isolation de l'exécution du code recrue dans un processus enfant jetable

> Conception du 2026-06-09. Branche `fix/review-grading-integrity-hardening`.
> Contexte : rapport de revue approfondie ;
> [retex migration PhotinoX](../retex/2026-06-08-photinox-migration.md).

## Problème

`ProgramRunner` et `XunitRunner` (projet `Piscine.Grading`) exécutent du code recrue **non fiable**
en mémoire, dans le **processus de correction lui-même**, via `Task.Run` + `task.Wait(timeout)`.
Au timeout, la tâche n'est **jamais annulée** :

1. **Fuite de thread** — le thread de travail continue d'exécuter la boucle infinie de la recrue
   (CPU brûlé en continu) ; rien ne le termine.
2. **Fuite d'assembly** — le `finally` appelle `alc.Unload()` sur un `AssemblyLoadContext` collectible
   *encore actif* : il ne se décharge donc jamais. Chaque timeout fuit un assembly.
3. **Corruption de sortie inter-exécutions** — `ProgramRunner` restaure le `Console.Out`/`Console.In`
   **process-global** dans son `finally`, alors que la tâche orpheline peut encore écrire. Comme
   `Console.WriteLine` relit le `Console.Out` global *à chaque appel*, l'orpheline d'une exécution
   écrit dans le `StringWriter` capturé d'une exécution **ultérieure** → résultats faussés.
4. **Fuite de fixture** — `XunitRunner.RunOne` crée l'instance de test par réflexion mais ne la
   **dispose jamais** : les fixtures `IDisposable`/`IAsyncDisposable` qui tiennent fichiers/sockets
   fuient.

Sur une session de correction longue (ré-essais, `grade-received` sur plusieurs exercices,
`MutationGrader` exécutant *N* mutants), ces fuites s'accumulent et la sortie peut se contaminer
d'une exécution à l'autre.

Bugs latents supplémentaires du modèle in-process (corrigés gratuitement par le modèle enfant) :

- un appel recrue à `Environment.Exit(n)` **tue le processus de correction entier** ;
- un `StackOverflowException` / `Environment.FailFast` recrue **fait planter la moulinette**.

## Objectif

Rendre une soumission qui dépasse le délai **réellement terminable** et **isolée** : chaque
exécution de code non fiable se déroule dans un **processus enfant jetable** que le parent peut
**tuer (arbre de processus complet)** au timeout, récupérant ainsi thread, assembly et capture de
sortie d'un seul coup. La frontière de processus *est* l'isolation.

## Non-objectifs

- Pas de bac à sable de **sécurité** au sens OS (seccomp, job objects, quotas mémoire/CPU,
  restrictions réseau/FS). On vise l'intégrité de la correction et la terminabilité, pas le
  confinement d'un adversaire actif. (Suivi possible ultérieur.)
- Pas d'optimisation du temps de démarrage (ReadyToRun / AOT) dans cette itération.
- Pas de plafond sur la taille de la sortie capturée.
- **Aucune modification des points d'entrée** des front-ends (`Cli`, `App`, `Desktop`, `DevHost`).

## Décisions actées

- **Le processus enfant est la seule isolation.** Les parades in-process envisagées (écrivain
  `Console` routé par `AsyncLocal`, `alc.Unload()` différé via `ContinueWith`) sont **abandonnées** :
  la mort du processus récupère tout. Le bac à sable charge l'assembly dans un `AssemblyLoadContext`
  ordinaire et laisse la sortie du processus le récupérer. **Aucune mutation du `Console` global
  côté parent.**
- **La seule correction d'hygiène conservée** : disposer l'instance de test dans un `finally`
  (`IDisposable` et `IAsyncDisposable`), désormais **dans le bac à sable**.
- **Fail-closed** : si le binaire du bac à sable ne peut être résolu ou lancé, le grader renvoie une
  **erreur interne** (ni échec recrue, ni faux « Réussi »). Aucun repli in-process — un repli
  réintroduirait exactement les bugs supprimés et masquerait la casse de packaging.

## Architecture

```
IoGrader / ReseauGrader / ProjectGrader / UnitGrader / MutationGrader / TryCommand
        │ (appelants inchangés)
        ▼
ProgramRunner.Run(...) / XunitRunner.Run(...)        ← réécrits en CLIENTS de lancement (Piscine.Grading)
        │  écrit asm.dll + request.json dans un dossier temp unique ; lance ; attend(timeout) ;
        │  tue l'arbre si dépassement ; lit result.json ; nettoie le temp
        ▼
Piscine.Sandbox(.exe)  ← NOUVEAU. Charge les octets dans un ALC, exécute io OU xunit,
                          écrit result.json, sort.
```

### Nouveau projet `src/Piscine.Sandbox`

- Type : **exécutable console** (`OutputType=Exe`), `net10.0`.
- Dépendances : **uniquement** `xunit.core` + `xunit.assert` (pas de Roslyn, pas de LibGit2Sharp).
  `System.Text.Json` et `System.Runtime.Loader` sont dans la BCL.
- Contenu :
  - `SandboxRequest` / `SandboxResult` — DTO du contrat (publics, sérialisés JSON).
  - `SandboxExecutor` — logique de chargement ALC + exécution io + exécution xunit + dispose,
    **déplacée** depuis `ProgramRunner`/`XunitRunner`. Appelable en proc (pour les tests unitaires).
  - `Program.Main(args)` → `return SandboxEntry.Run(args[0]);` (où `args[0]` = dossier de travail).
- Le bac à sable exécute le code recrue **synchroniquement** (pas de `Task.Run`/`Wait` interne) :
  le **timeout est la prérogative du parent** (attente + kill). Si le code recrue boucle, le parent
  tue.

### `Piscine.Grading`

- Ajoute un `ProjectReference` → `Piscine.Sandbox`. Double effet : (1) accès aux DTO du contrat ;
  (2) MSBuild **co-localise** `Piscine.Sandbox.dll` (et sa config) dans la sortie de **chaque**
  consommateur (front-ends + projet de tests).
- `ProgramRunner.Run` et `XunitRunner.Run` **conservent leur signature publique et leurs types de
  retour** (`RunOutcome`, `XunitRunner.RunResult`, `XunitRunner.References`) — les graders ne
  changent pas — mais leur **corps** devient : construire la requête, lancer le bac à sable,
  appliquer le timeout, tuer si besoin, parser le résultat.
- `RunOutcome.Error` passe de `Exception?` à un enregistrement `RunError(string TypeName, string Message)?`
  (une exception ne traverse pas une frontière de processus). Les 4 sites d'appel font déjà
  exactement `run.Error.GetType().Name` + `run.Error.Message` → renommage mécanique en
  `run.Error.TypeName` + `run.Error.Message` (IoGrader, ReseauGrader, ProjectGrader, TryCommand).

### Front-ends

**Aucune modification de code.** Le binaire `Piscine.Sandbox.dll` est co-localisé par le
`ProjectReference` transitif (front-end → Grading → Sandbox). L'exécutable dédié est une frontière
de sécurité plus nette que ré-instancier l'app principale, et c'est de toute façon la seule option
qui fonctionne sous `dotnet test`.

## Contrat IPC

Par exécution, un dossier temporaire unique `…/piscine-sbx/<guid>/` contient :

- `asm.dll` — octets de l'assembly émis par Roslyn (écrits sur disque plutôt qu'en base64 dans le
  JSON).
- `request.json` — la `SandboxRequest`.
- `result.json` — la `SandboxResult` (écrite par l'enfant).

Lancement : `Piscine.Sandbox <dossier>`. Le dossier est **supprimé** par le parent dans un `finally`.

**`SandboxRequest`**

| Champ | Type | Sens |
|---|---|---|
| `Mode` | `"io"` \| `"xunit"` | quel exécuteur invoquer |
| `Args` | `string[]` | arguments programme (io ; pour reseau : host/port en tête) |
| `Stdin` | `string` | stdin (io) |
| `ReferencePaths` | `string[]` | chemins d'assemblies à résoudre par l'ALC (xunit ; en général redondant — voir Résolution xunit) |

**`SandboxResult`**

| Champ | Type | Sens |
|---|---|---|
| `Stdout` | `string` | sortie capturée (io) |
| `ExitCode` | `int` | code de sortie recrue (retour de `Main`) (io) |
| `ErrorType` | `string?` | type de l'exception recrue non rattrapée (io) |
| `ErrorMessage` | `string?` | message de l'exception recrue (io) |
| `FactCount` | `int` | nombre de `[Fact]` trouvés (xunit) |
| `Failures` | `string[]` | échecs `Type.Méthode : message` (xunit) |
| `ExitedEarly` | `bool` | l'enfant est sorti via `Environment.Exit` (résultat partiel via hook `ProcessExit`) |

La sortie standard recrue est capturée **dans l'enfant** (son propre `Console.Out` → `StringWriter`)
puis sérialisée dans `result.json`. Le vrai stdout/stderr de l'enfant ne porte **pas** le protocole
(il reste libre pour le diagnostic d'un plantage du bac à sable lui-même) ; le parent le draine pour
éviter un blocage de pipe.

### Résolution xunit dans le bac à sable

L'assembly de test recrue est compilé contre `XunitRunner.References` (= xunit.core/assert 2.9.3 de
l'hôte). À l'exécution, l'ALC du bac à sable résout xunit en se repliant sur
`AssemblyLoadContext.Default` — qui contient xunit, puisque `Piscine.Sandbox` référence
xunit.core/assert 2.9.3 (même version). `ReferencePaths` fournit un gestionnaire `Resolving` de
secours, en général inutilisé.

## Sémantique timeout / kill / plantage

Parent : `process.WaitForExit(timeout)` ; au dépassement → `process.Kill(entireProcessTree: true)`
puis `WaitForExit()` → `TimedOut = true`. **Seul chemin qui termine réellement** une boucle infinie.

| Scénario | In-process (avant) | Processus enfant (après) |
|---|---|---|
| Timeout / boucle infinie | thread orphelin, ALC fuit, risque de corruption Console | **kill de l'arbre → tout récupéré, corruption impossible** |
| `Main` renvoie *N* | `ExitCode = N` | idem, via result.json |
| Exception recrue non rattrapée | `Error` renseigné | idem (`ErrorType`/`ErrorMessage`) |
| `Environment.Exit(n)` recrue | **tue la moulinette** | l'enfant sort ; hook `ProcessExit` écrit un result.json partiel (`ExitedEarly=true`) ; le parent lit le code de sortie du processus |
| StackOverflow / FailFast | **fait planter la moulinette** | l'enfant meurt ; result.json absent + sortie anormale → le parent rapporte une erreur d'exécution |
| Échecs de `[Fact]` | liste `Failures` | idem |
| Timeout xunit | thread orphelin | **kill de l'arbre → TimedOut** |
| Fixture `IDisposable` | **jamais disposée (fuite)** | **disposée dans un `finally`** |

Règle de lecture du résultat (parent) : si timeout → `TimedOut`. Sinon, si `result.json` existe → le
parser. Sinon (pas de result.json, pas de timeout) → erreur d'exécution « arrêt anormal » avec le
code de sortie du processus.

## Résolution du lanceur (risque principal)

Le binaire doit être trouvé en dev, sous `dotnet test`, et dans tous les front-ends packagés
(zip, installeurs, AppImage). Ordre de résolution :

1. **Variable d'env `PISCINE_SANDBOX`** — surcharge explicite (échappatoire + amorce de test).
2. **Apphost `Piscine.Sandbox(.exe)`** à côté de `AppContext.BaseDirectory` → lancé directement
   (auto-localisé, marche framework-dependent comme self-contained).
3. **`dotnet exec Piscine.Sandbox.dll`** à côté de `BaseDirectory` (repli si l'apphost n'a pas été
   copié), `dotnet` localisé via le runtime actif / `DOTNET_ROOT` / `PATH`.

Le `ProjectReference` transitif garantit la présence du binaire à côté de `BaseDirectory` (y compris
dans la sortie de test) → (2)/(3) fonctionnent sans surcharge. **À vérifier** sur les trois chemins
qui comptent : `dotnet test`, `dotnet run` (smoke E2E DevHost), et un `dotnet publish` de la CLI.
Une **assertion de co-localisation** (smoke) est ajoutée, à l'image des assertions de libs natives
déjà présentes en CI.

## Plan de tests (TDD)

Nouveaux fichiers dans `tests/Piscine.Grading.Tests` (qui référence aussi `Piscine.Sandbox` pour
co-localiser le binaire dans la sortie de test) :

1. **Acceptation — pas de contamination inter-exécutions** (`ProgramRunnerTests`) : une soumission à
   boucle infinie atteint le timeout (`TimedOut=true`), puis une exécution *ultérieure* d'un
   programme propre renvoie une sortie **correcte et non corrompue**. (Bug principal.)
2. **Acceptation — fixture disposée** (`XunitRunnerTests`) : une fixture `IDisposable` (et
   `IAsyncDisposable`) dont `Dispose` écrit dans un chemin de fichier temporaire *embarqué dans la
   source compilée* ; on assied que le fichier est écrit **même quand le `[Fact]` lève** (preuve du
   `finally`). Piloté via `SandboxExecutor` en proc pour un test serré, **et** de bout en bout via
   `UnitGrader`.
3. **Parité** : stdout, code de sortie (`return N`), report d'exception non rattrapée.
4. **Tous les tests de graders existants restent verts** (ils exercent désormais le chemin de
   lancement de processus de bout en bout) ; `validate-content` reste passant.

## Compromis accepté

Latence de démarrage de processus par exécution, **multipliée par *N*** dans `MutationGrader`
(un exercice à 10 mutants ⇒ ~12 lancements de bac à sable par correction). Acceptable pour de la
correction (correction > vitesse) ; pas d'optimisation R2R/AOT dans cette itération.

## Découpage de mise en œuvre (indicatif)

1. `Piscine.Sandbox` : projet + DTO + `SandboxExecutor` (io & xunit, dispose en `finally`) +
   `SandboxEntry`/`Program`. Ajout à `Piscine.slnx`.
2. `Piscine.Grading` : `RunError`, réécriture `ProgramRunner.Run`/`XunitRunner.Run` en clients de
   lancement + résolution du lanceur + fail-closed. `ProjectReference` → Sandbox.
3. Mise à jour des 4 sites `run.Error` (renommage `TypeName`).
4. Tests (acceptation + parité) ; `ProjectReference` Sandbox dans le projet de tests.
5. Vérification : tous les tests verts, `validate-content` passant, smoke de co-localisation
   (`dotnet test` + `dotnet run` DevHost + `dotnet publish` CLI).
