namespace Piscine.Core;

/// <summary>
/// Compose le libellé d'un exercice pour l'affichage CLI (<c>list</c>) : identifiant,
/// difficulté et marqueur bonus. Pur et testable, indépendant de la console.
/// </summary>
public static class ExerciseLabel
{
    /// <summary>Ex. <c>ex00-tri-bulle — facile</c>, ou <c>ex03-x — difficile (bonus)</c>.</summary>
    public static string Format(string id, string? difficulty, bool bonus, int estimatedMinutes = 0, int xp = 0)
    {
        var label = id;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            label += $" — {difficulty}";
        }

        if (bonus)
        {
            label += " (bonus)";
        }

        if (estimatedMinutes > 0)
        {
            label += $" · ≈ {estimatedMinutes} min";
        }

        if (xp > 0)
        {
            label += $" · {xp} XP";
        }

        return label;
    }
}
