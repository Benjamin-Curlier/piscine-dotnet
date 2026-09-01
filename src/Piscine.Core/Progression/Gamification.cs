using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.Core.Content;
using Piscine.Core.Model;

namespace Piscine.Core.Progression;

/// <summary>Métadonnées minimales nécessaires au calcul de la progression ludique.</summary>
public sealed record GamificationExercise(
    string ModuleId,
    string Id,
    int Xp,
    bool Bonus,
    IReadOnlyList<string> Tags);

/// <summary>Badge déterministe : aucun compteur séparé à synchroniser.</summary>
public sealed record EarnedBadge(string Id, string Label, string Description);

/// <summary>Bilan calculé depuis le catalogue et progress.json.</summary>
public sealed record GamificationSummary(
    int TotalXp,
    int Level,
    int LevelStartXp,
    int NextLevelXp,
    int StreakDays,
    int CompletedCount,
    IReadOnlyList<EarnedBadge> Badges)
{
    public int XpIntoLevel => TotalXp - LevelStartXp;

    public int XpForNextLevel => NextLevelXp - LevelStartXp;

    public int LevelPercent => XpForNextLevel == 0
        ? 100
        : (int)Math.Clamp(Math.Round(100d * XpIntoLevel / XpForNextLevel), 0, 100);
}

/// <summary>
/// Calcule XP, niveau, série et badges. Une réussite ne rapporte ses XP qu'une fois car elle est
/// déduite de FirstSucceededAt (ou d'un ancien statut Reussi pour compatibilité).
/// </summary>
public static class GamificationCalculator
{
    private const int LevelStep = 250;

    public static GamificationSummary Calculate(
        IReadOnlyList<GamificationExercise> exercises,
        Progress progress,
        DateOnly today)
    {
        var completed = progress.Exercises
            .Where(pair => pair.Value.FirstSucceededAt.HasValue || pair.Value.Status == ExerciseStatus.Reussi)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        var completedExercises = exercises.Where(exercise => completed.ContainsKey(exercise.Id)).ToList();
        var totalXp = completedExercises.Sum(exercise => Math.Max(0, exercise.Xp));
        var level = 1;
        while (LevelStart(level + 1) <= totalXp)
        {
            level++;
        }

        var badges = BuildBadges(exercises, completedExercises, completed);
        return new GamificationSummary(
            totalXp,
            level,
            LevelStart(level),
            LevelStart(level + 1),
            CalculateStreak(progress.PracticeDays, today),
            completedExercises.Count,
            badges);
    }

    private static int LevelStart(int level) => LevelStep * (level - 1) * level / 2;

    private static int CalculateStreak(IEnumerable<DateOnly> practicedDays, DateOnly today)
    {
        var days = practicedDays.Distinct().ToHashSet();
        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static IReadOnlyList<EarnedBadge> BuildBadges(
        IReadOnlyList<GamificationExercise> catalog,
        IReadOnlyList<GamificationExercise> completed,
        IReadOnlyDictionary<string, ExerciseProgress> progress)
    {
        var badges = new List<EarnedBadge>();
        if (completed.Count > 0)
        {
            badges.Add(new EarnedBadge("first-dive", "Premier plongeon", "Premier exercice validé."));
        }

        if (completed.Count >= 25)
        {
            badges.Add(new EarnedBadge("explorer", "Explorateur", "25 exercices validés."));
        }

        if (completed.Count >= 75)
        {
            badges.Add(new EarnedBadge("veteran", "Vétéran", "75 exercices validés."));
        }

        if (completed.Any(exercise => exercise.Bonus
                && progress.TryGetValue(exercise.Id, out var entry)
                && entry.Attempts == 1))
        {
            badges.Add(new EarnedBadge("no-buoy", "Sans bouée", "Un bonus réussi du premier coup."));
        }

        AddTagBadge(badges, completed, "git", "navigator", "Navigateur Git", "Une mission Git validée.");
        AddTagBadge(badges, completed, "architecture", "architect", "Architecte", "Une mission d'architecture validée.");
        AddTagBadge(badges, completed, "messaging", "messenger", "Messager", "Une mission de messagerie validée.");
        AddTagBadge(badges, completed, "capstone", "mission-control", "Mission accomplie", "Un mini-projet transverse validé.");

        var configuredRushes = catalog
            .Where(exercise => exercise.ModuleId == ContentLocator.RushesModuleId)
            .ToList();
        if (configuredRushes.Count > 0 && configuredRushes.All(rush =>
                completed.Any(done => string.Equals(done.Id, rush.Id, StringComparison.Ordinal))))
        {
            badges.Add(new EarnedBadge(
                "asteria-online",
                "Asteria opérationnel",
                "Toutes les missions de synthèse sont validées."));
        }

        return badges;
    }

    private static void AddTagBadge(
        ICollection<EarnedBadge> badges,
        IEnumerable<GamificationExercise> completed,
        string tag,
        string id,
        string label,
        string description)
    {
        if (completed.Any(exercise => exercise.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            badges.Add(new EarnedBadge(id, label, description));
        }
    }
}
