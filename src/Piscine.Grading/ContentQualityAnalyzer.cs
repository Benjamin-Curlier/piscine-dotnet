using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;

namespace Piscine.Grading;

public sealed record ContentQualityIssue(string Scope, string Rule, string Message);

/// <summary>
/// Audit auteur déterministe et non bloquant. Il ne remplace pas <see cref="ContentValidator"/> :
/// il signale des risques pédagogiques qui demandent une décision humaine.
/// </summary>
public static partial class ContentQualityAnalyzer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "avec", "dans", "pour", "puis", "sans", "une", "des", "les", "qui", "que", "sur",
        "aux", "par", "est", "sont", "doit", "doivent", "chaque", "exercice", "programme"
    };

    public static IReadOnlyList<ContentQualityIssue> Analyze(PiscineLayout layout)
    {
        var issues = new List<ContentQualityIssue>();
        foreach (var module in ContentDiscovery.DiscoverModules(layout.Content))
        {
            var moduleDir = Path.Combine(layout.Content.ModulesDirectory, module.Id);
            var coursePath = Path.Combine(moduleDir, string.IsNullOrWhiteSpace(module.Course) ? "cours.md" : module.Course);
            var course = File.Exists(coursePath) ? File.ReadAllText(coursePath) : string.Empty;
            var exercises = LoadExercises(moduleDir, module);

            if (exercises.Count == 0)
            {
                issues.Add(new ContentQualityIssue(
                    module.Id,
                    "empty-module",
                    "le module ne contient aucun exercice auto-corrigé."));
            }

            DetectSolutionLeakage(course, exercises, issues);
            DetectSimilarObjectives(module.Id, exercises, issues);
            DetectFileBasedNameCollisions(module.Id, exercises, issues);
            DetectSuspiciousDifficulty(exercises, issues);
        }

        return issues;
    }

    private static List<ExerciseDocument> LoadExercises(string moduleDir, Module module)
    {
        var documents = new List<ExerciseDocument>();
        foreach (var exerciseId in module.Groups.SelectMany(group => group.Exercises))
        {
            var directory = Path.Combine(moduleDir, ContentLocator.ExercisesDirName, exerciseId);
            try
            {
                var manifest = ExerciseManifestLoader.Load(directory);
                var subjectPath = Path.Combine(directory, "subject.md");
                var subject = File.Exists(subjectPath) ? File.ReadAllText(subjectPath) : string.Empty;
                var starterDirectory = Path.Combine(directory, StarterInstaller.StarterDirName);
                var starter = Directory.Exists(starterDirectory)
                    ? string.Join("\n", Directory.EnumerateFiles(starterDirectory, "*", SearchOption.AllDirectories)
                        .Where(IsProbablyText)
                        .Select(File.ReadAllText))
                    : string.Empty;
                var hints = string.Join("\n", manifest.Feedback.Hints.Select(hint => hint.Message));
                documents.Add(new ExerciseDocument(directory, manifest, subject, ExtractComments(starter), hints));
            }
            catch (Exception e) when (e is IOException or InvalidOperationException)
            {
                // validate-content rapporte le contenu illisible ; l'audit consultatif poursuit les autres exercices.
            }
        }

        return documents;
    }

    private static void DetectSolutionLeakage(
        string course,
        IReadOnlyList<ExerciseDocument> exercises,
        List<ContentQualityIssue> issues)
    {
        foreach (var exercise in exercises)
        {
            var guidance = course + "\n" + exercise.Subject + "\n" + exercise.Hints;
            var solutionDir = Path.Combine(exercise.Directory, ContentValidator.SolutionDirName);
            if (!Directory.Exists(solutionDir))
            {
                continue;
            }

            var implementationLines = Directory.EnumerateFiles(solutionDir, "*", SearchOption.AllDirectories)
                .Where(IsProbablyText)
                .SelectMany(File.ReadLines)
                .Select(line => line.Trim())
                .Where(LooksLikeImplementation)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var guidanceLeak = exercise.Manifest.Difficulty == "facile"
                ? null
                : implementationLines.FirstOrDefault(line => guidance.Contains(line, StringComparison.Ordinal));
            if (guidanceLeak is not null)
            {
                issues.Add(new ContentQualityIssue(
                    exercise.Manifest.Id,
                    "solution-leak",
                    $"une ligne d'implémentation du corrigé est reproduite dans le cours, l'énoncé ou un indice : {Abbreviate(guidanceLeak)}"));
            }

            var starterLeak = implementationLines.FirstOrDefault(line =>
                exercise.Starter.Contains(line, StringComparison.Ordinal));
            if (starterLeak is not null)
            {
                issues.Add(new ContentQualityIssue(
                    exercise.Manifest.Id,
                    "starter-leak",
                    $"le starter contient déjà une ligne d'implémentation structurante du corrigé : {Abbreviate(starterLeak)}"));
            }
        }
    }

    private static void DetectSimilarObjectives(
        string moduleId,
        IReadOnlyList<ExerciseDocument> exercises,
        List<ContentQualityIssue> issues)
    {
        for (var left = 0; left < exercises.Count; left++)
        {
            var leftTokens = Tokens(exercises[left].Manifest.Objective);
            if (leftTokens.Count < 5)
            {
                continue;
            }

            for (var right = left + 1; right < exercises.Count; right++)
            {
                var rightTokens = Tokens(exercises[right].Manifest.Objective);
                if (rightTokens.Count < 5)
                {
                    continue;
                }

                var union = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
                var common = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
                var similarity = union == 0 ? 0 : (double)common / union;
                if (similarity >= 0.72)
                {
                    issues.Add(new ContentQualityIssue(
                        moduleId,
                        "similar-objectives",
                        $"{exercises[left].Manifest.Id} et {exercises[right].Manifest.Id} ont des objectifs très proches ({similarity:P0})."));
                }
            }
        }
    }

    private static void DetectFileBasedNameCollisions(
        string moduleId,
        IReadOnlyList<ExerciseDocument> exercises,
        List<ContentQualityIssue> issues)
    {
        var names = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var exercise in exercises.Where(document => document.Manifest.Starter.Count == 1))
        {
            var starter = exercise.Manifest.Starter[0];
            var path = Path.Combine(exercise.Directory, StarterInstaller.StarterDirName, starter);
            if (!File.Exists(path) || !File.ReadAllText(path).Contains("#:package ", StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = Path.GetFileName(starter);
            if (!names.TryGetValue(fileName, out var ids))
            {
                names[fileName] = ids = new List<string>();
            }

            ids.Add(exercise.Manifest.Id);
        }

        foreach (var (fileName, ids) in names.Where(pair => pair.Value.Count > 1))
        {
            issues.Add(new ContentQualityIssue(
                moduleId,
                "file-based-name",
                $"le starter file-based {fileName} est partagé par {string.Join(", ", ids)} ; utilise un nom unique par exercice."));
        }
    }

    private static void DetectSuspiciousDifficulty(
        IReadOnlyList<ExerciseDocument> exercises,
        List<ContentQualityIssue> issues)
    {
        foreach (var exercise in exercises)
        {
            var solutionDir = Path.Combine(exercise.Directory, ContentValidator.SolutionDirName);
            var isRepositoryExercise = exercise.Manifest.Grading.Count > 0
                && exercise.Manifest.Grading.All(step => step.Type == "git");
            if (exercise.Manifest.Difficulty == "difficile"
                && !isRepositoryExercise
                && exercise.Manifest.Feedback.Hints.Count < 2)
            {
                issues.Add(new ContentQualityIssue(
                    exercise.Manifest.Id,
                    "hint-depth",
                    "exercice difficile avec moins de deux paliers d'indice ; ajoute un indice conceptuel puis un indice technique."));
            }

            var minutes = exercise.Manifest.EstimatedMinutes;
            var suspiciousDuration = exercise.Manifest.Difficulty switch
            {
                "facile" => minutes is < 10 or > 60,
                "moyen" => minutes is < 25 or > 120,
                "difficile" => minutes is < 50 or > 240,
                _ => false
            };
            if (suspiciousDuration)
            {
                issues.Add(new ContentQualityIssue(
                    exercise.Manifest.Id,
                    "duration",
                    $"durée {minutes} min atypique pour la difficulté {exercise.Manifest.Difficulty}."));
            }
        }
    }

    private static HashSet<string> Tokens(string text) => WordRegex()
        .Matches(text.ToLowerInvariant())
        .Select(match => match.Value)
        .Where(token => !StopWords.Contains(token))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool LooksLikeImplementation(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 28 || trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        return trimmed.Contains("return ", StringComparison.Ordinal)
            || trimmed.Contains(".Select(", StringComparison.Ordinal)
            || trimmed.Contains(".Where(", StringComparison.Ordinal)
            || trimmed.Contains(".GroupBy(", StringComparison.Ordinal)
            || trimmed.Contains(".OrderBy", StringComparison.Ordinal)
            || trimmed.Contains("switch", StringComparison.Ordinal)
            || trimmed.Contains("await ", StringComparison.Ordinal)
            || trimmed.Contains("JsonSerializer.", StringComparison.Ordinal)
            || trimmed.Contains("new HashSet<", StringComparison.Ordinal);
    }

    private static bool IsMeaningfulSolutionLine(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length > 0
            && trimmed is not "{" and not "}" and not ");" and not "];"
            && !trimmed.StartsWith("//", StringComparison.Ordinal)
            && !trimmed.StartsWith("using ", StringComparison.Ordinal)
            && !trimmed.StartsWith("namespace ", StringComparison.Ordinal)
            && !trimmed.StartsWith("#!", StringComparison.Ordinal)
            && !trimmed.StartsWith("#:", StringComparison.Ordinal);
    }

    private static string Abbreviate(string line) => line.Length <= 100 ? line : line[..97] + "…";

    private static bool IsProbablyText(string path) =>
        !new[] { ".png", ".jpg", ".jpeg", ".gif", ".dll", ".exe", ".pdb", ".zip" }
            .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string ExtractComments(string text)
    {
        var comments = new List<string>();
        var inBlock = false;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (inBlock)
            {
                var end = line.IndexOf("*/", StringComparison.Ordinal);
                comments.Add(end >= 0 ? line[..end] : line);
                inBlock = end < 0;
                continue;
            }

            var lineComment = line.IndexOf("//", StringComparison.Ordinal);
            var blockComment = line.IndexOf("/*", StringComparison.Ordinal);
            if (lineComment >= 0 && (blockComment < 0 || lineComment < blockComment))
            {
                comments.Add(line[(lineComment + 2)..]);
                continue;
            }

            if (blockComment >= 0)
            {
                var content = line[(blockComment + 2)..];
                var end = content.IndexOf("*/", StringComparison.Ordinal);
                comments.Add(end >= 0 ? content[..end] : content);
                inBlock = end < 0;
            }
        }

        return string.Join("\n", comments);
    }

    [GeneratedRegex(@"[\p{L}\p{N}][\p{L}\p{N}-]{2,}")]
    private static partial Regex WordRegex();

    private sealed record ExerciseDocument(
        string Directory,
        ExerciseManifest Manifest,
        string Subject,
        string Starter,
        string Hints);
}
