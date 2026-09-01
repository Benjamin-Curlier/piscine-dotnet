# Traçabilité des retours de réalisation

Ce document relie chaque retour de `RetoursPiscine.md` à une décision vérifiable. Il sert de
check-list de recette et évite qu'une suggestion soit appliquée sans confirmer sa cause.

| Retour | Décision | Correction et preuve attendue |
|---|---|---|
| `piscine init` crée `master` | Confirmé | HEAD du workspace et du dépôt bare initialisé sur `main` ; tests `GitWorkspaceTests`. |
| Reduce difficile à comprendre | Confirmé | Trace pas-à-pas de l'accumulateur et distinction seed/combiner. |
| Decorator confus / `TexteBrut` | Confirmé | `TexteSimple`, explication de l'enveloppement et exemple d'ordre. |
| `Split().Select()` échoue dans la Piscine | Cause confirmée, solution proposée rejetée | Le chaînage LINQ est valide ; garantir `System.Linq`. Ajouter `.ToList()` avant `Select` ne corrige pas une référence LINQ absente. |
| `switch`, `enum`, `static` arrivent tard | Confirmé | Aperçus précoces, plus carte de prérequis visible par module. |
| Ordre et formulation `const`/`readonly` | Confirmé | `static`, puis `readonly`, puis `const` ; tableau reformulé. |
| « nul » ambigu avec `null` | Confirmé | Employer « supérieur ou égal à zéro (`n >= 0`) ». |
| Base 2 trop simple et indice = solution | Confirmé | Conversion manuelle `% 2`/`/ 2`, indices progressifs et grader `source` interdisant `Convert.ToString`. |
| Taille `N` redondante pour les tris | Confirmé | Une seule ligne de valeurs, taille déduite de `Length`. |
| Pathfinding peu visuel | Confirmé | Comparaison visuelle BFS/Dijkstra/A* et rappel du rôle de `g`, `h`, files et priorités. |
| `'1'` vaut 49 | Confirmé | Rappel Unicode et conversion `c - '0'` dans le cours et A*. |
| Exercices d'unions trop similaires | Confirmé | `ex03-etat` devient une machine à transitions ; notation préfixe expliquée par un arbre. |
| Fichiers EF Core homonymes dans Rider | Confirmé | Noms uniques par exercice file-based. |
| Directives EF incompatibles avec `check` | Confirmé | Le loader retire `#!`/`#:` avant Roslyn ; le validateur vérifie includes et versions de paquets. |
| EF `Local` | Confirmé avec réserve | Indice ajouté avec explication de la portée avant `SaveChanges`. |
| Clean Architecture trop conceptuelle | Confirmé | Schéma des dépendances, composition root, namespaces, atelier guidé puis exercice autonome. |
| `Tache.Terminer()` et `using Domain` ambigus | Confirmé | Contrat `void`/idempotent et rôle exceptionnel de la composition root explicités. |
| Directives multi-fichiers Rider | Confirmé | Starters reliés par `#:include`, acceptés par `piscine check`. |
| Triangle Silk.NET sans socle | Confirmé | Module de lecture non noté ; pipeline VBO/VAO/shaders expliqué, triangle avancé explicitement optionnel. |
| Déplacement/redimensionnement/maximisation | Confirmé | Huit poignées, zone de déplacement explicite et maximisation native ; tests composants/E2E. |
| Bouton réduire invisible en sombre | Confirmé | Icône en `currentColor` et test de rendu. |
| Puces de navigation figées | Confirmé | Abonnement au watcher de progression et test E2E sans redémarrage. |
| Toast impossible à fermer | Confirmé | Hauteur bornée et contenu défilable. |
| Nommage et comparateurs absents | Confirmé | PascalCase/camelCase/_camelCase, `=`, `==`, `&&`, `||`, `!` introduits au module 01. |
| Exercices copiables depuis le cours | Confirmé | Audit `piscine audit-content`, exemples de cours différenciés et contraintes de source sur les algorithmes. |
| Tester sous un IDE | Confirmé | Procédure `dotnet run fichier.cs` et entrée PowerShell dès le module 00. |
| Exception sans cas de test | Confirmé | Entrée fautive affichée et `piscine check --replay-last`. |
| Difficulté et durée sous-estimées | Confirmé | 193 manifests (exercices + Rushes) déclarent difficulté, minutes, XP et compétences ; calibrage contrôlé par validation/audit. |
| Parcours sans suite logique | Confirmé | Fil rouge **Centre Asteria**, six actes, mission par module et jalon narratif par exercice ; carte dans `content/parcours.md`. |
| Manque de mini-projets transverses | Confirmé | Rush 5 (event processor), Rush 6 (Aspire/NATS/OTel) et Rush 7 (client lourd, services, données, sécurité et CI). |
| Manque d'accroche | Confirmé | XP à première réussite, niveaux progressifs, série, badges, profil CLI et panneau de gamification dans l'app, sans verrouillage. |
| Cours manquants sur les brokers | Confirmé | M40 compare NATS, RabbitMQ et ZeroMQ ; M41 approfondit NATS .NET/Core/JetStream. |
| Aspire et exploitation distribuée absents | Confirmé | M42 observabilité/résilience puis M43 Aspire 13, AppHost, discovery, Service Defaults et dashboard. |
| Contexte surtout applicatif lourd et distribué | Confirmé | M45 place MVVM, thread UI, dispatcher, annulation et latest-wins au centre ; le Rush 7 part du poste opérateur. |
| Web utile mais non central | Confirmé | M46 enseigne ASP.NET Core/OpenAPI comme façade consommée par le desktop ; M49 ajoute gRPC/Protobuf pour les échanges typés. |
| Chaînon entreprise incomplet | Confirmé | M44–M52 couvrent socle projet, identité, données de production, tests d'intégration, CI/CD, release, diagnostic, cache et budgets. |
| Modules de plateforme sans pratique | Confirmé | Exercices déterministes `fichier` pour interop, Docker, shaders Silk.NET, composants Blazor et AppHost, complétés par ateliers réels. |
| Tests unitaires trop simulés | Confirmé | Les quatre exercices M13 demandent de vrais tests xUnit ; les trois premiers sont confrontés à des mutants cachés progressifs. |
| Decorator enseigné deux fois | Confirmé | M30 remplace le doublon par Chain of Responsibility ; Decorator reste concentré en M23. |
| Starters qui réussissent sans travail | Confirmé | Contraintes `source` ajoutées aux refactorings et aux exercices techniques ; occurrences minimales et audit starter/indice. |

## Recette de référence

```powershell
dotnet restore Piscine.slnx
dotnet list Piscine.slnx package --vulnerable --include-transitive --no-restore
dotnet build Piscine.slnx -c Release --no-restore
dotnet test --solution Piscine.slnx -c Release --no-build --no-restore
$env:PISCINE_CONTENT = "$PWD\content"
$env:PISCINE_SANDBOX = "$PWD\src\Piscine.Sandbox\bin\Release\net10.0\Piscine.Sandbox.dll"
dotnet run --project src/Piscine.Cli -c Release --no-build -- validate-content
dotnet run --project src/Piscine.Cli -c Release --no-build -- audit-content
dotnet run --project src/Piscine.Cli -c Release --no-build -- doctor
git diff --check
```

`validate-content` est bloquant. `audit-content` est consultatif : chaque alerte demande une décision
humaine, mais l'outil ne modifie jamais le contenu.

## Résultat de la recette du 1er septembre 2026

- Restauration et compilation Release : réussies, sans avertissement ni erreur.
- Tests : 519 réussis, aucun échec (`Core` 59, `Components` 78, `App` 141,
  `Git` 21, `Grading` 198, `DevHost.E2E` 22).
- Validation des 193 activités : `Contenu valide.`
- Audit pédagogique déterministe : `Aucune alerte pédagogique déterministe.`
- Audit NuGet direct et transitif : aucun paquet vulnérable connu et aucune mise à jour disponible
  dans les sources configurées.
- Recette apprenant isolée : initialisation sur `main`, diagnostic de 53 modules, profil XP,
  catalogue desktop/MVVM et Rush 7, démarrage d'un exercice, refus attendu du starter et scaffold
  complet. Le pipeline caché `.gitlab-ci.yml` et le runbook du Rush 7 sont bien installés.
- Qualité du dépôt : `dotnet format --verify-no-changes` global et `git diff --check` réussis.
