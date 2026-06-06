# Sprint 3 (V3) — Grader `projet` multi-fichiers (issue #4)

> Scrum / loop V3. Branche `feat/grader-projet`. Keystone V3 (fondation Blazor #7 / Clean Arch #5).

## Constat de design (investigation)
- `CompilationService.Compile` prend **déjà un dict de plusieurs fichiers** ; `IoGrader` compile déjà
  tous les `context.Sources` ensemble.
- `SubmissionLoader` lit les `deliverables` depuis `solution/` et **gère les sous-chemins**
  (`Path.Combine(dir, "Domain/Compte.cs")`). Donc un projet **multi-dossiers** marche déjà via des
  deliverables comme `Domain/Compte.cs`, `Application/Service.cs`, …
- ⇒ La vraie valeur d'un grader `projet` n'est PAS « compiler plusieurs fichiers » (déjà acquis) mais
  **les assertions d'architecture** : c'est le différenciateur pédagogique de Clean Architecture.

## Conception
Type `projet` = compilation multi-fichiers + (io optionnel) + **assertions d'architecture** (Roslyn).
Bloc manifest :
```yaml
grading:
  - type: projet
    cases: [ ... ]              # io optionnel (si présent → ConsoleApplication, sinon DLL)
    project:
      requires_types:           # types qui DOIVENT exister (nom pleinement qualifié / metadata)
        - Domain.Compte
        - Application.ICompteRepository
      forbidden_dependencies:   # interdiction de dépendance entre couches (namespaces)
        - from: Domain
          to: Infrastructure    # aucun type de Domain.* ne doit référencer Infrastructure.*
```
Classes : `ProjectAssertions { List<string> RequiresTypes; List<LayerRule> ForbiddenDependencies; }`,
`LayerRule { string From; string To; }` sur `GradingStep.Project`.

### `ProjectGrader : IGrader` (Type `projet`)
1. Compile `context.Sources` (Console si `cases`, sinon DLL). Échec compile → CompileError.
2. Si `cases` : exécute via `ProgramRunner` et compare stdout/exit (logique io réutilisée).
3. **requires_types** : `compilation.GetTypeByMetadataName(fqn)` ; absent → échec.
4. **forbidden_dependencies** : pour chaque arbre, via `SemanticModel`, pour chaque référence de type,
   namespace du type *déclarant* vs namespace du symbole *référencé* ; si `from.*` → `to.*` → violation.
5. Verdict unique éducatif ; trigger `project_structure` (ou io triggers pour les cas io).

### Câblage
- `Graders.Default()` ajoute `new ProjectGrader()`.
- `FeedbackTriggers.ProjectStructure = "project_structure"` (+ `All`).
- Aucun changement `SubmissionLoader`/`ContentValidator` (deliverables sous-chemins déjà gérés).

## Périmètre (re-scope scrum)
- **Sprint 3 = fondation moteur** : modèle + `ProjectGrader` + assertions + **tests unitaires** verts.
- **Suivi** : module **Clean Architecture #5** (contenu `projet`) ; harnais web Blazor #7.

## DoD (Sprint 3)
- [ ] Modèle `projet` (`ProjectAssertions`/`LayerRule`) Core
- [ ] `ProjectGrader` (compile multi-fichiers + io optionnel + requires_types + forbidden_dependencies)
- [ ] Trigger `project_structure`, enregistré dans `Graders.Default`
- [ ] Tests unitaires (compile OK, io, type requis manquant, dépendance interdite) verts
- [ ] `dotnet test Piscine.slnx -c Release` vert + `validate-content` vert
- [ ] Revue agent + docs + retex + PR mergée CI verte
