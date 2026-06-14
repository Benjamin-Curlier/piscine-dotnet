using Piscine.App.Search;
using Piscine.Components.Navigation;

namespace Piscine.Components.Services;

/// <summary>
/// Construit l'index de la palette de commande (côté UI) depuis les briques existantes — sans nouvelle
/// source de vérité : destinations de navigation (<see cref="NavDestinations.Primary"/>), actions
/// globales, puis chaque module/exercice du <see cref="CourseCatalog"/> avec son markdown pour la
/// recherche plein-texte. Le tri/scoring est délégué au <see cref="SearchService"/> pur de Piscine.App.
/// </summary>
public static class SearchIndexBuilder
{
    /// <summary>Actions globales joignables depuis n'importe où via la palette.</summary>
    private static readonly (string Title, string Subtitle, string Route, string TestId, string[] Keywords)[] Actions =
    [
        ("Vérifier l'exercice", "Lancer la correction in-process", "/check", "cmd-action-check", ["check", "corriger", "tester"]),
        ("Initialiser le workspace", "Préparer le dépôt et le poste", "/init", "cmd-action-init", ["init", "setup", "demarrer"]),
        ("Voir le dernier résultat de push", "Verdict de la dernière soumission", "/resultat", "cmd-action-resultat", ["resultat", "push", "verdict"]),
        ("Ouvrir le terminal intégré", "Shell + coaching git", "/terminal", "cmd-action-terminal", ["terminal", "shell", "git"]),
    ];

    public static IReadOnlyList<SearchCommand> Build(CourseCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var commands = new List<SearchCommand>();

        // 1) Destinations de navigation primaires.
        foreach (var d in NavDestinations.Primary)
        {
            commands.Add(new SearchCommand(
                SearchKind.Destination,
                d.Label,
                "Navigation",
                d.Route,
                $"cmd-{d.TestId}",
                Keywords: [d.Route.Trim('/')]));
        }

        // 2) Actions globales.
        foreach (var (title, subtitle, route, testId, keywords) in Actions)
        {
            commands.Add(new SearchCommand(
                SearchKind.Action,
                title,
                subtitle,
                route,
                testId,
                Keywords: keywords));
        }

        // 3) Modules + exercices (avec markdown pour le plein-texte).
        foreach (var module in catalog.Modules)
        {
            commands.Add(new SearchCommand(
                SearchKind.Module,
                $"{module.Number} — {module.Title}",
                "Module",
                $"/module/{module.Id}",
                $"cmd-module-{module.Id}",
                Body: module.CourseMarkdown,
                Keywords: [module.Id, module.Number]));

            foreach (var exercise in module.Groups.SelectMany(g => g.Exercises))
            {
                var keywords = new List<string> { exercise.Id, module.Id };
                if (exercise.Bonus)
                {
                    keywords.Add("bonus");
                }

                commands.Add(new SearchCommand(
                    SearchKind.Exercise,
                    exercise.Title,
                    $"{module.Number} · {exercise.Id}",
                    $"/module/{module.Id}/{exercise.Id}",
                    $"cmd-exo-{module.Id}-{exercise.Id}",
                    Body: BuildExerciseBody(exercise),
                    Keywords: keywords));
            }
        }

        return commands;
    }

    private static string BuildExerciseBody(CourseExercise exercise)
    {
        // Objectif + sujet : alimente la recherche plein-texte sur l'énoncé.
        var parts = new List<string> { exercise.Objective };
        if (!string.IsNullOrWhiteSpace(exercise.SubjectMarkdown))
        {
            parts.Add(exercise.SubjectMarkdown);
        }

        return string.Join("\n\n", parts);
    }
}
