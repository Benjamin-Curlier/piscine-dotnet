using System;
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Core.Tests;

public sealed class GamificationCalculatorTests
{
    [Fact]
    public void Calculate_UsesFirstSuccessAndLegacySuccess_AndAwardsBadges()
    {
        var today = new DateOnly(2026, 9, 1);
        var progress = new Progress
        {
            PracticeDays = { today.AddDays(-1), today },
            Exercises =
            {
                ["ex-architecture"] = new ExerciseProgress
                {
                    Status = ExerciseStatus.Reussi,
                    Attempts = 2,
                    FirstSucceededAt = new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero)
                },
                ["ex-bonus"] = new ExerciseProgress
                {
                    Status = ExerciseStatus.Reussi,
                    Attempts = 1
                },
                ["ex-failed"] = new ExerciseProgress { Status = ExerciseStatus.ARevoir, Attempts = 3 }
            }
        };
        var exercises = new List<GamificationExercise>
        {
            new("m1", "ex-architecture", 200, false, new[] { "architecture" }),
            new("m2", "ex-bonus", 100, true, new[] { "messaging" }),
            new("m2", "ex-failed", 500, false, new[] { "capstone" })
        };

        var result = GamificationCalculator.Calculate(exercises, progress, today);

        Assert.Equal(300, result.TotalXp);
        Assert.Equal(2, result.Level);
        Assert.Equal(2, result.CompletedCount);
        Assert.Equal(2, result.StreakDays);
        Assert.Contains(result.Badges, badge => badge.Id == "first-dive");
        Assert.Contains(result.Badges, badge => badge.Id == "no-buoy");
        Assert.Contains(result.Badges, badge => badge.Id == "architect");
        Assert.Contains(result.Badges, badge => badge.Id == "messenger");
        Assert.DoesNotContain(result.Badges, badge => badge.Id == "mission-control");
    }

    [Fact]
    public void Calculate_StreakMayEndYesterday_ButStopsAtGap()
    {
        var today = new DateOnly(2026, 9, 1);
        var progress = new Progress
        {
            PracticeDays = { today.AddDays(-4), today.AddDays(-2), today.AddDays(-1) }
        };

        var result = GamificationCalculator.Calculate([], progress, today);

        Assert.Equal(2, result.StreakDays);
        Assert.Equal(0, result.TotalXp);
        Assert.Equal(1, result.Level);
    }

    [Fact]
    public void Calculate_AwardsAsteriaBadgeOnlyWhenEveryRushIsComplete()
    {
        var today = new DateOnly(2026, 9, 1);
        var progress = new Progress
        {
            Exercises =
            {
                ["r0"] = new ExerciseProgress { Status = ExerciseStatus.Reussi },
                ["r1"] = new ExerciseProgress { Status = ExerciseStatus.Reussi },
            }
        };
        var catalog = new List<GamificationExercise>
        {
            new("rushes", "r0", 100, false, ["capstone"]),
            new("rushes", "r1", 100, false, ["capstone"]),
        };

        var complete = GamificationCalculator.Calculate(catalog, progress, today);
        Assert.Contains(complete.Badges, badge => badge.Id == "asteria-online");

        progress.Exercises.Remove("r1");
        var incomplete = GamificationCalculator.Calculate(catalog, progress, today);
        Assert.DoesNotContain(incomplete.Badges, badge => badge.Id == "asteria-online");
    }
}
