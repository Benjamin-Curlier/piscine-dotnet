using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;

namespace Piscine.App.Progress;

/// <summary>
/// Réinitialise la progression locale (<c>progress.json</c>) — tout ou un sous-ensemble d'exercices.
/// Pur côté moteur (réutilise <see cref="ProgressStore"/>) : ne connaît pas le catalogue, l'appelant
/// (UI) fournit les identifiants d'exercice à effacer pour une réinitialisation « par module ».
/// </summary>
public sealed class ProgressResetService
{
    private readonly PiscineLayout _layout;

    public ProgressResetService(PiscineLayout layout) => _layout = layout;

    /// <summary>Efface TOUTE la progression (repart d'un <see cref="Core.Model.Progress"/> vide).</summary>
    public void ResetAll() => new ProgressStore(_layout.ProgressPath).Save(new Core.Model.Progress());

    /// <summary>
    /// Retire les exercices indiqués de la progression (no-op pour ceux absents). Renvoie le nombre
    /// d'entrées effectivement supprimées. Utilisé pour la réinitialisation « par module » : l'UI
    /// résout les identifiants d'exercice du module depuis le catalogue puis les passe ici.
    /// </summary>
    public int ResetExercises(IEnumerable<string> exerciseIds)
    {
        var store = new ProgressStore(_layout.ProgressPath);
        var progress = store.Load();

        var removed = 0;
        foreach (var id in exerciseIds)
        {
            if (progress.Exercises.Remove(id))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            store.Save(progress);
        }

        return removed;
    }
}
