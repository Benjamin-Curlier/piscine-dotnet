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
        foreach (var module in ContentDiscovery.DiscoverModules(layout.Content))
        {
            foreach (var exerciseId in module.Groups.SelectMany(g => g.Exercises))
            {
                ValidateExercise(layout, exerciseId, issues);
            }
        }

        // Les Rushes sont des livrables autonomes : on valide aussi leur corrigé.
        foreach (var rush in ContentDiscovery.DiscoverRushes(layout.Content))
        {
            ValidateExercise(layout, rush.Id, issues);
        }

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

        foreach (var testFile in manifest.Grading.SelectMany(s => s.TestFiles))
        {
            if (!File.Exists(Path.Combine(location.ContentDir, testFile)))
            {
                issues.Add(new ContentIssue(exerciseId, $"fichier de grader manquant : {testFile}"));
            }
        }

        ValidateDifficulty(exerciseId, manifest, issues);
        ValidateStarterFiles(exerciseId, location, manifest, issues);
        ValidateCourseRef(exerciseId, location, manifest, issues);
        ValidateHints(exerciseId, manifest, issues);

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
