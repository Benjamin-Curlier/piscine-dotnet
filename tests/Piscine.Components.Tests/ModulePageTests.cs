using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Git;
using Piscine.App.Progress;
using Piscine.Components.Components.Pages;
using Piscine.Components.Services;
using Piscine.Core;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit de la page <see cref="Module"/> : rendu des cartes d'exercice avec une pastille de
/// statut (<c>StatusDot</c>) par carte. Services réels (catalogue du dépôt + workspace temporaire
/// isolé, non initialisé → tous les exercices ressortent « non commencé »). JSInterop en mode loose.
/// </summary>
public sealed class ModulePageTests : BunitContext, IDisposable
{
    private readonly string _tempHome;

    public ModulePageTests()
    {
        var repoRoot = FindRepoRoot();
        var content = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-bunit-module-{Guid.NewGuid():N}");
        var workspace = Path.Combine(_tempHome, "workspace");
        var state = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(state);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PISCINE_CONTENT"] = content })
            .Build();

        var layout = new PiscineLayout(content, workspace, state);

        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(new CourseCatalog(config));
        Services.AddSingleton(layout);
        Services.AddSingleton<GitStatusService>();
        Services.AddSingleton(sp => new ProgressService(
            sp.GetRequiredService<PiscineLayout>(),
            sp.GetRequiredService<GitStatusService>()));
        // La page rend le cours via MarkdownView / CourseToc → MarkdownRenderer requis en DI.
        Services.AddSingleton<MarkdownRenderer>();
    }

    [Fact]
    public void Renders_a_status_dot_per_exercise_card()
    {
        var catalog = Services.GetRequiredService<CourseCatalog>();
        var module = catalog.Modules.First(m => m.HasExercises);
        var exerciseCount = module.Groups.SelectMany(g => g.Exercises).Count();

        var cut = Render<Module>(parameters => parameters.Add(p => p.Id, module.Id));

        var cards = cut.FindAll(".exercise-card");
        Assert.Equal(exerciseCount, cards.Count);
        // Une pastille de statut par carte (accessibilité : StatusDot expose aria-label + data-status).
        Assert.Equal(exerciseCount, cut.FindAll("[data-testid='status-dot']").Count);
    }

    public new void Dispose()
    {
        base.Dispose();
        try
        {
            if (Directory.Exists(_tempHome))
            {
                Directory.Delete(_tempHome, recursive: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Racine du dépôt introuvable (Piscine.slnx absent).");
    }
}
