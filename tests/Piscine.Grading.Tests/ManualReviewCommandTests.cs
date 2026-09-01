using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Grading;

namespace Piscine.Grading.Tests;

public sealed class ManualReviewCommandTests
{
    [Fact]
    public void Approve_PendingManualMission_RecordsReviewerAndAwardsSuccess()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, manualValidation: true, ExerciseStatus.EnAttenteRelecture);
        var instant = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var result = new ManualReviewCommand(layout, new FixedClock(instant)).Run(
            "r-demo",
            approved: true,
            "ticket QA-42");

        Assert.Equal(0, result.ExitCode);
        var entry = new ProgressStore(layout.ProgressPath).Load().Exercises["r-demo"];
        Assert.Equal(ExerciseStatus.Reussi, entry.Status);
        Assert.Equal(instant, entry.FirstSucceededAt);
        Assert.Equal(instant, entry.ReviewedAt);
        Assert.Equal("auto-relecture", entry.ReviewedBy);
        Assert.Equal("ticket QA-42", entry.ReviewEvidence);
    }

    [Fact]
    public void Reject_PendingManualMission_RequiresAnotherAutomaticPass()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, manualValidation: true, ExerciseStatus.EnAttenteRelecture);

        var result = new ManualReviewCommand(layout).Run(
            "r-demo",
            approved: false,
            "preuve de reprise manquante");

        Assert.Equal(0, result.ExitCode);
        var entry = new ProgressStore(layout.ProgressPath).Load().Exercises["r-demo"];
        Assert.Equal(ExerciseStatus.ARevoir, entry.Status);
        Assert.Null(entry.FirstSucceededAt);
    }

    [Fact]
    public void Review_RefusesNonManualOrNonPendingExercise()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, manualValidation: false, ExerciseStatus.EnAttenteRelecture);

        var result = new ManualReviewCommand(layout).Run("r-demo", true, "preuve");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("ne demande pas", result.Output);
    }

    [Fact]
    public void Reopen_CompletedSelfReview_MarksMissionForImprovement()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, manualValidation: true, ExerciseStatus.Reussi);

        var result = new ManualReviewCommand(layout).Run(
            "r-demo",
            approved: false,
            "je veux rejouer la coupure NATS");

        Assert.Equal(0, result.ExitCode);
        var entry = new ProgressStore(layout.ProgressPath).Load().Exercises["r-demo"];
        Assert.Equal(ExerciseStatus.ARevoir, entry.Status);
        Assert.Equal("je veux rejouer la coupure NATS", entry.ReviewEvidence);
    }

    private static PiscineLayout Setup(TempDir dir, bool manualValidation, ExerciseStatus status)
    {
        var exerciseDir = Path.Combine("content", "rushes", "r-demo");
        dir.WriteFile(Path.Combine(exerciseDir, "manifest.yaml"), $$"""
            id: r-demo
            title: Demo
            objective: Demo
            difficulty: difficile
            estimated_minutes: 30
            xp: 100
            tags: [capstone]
            story_beat: Demo
            recommended_after: 00-setup
            manual_validation: {{manualValidation.ToString().ToLowerInvariant()}}
            deliverables: [OPERATIONS.md]
            grading:
              - type: fichier
                file:
                  rules:
                    - path: OPERATIONS.md
                      required_fragments: [preuve]
            """);
        var layout = new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state"));
        var progress = new Progress();
        progress.Exercises["r-demo"] = new ExerciseProgress { Status = status, Attempts = 1 };
        new ProgressStore(layout.ProgressPath).Save(progress);
        return layout;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
