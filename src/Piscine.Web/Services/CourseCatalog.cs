using Piscine.Core;
using Piscine.Core.Content;

namespace Piscine.Web.Services;

/// <summary>
/// Charge une fois pour toutes le contenu pédagogique (cours + sujets, sans corrigés)
/// en réutilisant les loaders de <c>Piscine.Core</c>.
/// </summary>
public sealed class CourseCatalog
{
    public string ContentRoot { get; }

    public IReadOnlyList<CourseModule> Modules { get; }

    private readonly Dictionary<string, CourseModule> _byId;

    public CourseCatalog(IConfiguration config)
    {
        ContentRoot = ContentRootResolver.Resolve(config);
        Modules = Load(ContentRoot);
        _byId = Modules.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
    }

    public CourseModule? GetModule(string id)
        => _byId.GetValueOrDefault(id);

    public CourseExercise? GetExercise(string moduleId, string exerciseId)
        => GetModule(moduleId)?.Groups
            .SelectMany(g => g.Exercises)
            .FirstOrDefault(e => string.Equals(e.Id, exerciseId, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<CourseModule> Load(string contentRoot)
    {
        var modulesDir = new PiscinePaths(contentRoot).ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return [];
        }

        var modules = new List<CourseModule>();
        foreach (var moduleDir in Directory.EnumerateDirectories(modulesDir))
        {
            if (!File.Exists(Path.Combine(moduleDir, ModuleLoader.FileName)))
            {
                continue;
            }

            var module = ModuleLoader.Load(moduleDir);

            var courseFile = string.IsNullOrWhiteSpace(module.Course) ? "cours.md" : module.Course;
            var coursePath = Path.Combine(moduleDir, courseFile);
            var courseMarkdown = File.Exists(coursePath) ? File.ReadAllText(coursePath) : string.Empty;

            var groups = new List<CourseGroup>();
            foreach (var group in module.Groups)
            {
                var exercises = new List<CourseExercise>();
                foreach (var exerciseId in group.Exercises)
                {
                    var exerciseDir = Path.Combine(moduleDir, ContentLocator.ExercisesDirName, exerciseId);
                    if (!File.Exists(Path.Combine(exerciseDir, ExerciseManifestLoader.FileName)))
                    {
                        continue;
                    }

                    var manifest = ExerciseManifestLoader.Load(exerciseDir);
                    var subjectPath = Path.Combine(exerciseDir, "subject.md");
                    var subject = File.Exists(subjectPath)
                        ? StripLeadingH1(File.ReadAllText(subjectPath))
                        : null;

                    exercises.Add(new CourseExercise(
                        module.Id,
                        manifest.Id,
                        manifest.Title,
                        manifest.Objective,
                        manifest.Difficulty,
                        manifest.Bonus,
                        manifest.Deliverables,
                        subject));
                }

                groups.Add(new CourseGroup(group.Id, group.Title, exercises));
            }

            modules.Add(new CourseModule(module.Id, module.Order, module.Title, courseMarkdown, groups));
        }

        return modules
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Retire un titre H1 de tête du sujet : la page d'exercice affiche déjà le titre
    /// (et les badges), inutile de le répéter.
    /// </summary>
    private static string StripLeadingH1(string markdown)
    {
        var text = markdown.TrimStart('﻿', '\r', '\n', ' ', '\t');
        if (!text.StartsWith("# ", StringComparison.Ordinal))
        {
            return markdown;
        }

        var newline = text.IndexOf('\n');
        return newline < 0 ? string.Empty : text[(newline + 1)..].TrimStart('\r', '\n');
    }
}
