using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Progress;

namespace Piscine.App.Board;

/// <summary>Compteurs agrégés du tableau de bord (pur). Fait = PousseNote ; En cours = EnCours +
/// CommiteNonPousse ; À revoir = ARevoir ; Restant = NonCommence. % = Fait / Total (arrondi).</summary>
public sealed record BoardCounts(int Fait, int EnCours, int ARevoir, int Restant, int Total)
{
    public int PercentFait => Total == 0 ? 0 : (int)Math.Round(100.0 * Fait / Total);

    public static BoardCounts From(IReadOnlyList<ExerciseProgressStatus> statuses)
    {
        int Count(ExerciseProgressStatus s) => statuses.Count(x => x == s);
        var enCours = Count(ExerciseProgressStatus.EnCours) + Count(ExerciseProgressStatus.CommiteNonPousse);
        return new BoardCounts(
            Fait: Count(ExerciseProgressStatus.PousseNote),
            EnCours: enCours,
            ARevoir: Count(ExerciseProgressStatus.ARevoir),
            Restant: Count(ExerciseProgressStatus.NonCommence),
            Total: statuses.Count);
    }
}
