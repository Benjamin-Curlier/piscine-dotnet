using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Git;
using Piscine.App.Progress;
using Piscine.App.Push;
using Piscine.App.Report;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>
/// <see cref="ReportBuilder"/> est pur : il assemble identité + compteurs + lignes par module à
/// partir de données déjà lues (aucun I/O). On vérifie l'attribution par statut, le comptage des
/// bonus, l'historique de push et l'identité reprise du RepoState.
/// </summary>
public sealed class ReportBuilderTests
{
    private static readonly DateTimeOffset At =
        new(2026, 6, 14, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Builds_module_rows_counts_bonus_and_global_progress()
    {
        var repo = new RepoState
        {
            IsRepository = true,
            CurrentBranch = "main",
            UserName = "Ada",
            UserEmail = "ada@piscine.dev",
        };

        var modules = new List<ReportModuleHeader>
        {
            new("01-bases", "01", "Bases C#"),
        };

        var exercises = new List<ReportExercise>
        {
            new("01-bases", "ex00", Bonus: false),
            new("01-bases", "ex01", Bonus: false),
            new("01-bases", "ex02", Bonus: true),
            new("01-bases", "ex03", Bonus: true),
        };

        var statuses = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("01-bases", "ex00")] = ExerciseProgressStatus.PousseNote,
            [("01-bases", "ex01")] = ExerciseProgressStatus.EnCours,
            [("01-bases", "ex02")] = ExerciseProgressStatus.PousseNote, // bonus fait
            [("01-bases", "ex03")] = ExerciseProgressStatus.NonCommence, // bonus non fait
        };

        var model = ReportBuilder.Build(repo, At, modules, exercises, statuses, recent: null);

        Assert.Equal("Ada", model.UserName);
        Assert.Equal("ada@piscine.dev", model.UserEmail);
        Assert.Equal("main", model.Branch);
        Assert.Equal(At, model.GeneratedAt);

        // Global : 2 faits / 1 en cours / 1 restant sur 4.
        Assert.Equal(4, model.Total);
        Assert.Equal(2, model.Fait);
        Assert.Equal(1, model.EnCours);
        Assert.Equal(1, model.Restant);
        Assert.Equal(50, model.PercentFait);

        var row = Assert.Single(model.Modules);
        Assert.Equal("01", row.Number);
        Assert.Equal(2, row.Fait);
        Assert.Equal(1, row.EnCours);
        Assert.Equal(1, row.Restant);
        Assert.Equal(2, row.BonusTotal);
        Assert.Equal(1, row.BonusFaits);
        Assert.Equal(4, row.Total);
    }

    [Fact]
    public void CommiteNonPousse_counts_as_en_cours()
    {
        var modules = new List<ReportModuleHeader> { new("01-bases", "01", "Bases") };
        var exercises = new List<ReportExercise> { new("01-bases", "ex00", false) };
        var statuses = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("01-bases", "ex00")] = ExerciseProgressStatus.CommiteNonPousse,
        };

        var model = ReportBuilder.Build(new RepoState(), At, modules, exercises, statuses, null);

        Assert.Equal(1, model.EnCours);
        Assert.Equal(1, Assert.Single(model.Modules).EnCours);
    }

    [Fact]
    public void Modules_without_exercises_are_skipped()
    {
        var modules = new List<ReportModuleHeader>
        {
            new("01-bases", "01", "Bases"),
            new("99-vide", "99", "Vide"),
        };
        var exercises = new List<ReportExercise> { new("01-bases", "ex00", false) };
        var statuses = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("01-bases", "ex00")] = ExerciseProgressStatus.PousseNote,
        };

        var model = ReportBuilder.Build(new RepoState(), At, modules, exercises, statuses, null);

        Assert.Single(model.Modules);
        Assert.Equal("01", model.Modules[0].Number);
    }

    [Fact]
    public void Maps_recent_pushes_with_verdict_labels()
    {
        var recent = new PushResult(
            [
                new PushResultEntry("ex00", PushVerdict.Reussi, 1, At),
                new PushResultEntry("ex01", PushVerdict.ARevoir, 3, At),
            ],
            At);

        var model = ReportBuilder.Build(new RepoState(), At, [], [], new Dictionary<(string, string), ExerciseProgressStatus>(), recent);

        Assert.Equal(2, model.RecentPushes.Count);
        Assert.Equal("Réussi", model.RecentPushes.First(p => p.ExerciseId == "ex00").Verdict);
        Assert.Equal("À revoir", model.RecentPushes.First(p => p.ExerciseId == "ex01").Verdict);
        Assert.Equal(3, model.RecentPushes.First(p => p.ExerciseId == "ex01").Attempts);
    }
}
