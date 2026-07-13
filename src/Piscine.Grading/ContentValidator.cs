using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Un problème détecté dans le contenu pédagogique.</summary>
public sealed record ContentIssue(string ExerciseId, string Message);

/// <summary>Rapport de validation du contenu.</summary>
public sealed class ContentValidationReport
{
    public ContentValidationReport(IReadOnlyList<ContentIssue> issues) => Issues = issues;

    public IReadOnlyList<ContentIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Garde-fou qualité : vérifie que chaque exercice référencé a un manifest valide,
/// des fichiers de graders présents, et un corrigé <c>solution/</c> qui passe ses
/// propres graders. (spec §4.5)
/// </summary>
public sealed class ContentValidator
{
    public const string SolutionDirName = "solution";

    /// <summary>Valeurs autorisées pour <c>difficulty</c> (cf. <see cref="ExerciseManifest.Difficulty"/>).</summary>
    public static readonly string[] ValidDifficulties = { "facile", "moyen", "difficile" };

    private const string CourseFileName = "cours.md";

    private readonly ExerciseGrader _grader;

    public ContentValidator(ExerciseGrader grader) => _grader = grader;

    public ContentValidationReport Validate(PiscineLayout layout)
    {
        var issues = new List<ContentIssue>();

        // DiscoverModules/Rushes est résilient (ignore les yaml malformés pour ne pas casser l'UX recrue) :
        // on doit donc les signaler ICI, sinon un module.yaml/manifest de rush cassé passerait la gate
        // (fail-open). On charge chaque fichier explicitement et on rapporte tout échec.
        ValidateManifestsLoadable(layout, issues);

        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in ContentDiscovery.DiscoverModules(layout.Content))
        {
            foreach (var exerciseId in module.Groups.SelectMany(g => g.Exercises))
            {
                referenced.Add(exerciseId);
                ValidateExercise(layout, exerciseId, issues);
            }
        }

        // Les Rushes sont des livrables autonomes : on valide aussi leur corrigé.
        foreach (var rush in ContentDiscovery.DiscoverRushes(layout.Content))
        {
            ValidateExercise(layout, rush.Id, issues);
        }

        ValidateNoOrphans(layout, referenced, issues);
        ValidateNoDuplicateIds(layout, issues);

        return new ContentValidationReport(issues);
    }

    private void ValidateExercise(PiscineLayout layout, string exerciseId, List<ContentIssue> issues)
    {
        var location = ContentLocator.FindExercise(layout.Content, exerciseId);
        if (location is null)
        {
            issues.Add(new ContentIssue(exerciseId, "référencé dans un groupe mais introuvable dans le contenu."));
            return;
        }

        ExerciseManifest manifest;
        try
        {
            manifest = ExerciseManifestLoader.Load(location.ContentDir);
        }
        catch (Exception e)
        {
            issues.Add(new ContentIssue(exerciseId, $"manifest.yaml invalide : {e.Message}"));
            return;
        }

        // Passe stricte : une clé inconnue (typo, ex. « expext_stdout ») est silencieusement ignorée au
        // runtime (lenient) mais fausserait la notation — on la signale ici comme problème de contenu.
        var unknownKey = ExerciseManifestLoader.FindUnknownKey(location.ContentDir);
        if (unknownKey is not null)
        {
            issues.Add(new ContentIssue(exerciseId, $"clé non reconnue dans {unknownKey}"));
        }

        foreach (var testFile in manifest.Grading.SelectMany(s => s.TestFiles))
        {
            if (!File.Exists(Path.Combine(location.ContentDir, testFile)))
            {
                issues.Add(new ContentIssue(exerciseId, $"fichier de grader manquant : {testFile}"));
            }
        }

        if (!File.Exists(Path.Combine(location.ContentDir, "subject.md")))
        {
            issues.Add(new ContentIssue(exerciseId, "subject.md manquant (énoncé affiché à la recrue)."));
        }

        ValidateDifficulty(exerciseId, manifest, issues);
        ValidateStarterFiles(exerciseId, location, manifest, issues);
        ValidateCourseRef(exerciseId, location, manifest, issues);
        ValidateHints(exerciseId, manifest, issues);

        // Un exercice `git` n'a pas de dossier solution/ : son « corrigé » est une fixture de dépôt
        // que l'on construit puis confronte aux assertions du grader.
        if (manifest.Grading.Any(s => s.Type == "git"))
        {
            ValidateGitExercise(exerciseId, manifest, issues);
            return;
        }

        var solutionDir = Path.Combine(location.ContentDir, SolutionDirName);
        if (!Directory.Exists(solutionDir))
        {
            issues.Add(new ContentIssue(exerciseId, "dossier solution/ manquant (corrigé de référence requis)."));
            return;
        }

        var submission = SubmissionLoader.Load(location.ContentDir, solutionDir);
        foreach (var deliverable in manifest.Deliverables)
        {
            if (!submission.Context.Sources.ContainsKey(deliverable))
            {
                issues.Add(new ContentIssue(exerciseId, $"corrigé manquant pour le livrable : {deliverable}"));
            }
        }

        var result = _grader.Grade(submission.Manifest, submission.Context);
        if (result.Status != GraderStatus.Reussi)
        {
            var detail = string.Join(" ; ", result.Results.SelectMany(r => r.Messages));
            issues.Add(new ContentIssue(exerciseId, $"le corrigé ne passe pas ses graders : {detail}"));
        }
    }

    /// <summary>Valide un exercice <c>git</c> : construit sa fixture de dépôt et la confronte aux assertions.</summary>
    private void ValidateGitExercise(string exerciseId, ExerciseManifest manifest, List<ContentIssue> issues)
    {
        if (manifest.Grading.Any(s => s.Type != "git"))
        {
            issues.Add(new ContentIssue(exerciseId, "un exercice git ne peut pas combiner d'autres types de notation (pas de dossier solution/ à compiler)."));
            return;
        }

        foreach (var step in manifest.Grading.Where(s => s.Type == "git"))
        {
            if (step.Git is null || step.Git.Fixture.Count == 0)
            {
                issues.Add(new ContentIssue(exerciseId, "étape git sans fixture : impossible de valider le corrigé."));
                continue;
            }

            var dir = Path.Combine(Path.GetTempPath(), "piscine-fixture", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                GitFixtureBuilder.Build(step.Git.Fixture, dir);
                var context = new GradingContext(new Dictionary<string, string>(), repositoryPath: dir);
                var result = _grader.Grade(manifest, context);
                if (result.Status != GraderStatus.Reussi)
                {
                    var detail = string.Join(" ; ", result.Results.SelectMany(r => r.Messages));
                    issues.Add(new ContentIssue(exerciseId, $"la fixture git ne passe pas les assertions : {detail}"));
                }

                // Notation live (#17) : si un signal « attempt » est déclaré, le corrigé (la fixture)
                // doit le satisfaire — sinon le vrai rendu ne serait jamais reconnu comme « tenté ».
                if (step.Git.Attempt is { } attempt && !GitAttemptEvaluator.IsAttempted(attempt, dir))
                {
                    issues.Add(new ContentIssue(exerciseId,
                        "le signal « attempt » n'est pas satisfait par la fixture : le corrigé devrait être reconnu comme « tenté »."));
                }
            }
            catch (Exception e)
            {
                issues.Add(new ContentIssue(exerciseId, $"construction de la fixture git échouée : {e.Message}"));
            }
            finally
            {
                DeleteRepo(dir);
            }
        }
    }

    /// <summary>Supprime un dépôt temporaire (les packs git sont en lecture seule sous Windows).</summary>
    private static void DeleteRepo(string dir)
    {
        try
        {
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(dir, recursive: true);
        }
        catch (Exception)
        {
            // nettoyage best-effort
        }
    }

    /// <summary>
    /// Charge explicitement chaque <c>module.yaml</c> et manifest de rush : un fichier malformé (que la
    /// découverte résiliente ignore) est ainsi rapporté comme problème de contenu au lieu d'être avalé.
    /// </summary>
    private static void ValidateManifestsLoadable(PiscineLayout layout, List<ContentIssue> issues)
    {
        var modulesDir = layout.Content.ModulesDirectory;
        if (Directory.Exists(modulesDir))
        {
            foreach (var moduleDir in Directory.EnumerateDirectories(modulesDir))
            {
                if (!File.Exists(Path.Combine(moduleDir, ModuleLoader.FileName)))
                {
                    continue;
                }

                try
                {
                    ModuleLoader.Load(moduleDir);
                }
                catch (Exception e)
                {
                    issues.Add(new ContentIssue(Path.GetFileName(moduleDir), $"{ModuleLoader.FileName} invalide : {e.Message}"));
                }
            }
        }

        var rushesDir = layout.Content.RushesDirectory;
        if (Directory.Exists(rushesDir))
        {
            foreach (var rushDir in Directory.EnumerateDirectories(rushesDir))
            {
                if (!File.Exists(Path.Combine(rushDir, ExerciseManifestLoader.FileName)))
                {
                    continue;
                }

                try
                {
                    ExerciseManifestLoader.Load(rushDir);
                }
                catch (Exception e)
                {
                    issues.Add(new ContentIssue(Path.GetFileName(rushDir), $"{ExerciseManifestLoader.FileName} invalide : {e.Message}"));
                }
            }
        }
    }

    /// <summary>Signale les exercices présents sur disque mais référencés dans aucun groupe (orphelins).</summary>
    private static void ValidateNoOrphans(PiscineLayout layout, HashSet<string> referenced, List<ContentIssue> issues)
    {
        var modulesDir = layout.Content.ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return;
        }

        foreach (var moduleDir in Directory.EnumerateDirectories(modulesDir))
        {
            var exercisesDir = Path.Combine(moduleDir, ContentLocator.ExercisesDirName);
            if (!Directory.Exists(exercisesDir))
            {
                continue;
            }

            foreach (var exerciseDir in Directory.EnumerateDirectories(exercisesDir))
            {
                if (!File.Exists(Path.Combine(exerciseDir, ExerciseManifestLoader.FileName)))
                {
                    continue;
                }

                var id = Path.GetFileName(exerciseDir);
                if (!referenced.Contains(id))
                {
                    issues.Add(new ContentIssue(id, $"exercice présent sur disque ({Path.GetFileName(moduleDir)}) mais référencé dans aucun groupe (orphelin)."));
                }
            }
        }
    }

    /// <summary>
    /// Signale les identifiants d'exercice présents dans plusieurs modules : <see cref="ContentLocator.FindExercise"/>
    /// renvoie le premier trouvé, donc un doublon masque silencieusement l'autre.
    /// </summary>
    private static void ValidateNoDuplicateIds(PiscineLayout layout, List<ContentIssue> issues)
    {
        var modulesDir = layout.Content.ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return;
        }

        var modulesById = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var moduleDir in Directory.EnumerateDirectories(modulesDir))
        {
            var exercisesDir = Path.Combine(moduleDir, ContentLocator.ExercisesDirName);
            if (!Directory.Exists(exercisesDir))
            {
                continue;
            }

            foreach (var exerciseDir in Directory.EnumerateDirectories(exercisesDir))
            {
                if (!File.Exists(Path.Combine(exerciseDir, ExerciseManifestLoader.FileName)))
                {
                    continue;
                }

                var id = Path.GetFileName(exerciseDir);
                if (!modulesById.TryGetValue(id, out var list))
                {
                    modulesById[id] = list = new List<string>();
                }

                list.Add(Path.GetFileName(moduleDir));
            }
        }

        foreach (var (id, modules) in modulesById)
        {
            if (modules.Count > 1)
            {
                issues.Add(new ContentIssue(id, $"identifiant d'exercice présent dans plusieurs modules ({string.Join(", ", modules)}) : ambigu pour la résolution."));
            }
        }
    }

    private static void ValidateDifficulty(string exerciseId, ExerciseManifest manifest, List<ContentIssue> issues)
    {
        if (!ValidDifficulties.Contains(manifest.Difficulty))
        {
            issues.Add(new ContentIssue(
                exerciseId,
                $"difficulty invalide : « {manifest.Difficulty} » (attendu : {string.Join(" | ", ValidDifficulties)})."));
        }
    }

    private static void ValidateHints(string exerciseId, ExerciseManifest manifest, List<ContentIssue> issues)
    {
        foreach (var hint in manifest.Feedback.Hints)
        {
            if (!FeedbackTriggers.All.Contains(hint.When))
            {
                issues.Add(new ContentIssue(
                    exerciseId,
                    $"hint when invalide : « {hint.When} » (attendu : {string.Join(" | ", FeedbackTriggers.All)})."));
            }
        }
    }

    private static void ValidateStarterFiles(string exerciseId, ExerciseLocation location, ExerciseManifest manifest, List<ContentIssue> issues)
    {
        foreach (var starter in manifest.Starter)
        {
            var path = Path.Combine(location.ContentDir, StarterInstaller.StarterDirName, starter);
            if (!File.Exists(path))
            {
                issues.Add(new ContentIssue(exerciseId, $"fichier starter déclaré mais manquant : {StarterInstaller.StarterDirName}/{starter}"));
            }
        }
    }

    private static void ValidateCourseRef(string exerciseId, ExerciseLocation location, ExerciseManifest manifest, List<ContentIssue> issues)
    {
        var courseRef = manifest.Feedback.CourseRef;
        if (string.IsNullOrWhiteSpace(courseRef))
        {
            return;
        }

        var (file, anchor) = CourseAnchors.ParseRef(courseRef);
        if (!string.Equals(file, CourseFileName, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(anchor))
        {
            // Référence vers un autre fichier, ou vers le document entier (pas d'ancre) : rien à résoudre ici.
            return;
        }

        // Le cours.md est à la racine du module ; les Rushes n'en ont pas.
        if (location.ModuleId == ContentLocator.RushesModuleId)
        {
            return;
        }

        var moduleDir = Directory.GetParent(location.ContentDir)?.Parent?.FullName;
        var coursePath = moduleDir is null ? null : Path.Combine(moduleDir, CourseFileName);
        if (coursePath is null || !File.Exists(coursePath))
        {
            issues.Add(new ContentIssue(exerciseId, $"course_ref « {courseRef} » : {CourseFileName} introuvable pour le module."));
            return;
        }

        var anchors = CourseAnchors.Extract(File.ReadAllText(coursePath));
        if (!anchors.Contains(anchor))
        {
            issues.Add(new ContentIssue(exerciseId, $"course_ref « {courseRef} » : ancre #{anchor} absente de {CourseFileName}."));
        }
    }
}
