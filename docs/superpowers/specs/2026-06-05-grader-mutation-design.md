# Design — Grader « élève-écrit-tests » (type `mutation`)

> Chantier moteur **Point 6** de `BLOCKERS-v1.0.md`. Valorise M13 (« écrire des tests qui
> attrapent les bugs ») et fournit une base réutilisable. Date : 2026-06-05.
> Lis aussi `docs/superpowers/HANDOFF.md` (état moteur) et `BLOCKERS-v1.0.md` (branche
> `v1.0-blockers`, contexte des chantiers restants).

## 1. Intention

Nouveau type de grading **`mutation`**. La recrue **livre une suite de tests xUnit** ; le moteur
fournit une **implémentation de référence cachée** + des **mutants** (implémentations boguées
dérivées de la référence par patch textuel). Le verdict est **binaire** :

1. les tests **compilent** contre l'API de référence,
2. ils sont **tous verts** sur l'implémentation correcte,
3. **chaque mutant** est **tué** par au moins un test rouge.

Un mutant survivant révèle son **label** (description du cas manquant). Aucun changement aux graders
`io` / `unit` / `norme` existants. Cohérent avec l'esprit de la piscine : **retour éducatif, jamais
de note**, verdict net « il te manque un cas ».

Distinction avec le grader `unit` existant : `unit` exécute des **tests cachés contre le code de la
recrue** ; `mutation` **inverse le schéma** — la recrue écrit les tests, le moteur fournit le code
(correct + bogué).

## 2. Schéma manifest

```yaml
deliverables: [BankAccountTests.cs]              # SEUL livrable = les tests de la recrue
starter: [BankAccount.cs, BankAccountTests.cs]   # stub d'API compilable + amorce de tests
grading:
  - type: mutation
    reference: reference/BankAccount.cs          # impl correcte cachée (dans le dossier content)
    mutants:
      - id: retrait-egal-solde
        label: "Le retrait d'un montant exactement égal au solde n'est pas couvert."
        find: "amount > balance"
        replace: "amount >= balance"
      - id: depot-negatif
        label: "Un dépôt négatif devrait être refusé."
        find: "if (amount < 0) throw"
        replace: "if (amount < -1) throw"
solution: [BankAccountTests.cs]                  # suite de tests MODÈLE de l'auteur (= corrigé)
```

- **`reference`** : chemin (relatif au dossier *content* de l'exercice) vers l'impl correcte cachée.
  Jamais livrée à la recrue, jamais dans le `starter`.
- **`mutants[]`** : liste de `{ id, label, find, replace }`.
  - `id` : identifiant court (diagnostics auteur).
  - `label` : phrase pédagogique affichée à la recrue si le mutant survit (décrit le **cas manquant**,
    pas le code muté).
  - `find` / `replace` : remplacement de chaîne appliqué à la source de référence. **`find` doit
    matcher exactement une fois** la référence **et** la modifier réellement (`find != replace`),
    sinon c'est une **erreur de contenu** (captée par la gate).
- **`starter`** : `BankAccount.cs` = **stub compilable** (signatures publiques, corps
  `=> throw new NotImplementedException()`) pour que les tests de la recrue **compilent localement** ;
  `BankAccountTests.cs` = amorce de tests. À la correction, le stub livré est **ignoré** (ce n'est pas
  un `deliverable`) ; le grader injecte la référence.
- **`solution`** : pointe vers la **suite de tests modèle** de l'auteur. C'est le « corrigé » exécuté
  par la gate `validate-content` ; il doit passer ses propres graders (donc tuer tous les mutants).

### Convention de répertoire

```
content/modules/13-tests-unitaires/exercises/ex03-mutation/
  manifest.yaml
  subject.md
  starter/
    BankAccount.cs            # stub NotImplementedException
    BankAccountTests.cs       # amorce
  reference/
    BankAccount.cs            # impl correcte CACHÉE
  solution/
    BankAccountTests.cs       # suite modèle de l'auteur (tue tous les mutants)
```

## 3. Modèle (`Piscine.Core`)

`GradingStep` (`src/Piscine.Core/Model/GradingStep.cs`) gagne deux champs :

```csharp
/// <summary>Pour le grader mutation : impl de référence cachée (chemin relatif au dossier content).</summary>
public string Reference { get; set; } = string.Empty;

/// <summary>Pour le grader mutation : mutations à dériver de la référence.</summary>
public List<Mutant> Mutants { get; set; } = new();
```

Nouveau type :

```csharp
/// <summary>Une mutation : un remplacement textuel nommé appliqué à l'impl de référence.</summary>
public sealed class Mutant
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Find { get; set; } = string.Empty;
    public string Replace { get; set; } = string.Empty;
}
```

Le `YamlLoader` / `ExerciseManifestLoader` mappe `reference` et `mutants` (avec `id`, `label`,
`find`, `replace`). Vérifier que le loader maison gère bien une liste d'objets imbriqués (déjà fait
pour `cases` / `hints`).

## 4. Chargement (`SubmissionLoader`)

En plus des `step.TestFiles`, charger le contenu de `step.Reference` dans `GraderFiles` (fichier
caché, comme les tests cachés du grader `unit`). Les patchs (`Mutants`) sont déjà portés par `step`
(le manifest), donc disponibles dans `Grade(context, step)`. Aucune nouvelle plomberie de chemins :

```csharp
foreach (var step in manifest.Grading)
{
    foreach (var testFile in step.TestFiles) { /* … existant … */ }

    if (!string.IsNullOrEmpty(step.Reference))
    {
        var path = Path.Combine(exerciseContentDir, step.Reference);
        if (File.Exists(path)) graderFiles[step.Reference] = File.ReadAllText(path);
    }
}
```

## 5. Grader (`MutationGrader : IGrader`)

`Type => "mutation"`. Réutilise `CompilationService` et la **découverte/exécution des `[Fact]`** de
`UnitGrader`. Pour éviter la duplication, extraire un helper partagé (ex. `XunitRunner`) exposant :
- `XunitReferences` (chemins des assemblies xUnit),
- `IReadOnlyList<MethodInfo> FindFactMethods(Assembly)`,
- `IReadOnlyList<string> RunFacts(byte[] assemblyBytes, TimeSpan timeout)` → liste des échecs
  (vide = tous verts ; une entrée spéciale ou un type de retour distingue le timeout).

`UnitGrader` est refactoré pour consommer ce helper (comportement inchangé, couvert par ses tests
existants).

### Algorithme `Grade(GradingContext context, GradingStep step)`

```
ref ← context.GraderFiles[step.Reference]
(si absent → échec CONTENU : "implémentation de référence introuvable")

# Passe 1 : la référence
compil ← Compile(context.Sources + {refFileName: ref}, DLL, xunitRefs)
si compil KO → GraderResult.Failure(CompileError, "tes tests ne compilent pas contre l'API")
facts ← FindFactMethods(asm)
si facts vide → Failure("aucun test trouvé")
échecs ← RunFacts(asm, 10s)
si échecs non vide → Failure(TestsFailOnReference,
                            "tes tests échouent sur l'implémentation correcte : …")

# Passe 2..N : les mutants
survivants ← []
pour chaque mutant dans step.Mutants :
    (src, n) ← ApplyPatch(ref, mutant.Find, mutant.Replace)
    si n ≠ 1 ou src == ref → échec CONTENU
        ("mutant {id} : find ne matche pas exactement une fois / ne change rien")
    compilM ← Compile(context.Sources + {refFileName: src}, DLL, xunitRefs)
    si compilM KO → échec CONTENU ("mutant {id} ne compile pas")
    échecsM ← RunFacts(asmM, 10s)
    si échecsM vide → survivants.Add(mutant.Label)   # le mutant a survécu

si survivants non vide → Failure(MutantSurvived, [labels…])
sinon → Success
```

- **Timeout** : 10 s par passe (comme `UnitGrader`). N+1 passes de compilation au total.
- **Erreurs de contenu** : un `find` qui n'applique pas, un mutant qui ne compile pas — ce sont des
  défauts d'**auteur**, pas de recrue. Le grader renvoie un `GraderResult.Failure` au message explicite
  (préfixé « contenu : ») ; la gate les remonte (cf. §7). En correction réelle d'une recrue, ces cas
  ne peuvent pas survenir si la gate est passée.
- **`ApplyPatch`** : `string.Replace` ne suffit pas pour compter les occurrences → compter via
  `IndexOf` en boucle (ou `Regex.Matches` sur `Regex.Escape(find)`), exiger exactement 1, puis
  remplacer cette occurrence.

## 6. Feedback

Deux nouveaux triggers dans `FeedbackTriggers` (`src/Piscine.Core/Model/FeedbackTriggers.cs`), ajoutés
à `All` :

```csharp
/// <summary>Les tests de la recrue échouent sur l'implémentation correcte (grader mutation).</summary>
public const string TestsFailOnReference = "tests_fail_on_reference";

/// <summary>Un mutant a survécu : un cas bogué n'est pas détecté (grader mutation).</summary>
public const string MutantSurvived = "mutant_survived";
```

Réutilise `CompileError` pour la non-compilation des tests. Exemple de rendu pour un mutant survivant :

> ⚠️ Un comportement bogué n'est pas détecté par tes tests :
> *« Le retrait d'un montant exactement égal au solde n'est pas couvert. »*
> Ajoute un test qui couvre ce cas.

Le `ResultFormatter` mappe ces triggers vers les hints du manifest (mécanique existante). Plusieurs
survivants → un message par label.

## 7. Validation de contenu (gate) — gratuite

`ContentValidator` exécute déjà le corrigé via ses graders (`ContentValidator.cs:121`). Comme
**`solution` = la suite de tests modèle**, la gate vérifie automatiquement, sans contrôle supplémentaire :

- le modèle **compile** contre la référence,
- il est **vert** sur la référence,
- il **tue tous les mutants**.

Si un patch n'applique pas ou un mutant ne compile pas, `MutationGrader` renvoie un échec → la gate le
remonte comme « le corrigé ne passe pas ses graders ». **Aucune logique gate à écrire.** (Vérifier
seulement que `SubmissionLoader` charge bien la référence quand on valide depuis `solution/`.)

## 8. Enregistrement

`Graders.Default()` (`src/Piscine.Grading/Graders.cs`) : ajouter `new MutationGrader(...)` à la liste.

## 9. Tests (`tests/Piscine.Grading.Tests`)

Unitaires `MutationGrader` (sources en mémoire, pas de disque) :
- `ApplyPatch` : 1 occurrence (OK), 0 occurrence (erreur contenu), 2 occurrences (erreur contenu),
  `find == replace` (erreur contenu).
- Tests recrue qui ne compilent pas → `compile_error`.
- Tests rouges sur la référence → `tests_fail_on_reference`.
- Suite faible (assert trivial) → mutant survit → `mutant_survived` + label exact.
- Suite complète → tous les mutants tués → succès.
- Mutant qui ne compile pas → erreur contenu au message explicite.

Intégration :
- `XunitRunner` partagé : `UnitGrader` reste vert (régression).
- `ContentValidator` : un exo `mutation` bien formé passe ; un exo avec un mutant non tué par la
  `solution` est rejeté.

Rappels environnement (cf. HANDOFF « Pièges connus ») : `Piscine.Grading.Tests` désactive la
parallélisation xUnit (Console global / ALC) ; lancer via `dotnet test Piscine.slnx -c Release`.

## 10. Contenu pilote — M13

Ajouter **`ex03-mutation`** au module `13-tests-unitaires` (non destructif : ex00/ex01/ex02 restent en
`io`, progression douce io → mutation). Sujet : un classeur de nombres ou un mini `BankAccount`, avec
des mutants sur les bornes (`> 0` vs `>= 0`, refus d'un montant négatif, etc.).

- Écrire `reference/<Impl>.cs` (correct) **d'abord**, puis la `solution/<Impl>Tests.cs` modèle, puis
  les `mutants` dans le manifest ; itérer avec `validate-content` jusqu'à ce que **tous les mutants
  soient tués** par le modèle.
- Starter : stub `NotImplementedException` + amorce de tests.
- Ajouter `ex03-mutation` à la fin de la liste `exercises:` du `module.yaml`.
- MAJ `cours.md` (section « écrire des tests qui attrapent les bugs / pourquoi un test qui ne casse
  jamais ne sert à rien ») et `docs/wiki/Curriculum.md`.

## 11. Hors périmètre (YAGNI)

- Pas de **score de mutation** ni de seuil : verdict **binaire** (tous les mutants tués).
- Pas de **génération automatique** de mutants (Roslyn) : patchs `find/replace` écrits par l'auteur.
- Pas de support `try` spécifique : la boucle auteur passe par `validate-content` / `check`, qui
  exécutent déjà le grader sur la `solution` et rapportent les survivants. (Améliorations d'affichage
  auteur possibles plus tard, hors de ce chantier.)
- Pas de **patchs multi-lignes** ni diff unifié : `find/replace` mono-occurrence suffit pour les
  mutations pédagogiques visées.

## 12. Récap effort

~2 j. Gros morceau : `MutationGrader` + extraction `XunitRunner`. Le reste est du câblage
modèle / loader / triggers / enregistrement, plus l'exercice pilote M13.
