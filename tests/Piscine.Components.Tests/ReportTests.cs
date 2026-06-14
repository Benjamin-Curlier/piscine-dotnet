using System;
using System.Collections.Generic;
using System.IO;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Git;
using Piscine.App.Progress;
using Piscine.App.Push;
using Piscine.Components.Components.Pages;
using Piscine.Components.Services;
using Piscine.Core;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit de la page <see cref="Report"/> : elle se rend sur les services réels (catalogue de
/// contenu du dépôt + workspace temporaire isolé), affiche l'en-tête d'identité, l'avancement, le
/// tableau par module et le bouton d'export. JSInterop en mode loose (l'import du module d'export
/// n'est pas exercé au rendu).
/// </summary>
public sealed class ReportTests : BunitContext
{
    private readonly string _tempHome;

    public ReportTests()
    {
        var repoRoot = FindRepoRoot();
        var content = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-bunit-report-{Guid.NewGuid():N}");
        var workspace = Path.Combine(_tempHome, "workspace");
        var state = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(state);

        // Planter un progress.json avec un exercice ARevoir pour que le tableau ait du contenu non trivial.
        var progress = new Piscine.Core.Model.Progress();
        progress.Exercises["ex00-hello"] = new Piscine.Core.Model.ExerciseProgress
        {
            Status = Piscine.Core.Model.ExerciseStatus.ARevoir,
            Attempts = 1,
        };
        new ProgressStore(Path.Combine(state, "progress.json")).Save(progress);

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
        Services.AddSingleton<IPushResultWatcher>(new FakeWatcher());
    }

    [Fact]
    public void Renders_header_progress_and_export_button()
    {
        var cut = Render<Report>();

        // En-tête identité + date présents.
        cut.Find("[data-testid='report']");
        cut.Find("[data-testid='report-identity']");
        cut.Find("[data-testid='report-date']");

        // Avancement global rendu.
        cut.Find("[data-testid='report-percent']");

        // Bouton d'export Markdown + impression présents.
        cut.Find("[data-testid='report-print']");
        cut.Find("[data-testid='report-copy-md']");
        cut.Find("[data-testid='report-save-md']");
    }

    [Fact]
    public void Renders_module_table_rows_from_catalog()
    {
        var cut = Render<Report>();

        // Le catalogue réel du dépôt comporte plusieurs modules avec exercices.
        var rows = cut.FindAll("[data-testid='report-module-row']");
        Assert.NotEmpty(rows);
        cut.Find("[data-testid='report-module-table']");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* nettoyage best-effort */ }
        }
        base.Dispose(disposing);
    }

    private sealed class FakeWatcher : IPushResultWatcher
    {
        public event Action<PushResult>? ResultReceived
        {
            add { }
            remove { }
        }

        public PushResult? LatestResult() => null;

        public PushResultDocument? LatestRichResult() => null;

        public void Start() { /* no-op */ }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }
}
