# Fonctionnement de la moulinette

La moulinette est le moteur d'auto-correction. Elle est **100 % locale et in-process** : pas de
serveur, pas de SDK requis. Elle compile le code de la recrue avec **Roslyn embarqué** et exécute
les graders dans un contexte isolé.

## Compilation Roslyn (zéro SDK)

- Le code C# de la recrue est compilé en mémoire via **Roslyn** (`Microsoft.CodeAnalysis.CSharp`),
  embarqué dans le binaire self-contained.
- Les références de base (.NET) sont résolues depuis les assemblies réellement présentes à côté du
  binaire (`TRUSTED_PLATFORM_ASSEMBLIES`) — d'où le choix d'une distribution **dossier
  self-contained** (et non single-file, qui casserait cette résolution).
- Les erreurs/avertissements de compilation sont transformés en **feedback éducatif** (numéro de
  ligne + message).

## Les types de grader

Un exercice combine un ou plusieurs graders, déclarés dans son `manifest.yaml` :

| Type | Ce qu'il fait | Bloquant ? |
|---|---|---|
| **`io`** | Compile en exécutable, lance dans un `AssemblyLoadContext` isolé (args/stdin injectés, timeout), **compare stdout/exit** au résultat attendu → diff éducatif. | Oui |
| **`unit`** | Compile le code recrue **+ des tests xUnit cachés** (`grader/`), exécute les `[Fact]` par réflexion dans un contexte isolé, récupère les messages d'assertion. | Oui |
| **`norme`** | Diagnostics de style **Roslyn** (Formatter + `.editorconfig`). Souple. | **Non** (avertissement) |
| **`mutation`** | La recrue **écrit ses propres tests** xUnit, confrontés à une impl. de référence cachée + des **mutants** nommés ; verdict binaire (tous les mutants tués). | Oui |
| **`git`** | Verdict sur l'**état attendu du dépôt rendu** (branches, `min_commits`, fusions, contenu de fichiers, absence de marqueurs de conflit), via LibGit2Sharp. Au push, noté contre le **dépôt bare** si l'exo est « tenté ». | Oui |
| **`projet`** | Compilation **multi-fichiers** + cas `io` optionnels + **assertions d'architecture** Roslyn (`requires_types`, `forbidden_dependencies` namespace→namespace). | Oui |
| **`reseau`** | Lance un **harnais d'écho TCP** loopback, injecte host/port en arguments, compare `io`. | Oui |

Chaque exécution C# se fait dans un `AssemblyLoadContext` **collectible** avec redirection de la
Console et un **timeout** — un programme qui boucle ou plante n'affecte pas la moulinette.

> Tous les modules ne sont pas auto-notés : ceux dont la sortie n'est pas déterministe en console
> (Docker, Silk.NET, Blazor, interop, git avancé, réseau brut) sont livrés en **lecture guidée**.

## Correction par groupe : arrêt au premier échec

Les exercices d'un groupe (`module.yaml`) sont **ordonnés**. La correction est **séquentielle** :

1. On corrige le 1er exercice du groupe.
2. S'il est **À revoir**, on **s'arrête** : tous les exercices suivants du groupe passent en
   **Non corrigé** (on ne les note pas).
3. Sinon on continue au suivant.

C'est le comportement de la trace 42 : on règle un exercice avant de débloquer la suite.

## Statuts & progression

- Trois statuts : **Réussi**, **À revoir**, **Non corrigé** — *jamais* de note chiffrée.
- La progression (statut par exercice, tentatives, dernier feedback) est persistée dans l'état
  local (`~/piscine`, surchargeable via `PISCINE_HOME`). Les *Non corrigé* ne sont pas enregistrés
  comme un échec définitif : ils seront corrigés une fois le blocage levé.

## Garde-fou qualité (CI)

`piscine validate-content` vérifie pour **chaque** exercice : manifest valide, fichiers de graders
présents, et surtout que le **corrigé `solution/` passe ses propres graders**. La CI exécute la même
commande → impossible de livrer un exercice cassé. Voir [Ajouter un exercice](Ajouter-un-exercice).
