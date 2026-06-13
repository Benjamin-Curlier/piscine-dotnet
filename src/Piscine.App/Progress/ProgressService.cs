using System.IO;
using Piscine.App.Git;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;

namespace Piscine.App.Progress;

/// <summary>
/// Dérive un <see cref="ExerciseProgressStatus"/> par exercice en combinant <c>progress.json</c>
/// (via <see cref="ProgressStore"/>), l'état git repo-wide (via <see cref="GitStatusService"/>) et
/// la présence de fichiers dans le workspace. <b>Lecture seule — n'écrit jamais.</b>
/// </summary>
public sealed class ProgressService(PiscineLayout layout, GitStatusService git)
{
    /// <summary>
    /// Calcule le statut dérivé pour un seul exercice. Charge progress.json et RepoState à
    /// chaque appel (adapté aux appels ponctuels depuis une page). Pour les vues de liste,
    /// préférer <see cref="SnapshotFor"/> qui charge une seule fois.
    /// </summary>
    public ExerciseStatusInfo StatusFor(string moduleId, string exerciseId)
    {
        var progress = new ProgressStore(layout.ProgressPath).Load();
        var repo = git.Read(layout.WorkspaceRoot);
        return Derive(moduleId, exerciseId, progress, repo);
    }

    /// <summary>
    /// Calcule le statut dérivé pour un ensemble d'exercices en chargeant progress.json et
    /// RepoState <b>une seule fois</b>.
    /// </summary>
    public IReadOnlyList<ExerciseStatusInfo> SnapshotFor(
        IEnumerable<(string ModuleId, string ExerciseId)> exos)
    {
        var progress = new ProgressStore(layout.ProgressPath).Load();
        var repo = git.Read(layout.WorkspaceRoot);

        var result = new List<ExerciseStatusInfo>();
        foreach (var (moduleId, exerciseId) in exos)
        {
            result.Add(Derive(moduleId, exerciseId, progress, repo));
        }
        return result;
    }

    /// <summary>
    /// Règle déterministe de dérivation du statut. Les signaux git sont attribués <b>par exercice</b>
    /// via le préfixe de chemin <c>&lt;moduleId&gt;/&lt;exerciseId&gt;/</c> (#65) : committer un exercice
    /// ne « contamine » plus le statut des autres.
    ///
    /// Priorité décroissante :
    /// 1. ARevoir  — progress.json dit ARevoir (signal fort, écrit par le moteur).
    /// 2. Reussi + HasOrigin + cet exo PAS en avance → PousseNote (best-effort : progress.json ne
    ///    distingue pas un check local d'un grade-received).
    /// 3. Reussi (sans origin, ou cet exo en avance sur origin) → CommiteNonPousse.
    /// 4. Pas d'entrée + cet exo en avance sur origin → CommiteNonPousse.
    /// 5. Pas d'entrée + (fichiers workspace || cet exo a du non-committé) → EnCours.
    /// 6. Sinon → NonCommence.
    /// </summary>
    private ExerciseStatusInfo Derive(
        string moduleId,
        string exerciseId,
        Piscine.Core.Model.Progress progress,
        RepoState repo)
    {
        var hasEntry = progress.Exercises.TryGetValue(exerciseId, out var entry);

        // Attribution par exercice : un fichier appartient à l'exo si son chemin (relatif au dépôt,
        // séparateur /) commence par « <moduleId>/<exerciseId>/ ».
        var prefix = $"{moduleId}/{exerciseId}/";
        var aheadHere = repo.AheadPaths.Any(p => p.StartsWith(prefix, StringComparison.Ordinal));
        var uncommittedHere = repo.UncommittedPaths.Any(p => p.StartsWith(prefix, StringComparison.Ordinal));

        // 1. ARevoir : signal persisté explicite.
        if (hasEntry && entry!.Status == ExerciseStatus.ARevoir)
        {
            return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.ARevoir, StatusSource.Progress);
        }

        // 2 & 3. Reussi : affiner avec l'état git DE CET EXERCICE.
        if (hasEntry && entry!.Status == ExerciseStatus.Reussi)
        {
            if (repo.HasOrigin && !aheadHere)
            {
                return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.PousseNote, StatusSource.GitDerived);
            }

            return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.CommiteNonPousse, StatusSource.GitDerived);
        }

        // 4. Pas d'entrée + commits de CET exo en avance → CommiteNonPousse.
        if (!hasEntry && aheadHere)
        {
            return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.CommiteNonPousse, StatusSource.GitDerived);
        }

        // 5. Fichiers dans le workspace ou travail non commité DE CET exo → EnCours.
        var hasFiles = HasWorkspaceFiles(moduleId, exerciseId);
        if (hasFiles || uncommittedHere)
        {
            return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.EnCours, StatusSource.GitDerived);
        }

        // 6. Rien → NonCommence.
        return new ExerciseStatusInfo(moduleId, exerciseId, ExerciseProgressStatus.NonCommence, StatusSource.Progress);
    }

    private bool HasWorkspaceFiles(string moduleId, string exerciseId)
    {
        var dir = layout.WorkspaceExerciseDir(moduleId, exerciseId);
        if (!Directory.Exists(dir))
        {
            return false;
        }

        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Any();
    }
}
