using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Piscine.Components.Services;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class CourseCatalogMetadataTests
{
    [Fact]
    public void RepositoryCatalog_ExposesNarrativeTimingXpAndRushes()
    {
        var root = FindRepoRoot();
        var content = Path.Combine(root, "content");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PISCINE_CONTENT"] = content })
            .Build();

        var catalog = new CourseCatalog(config);
        var exercises = catalog.Modules.SelectMany(module => module.Groups.SelectMany(group => group.Exercises)).ToList();

        Assert.Equal(53, catalog.Modules.Count);
        Assert.Equal(185, exercises.Count);
        Assert.Equal(8, catalog.Rushes.Count);
        Assert.Equal(
            exercises.Sum(exercise => exercise.EstimatedMinutes)
            + catalog.Rushes.Sum(rush => rush.EstimatedMinutes),
            catalog.EstimatedMinutes);
        Assert.True(catalog.EstimatedMinutes > 0);
        Assert.All(catalog.Rushes, rush =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rush.RecommendedAfterModuleId));
            Assert.NotNull(catalog.GetRecommendedModule(rush));
            Assert.NotEmpty(rush.GradingTypes);
        });
        Assert.All(catalog.Modules, module =>
        {
            Assert.False(string.IsNullOrWhiteSpace(module.Arc));
            Assert.False(string.IsNullOrWhiteSpace(module.Mission));
            Assert.True(module.HasExercises);
        });
        Assert.All(exercises, exercise =>
        {
            Assert.InRange(exercise.EstimatedMinutes, 10, 240);
            Assert.True(exercise.Xp > 0);
            Assert.NotEmpty(exercise.Tags);
            Assert.False(string.IsNullOrWhiteSpace(exercise.StoryBeat));
        });
        Assert.Contains(catalog.Modules, module => module.Id == "43-aspire");
        Assert.Contains(catalog.Modules, module => module.Id == "45-desktop-mvvm");
        Assert.Contains(catalog.Modules, module => module.Id == "49-grpc-protobuf");
        Assert.Contains(catalog.Rushes, rush => rush.Id == "r6-asteria-distribue" && rush.Xp == 420);
        Assert.Contains(catalog.Rushes, rush => rush.Id == "r7-poste-entreprise" && rush.Xp == 650);
        Assert.True(catalog.GetRush("r6-asteria-distribue")!.ManualValidation);
        Assert.True(catalog.GetRush("r7-poste-entreprise")!.ManualValidation);
        Assert.False(catalog.GetRush("r5-event-processor")!.ManualValidation);
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
