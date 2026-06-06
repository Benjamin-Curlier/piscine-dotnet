# Sprint 7 (V3) — Harnais réseau / grader `reseau` (issue #3)

> Scrum / loop V3. Branche `feat/grader-reseau`. Fondation moteur (façon grader git/projet).

## Objectif
Permettre des exercices **réseau déterministes** : un **serveur de test embarqué** (écho TCP sur
loopback, port éphémère) est démarré par le grader ; le programme de la recrue reçoit `host`/`port`
en **arguments** et dialogue avec ce serveur ; la sortie standard est comparée de façon déterministe.
Transforme M22 de *lecture* en auto-noté (contenu en suivi).

## Conception
- **`NetworkHarness`** (IDisposable) : `StartEcho()` ouvre un `TcpListener` sur `127.0.0.1:0`,
  expose `Host`/`Port`, accepte les connexions et **renvoie chaque ligne reçue** (écho, `\n`).
- **`ReseauGrader`** (type `reseau`) : compile (Console) ; pour chaque cas, démarre un harnais,
  exécute le programme via `ProgramRunner` avec `args = [host, port, ...case.Args]`, compare
  stdout/exit (logique io), puis arrête le harnais. Réutilise les triggers io.
- **Modèle** : `GradingStep.Network` = `NetworkConfig { Mode = "echo" }` (extensible ; mode inconnu →
  erreur de contenu). `echo` par défaut si absent.
- `Graders.Default()` enregistre `new ReseauGrader()`.

### Convention recrue
`args[0]` = hôte, `args[1]` = port du serveur de test. Le reste = `args` du cas.

## Périmètre (re-scope scrum)
- **Sprint 7 = fondation moteur** : `NetworkHarness` (écho TCP) + `ReseauGrader` + tests unitaires.
- **Suivi** : serveur **HTTP** (`HttpListener`) + exo pilote **M22** + bascule lecture→auto-noté.

## DoD (Sprint 7)
- [ ] `NetworkHarness` (écho TCP loopback, port éphémère)
- [ ] `ReseauGrader` (type `reseau`, injecte host/port en args, compare io)
- [ ] Modèle `NetworkConfig` ; enregistré dans `Graders.Default`
- [ ] Tests unitaires (écho OK, multi-lignes, compile KO, mismatch) verts
- [ ] `dotnet test Piscine.slnx -c Release` vert + `validate-content` vert
- [ ] Revue agent + docs + retex + PR mergée CI verte
