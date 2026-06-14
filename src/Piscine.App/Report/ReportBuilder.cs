using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Git;
using Piscine.App.Progress;
using Piscine.App.Push;

namespace Piscine.App.Report;

/// <summary>
/// Description d'un exercice nécessaire au rapport (module, id, bonus), découplée du catalogue UI
/// pour garder <see cref="ReportBuilder"/> pur et testable sans <c>CourseCatalog</c>.
/// </summary>
public sealed record ReportExercise(string ModuleId, string ExerciseId, bool Bonus);

/// <summary>En-tête d'un module pour le rapport (numéro affiché + titre).</summary>
public sealed record ReportModuleHeader(string ModuleId, string Number, string Title);

/// <summary>
/// Composeur <b>pur</b> du <see cref="ReportModel"/> : assemble identité git, avancement global,
/// lignes par module et historique de push à partir de données déjà lues (aucun I/O ici). La page
/// fournit les entrées via les services existants ; ce builder reste testable sans rendu ni disque.
/// </summary>
public static class ReportBuilder
{
    public static ReportModel Build(
        RepoState repo,
        DateTimeOffset generatedAt,
        IReadOnlyList<ReportModuleHeader> modules,
        IReadOnlyList<ReportExercise> exercises,
        IReadOnlyDictionary<(string ModuleId, string ExerciseId), ExerciseProgressStatus> statuses,
        PushResult? recent)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(exercises);
        ArgumentNullException.ThrowIfNull(statuses);

        var allStatuses = exercises
            .Select(e => statuses.GetValueOrDefault((e.ModuleId, e.ExerciseId), ExerciseProgressStatus.NonCommence))
            .ToList();

        var counts = Piscine.App.Board.BoardCounts.From(allStatuses);

        var rows = new List<ReportModuleRow>();
        foreach (var module in modules)
        {
            var exos = exercises.Where(e => e.ModuleId == module.ModuleId).ToList();
            if (exos.Count == 0)
            {
                continue;
            }

            int fait = 0, enCours = 0, aRevoir = 0, restant = 0, bonusTotal = 0, bonusFaits = 0;
            foreach (var exo in exos)
            {
                var status = statuses.GetValueOrDefault((exo.ModuleId, exo.ExerciseId), ExerciseProgressStatus.NonCommence);
                switch (status)
                {
                    case ExerciseProgressStatus.PousseNote:
                        fait++;
                        break;
                    case ExerciseProgressStatus.EnCours:
                    case ExerciseProgressStatus.CommiteNonPousse:
                        enCours++;
                        break;
                    case ExerciseProgressStatus.ARevoir:
                        aRevoir++;
                        break;
                    default:
                        restant++;
                        break;
                }

                if (exo.Bonus)
                {
                    bonusTotal++;
                    if (status == ExerciseProgressStatus.PousseNote)
                    {
                        bonusFaits++;
                    }
                }
            }

            rows.Add(new ReportModuleRow(
                module.Number,
                module.Title,
                fait,
                enCours,
                aRevoir,
                restant,
                bonusFaits,
                bonusTotal,
                exos.Count));
        }

        var pushes = recent?.Changed
            .Select(c => new ReportPushEntry(
                c.ExerciseId,
                c.Verdict == PushVerdict.Reussi ? "Réussi" : "À revoir",
                c.Attempts))
            .ToList()
            ?? [];

        return new ReportModel(
            repo.UserName,
            repo.UserEmail,
            repo.CurrentBranch,
            generatedAt,
            counts.PercentFait,
            counts.Fait,
            counts.EnCours,
            counts.ARevoir,
            counts.Restant,
            counts.Total,
            rows,
            pushes);
    }
}
