using Microsoft.Extensions.Configuration;
using Piscine.App.Progress;
using Piscine.Components.Services;
using Piscine.Core.Content;
using Piscine.Core.Model;

namespace Piscine.Components.Tests;

public sealed class RushProgressCalculatorTests
{
    [Fact]
    public void Build_marks_milestone_reached_and_preserves_attempt_history()
    {
        var catalog = CreateCatalog();
        var milestone = catalog.GetModule("04-tableaux-chaines")!;
        var statuses = catalog.Modules
            .SelectMany(module => module.Groups.SelectMany(group => group.Exercises))
            .Select(exercise => new ExerciseStatusInfo(
                exercise.ModuleId,
                exercise.Id,
                catalog.GetModule(exercise.ModuleId)!.Order <= milestone.Order
                    ? ExerciseProgressStatus.PousseNote
                    : ExerciseProgressStatus.NonCommence,
                StatusSource.Progress))
            .Concat(catalog.Rushes.Select(rush => new ExerciseStatusInfo(
                ContentLocator.RushesModuleId,
                rush.Id,
                ExerciseProgressStatus.NonCommence,
                StatusSource.Progress)))
            .ToList();
        var progress = new Progress
        {
            Exercises =
            {
                ["r0-fizzbuzz"] = new ExerciseProgress
                {
                    Status = ExerciseStatus.ARevoir,
                    Attempts = 2,
                    LastAttempt = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero),
                }
            }
        };

        var items = RushProgressCalculator.Build(catalog, statuses, progress);
        var r0 = Assert.Single(items, item => item.Rush.Id == "r0-fizzbuzz");
        var r1 = Assert.Single(items, item => item.Rush.Id == "r1-inventaire");

        Assert.True(r0.MilestoneReached);
        Assert.False(r1.MilestoneReached);
        Assert.Equal(2, r0.Attempts);
        Assert.NotNull(r0.LastAttempt);
    }

    [Fact]
    public void Recommend_prioritizes_a_rush_to_review_over_a_ready_new_rush()
    {
        var catalog = CreateCatalog();
        var statuses = catalog.Modules
            .SelectMany(module => module.Groups.SelectMany(group => group.Exercises))
            .Select(exercise => new ExerciseStatusInfo(
                exercise.ModuleId,
                exercise.Id,
                ExerciseProgressStatus.PousseNote,
                StatusSource.Progress))
            .Concat(catalog.Rushes.Select(rush => new ExerciseStatusInfo(
                ContentLocator.RushesModuleId,
                rush.Id,
                rush.Id == "r3-traitement" ? ExerciseProgressStatus.ARevoir : ExerciseProgressStatus.NonCommence,
                StatusSource.Progress)))
            .ToList();

        var items = RushProgressCalculator.Build(catalog, statuses, new Progress());
        var recommended = RushProgressCalculator.Recommend(items);

        Assert.Equal("r3-traitement", recommended!.Rush.Id);
        Assert.Equal(1, RushProgressCalculator.Counts(items).ARevoir);
    }

    private static CourseCatalog CreateCatalog()
    {
        var root = FindRepoRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PISCINE_CONTENT"] = Path.Combine(root, "content")
            })
            .Build();
        return new CourseCatalog(configuration);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Piscine.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Racine du dépôt introuvable.");
    }
}
