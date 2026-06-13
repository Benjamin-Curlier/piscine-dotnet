using Piscine.App.Progress;

namespace Piscine.Components.Components.Progress;

/// <summary>Libellé FR et suffixe de classe CSS partagés par StatusBadge et StatusDot (DRY).</summary>
public static class ExerciseProgressStatusText
{
    public static string Label(ExerciseProgressStatus status) => status switch
    {
        ExerciseProgressStatus.NonCommence => "Non commencé",
        ExerciseProgressStatus.EnCours => "En cours",
        ExerciseProgressStatus.CommiteNonPousse => "Commité, non poussé",
        ExerciseProgressStatus.PousseNote => "Poussé → noté",
        ExerciseProgressStatus.ARevoir => "À revoir",
        _ => status.ToString(),
    };

    public static string CssSuffix(ExerciseProgressStatus status) => status switch
    {
        ExerciseProgressStatus.NonCommence => "non-commence",
        ExerciseProgressStatus.EnCours => "en-cours",
        ExerciseProgressStatus.CommiteNonPousse => "commite-non-pousse",
        ExerciseProgressStatus.PousseNote => "pousse-note",
        ExerciseProgressStatus.ARevoir => "a-revoir",
        _ => "unknown",
    };
}
