using Piscine.Components.Services;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Git;

namespace Piscine.DevHost.Qa;

/// <summary>
/// Seede un état déterministe dans le PISCINE_HOME courant (résolu par <see cref="PiscineLayout"/>).
/// Réservé au DevHost (hôte dev/test, non livré) ; activé via la variable d'environnement
/// <c>PISCINE_QA_PROFILE</c>. Tout l'état provient des types réels du moteur (<see cref="ProgressStore"/>,
/// <see cref="LastPushResultStore"/>, <see cref="GitWorkspace"/>, <see cref="CourseCatalog"/>) — aucun
/// JSON codé en dur, aucune duplication de logique. Le seeder est idempotent : il réécrit l'état à
/// chaque démarrage pour garantir le même rendu à chaque lancement d'un profil donné.
/// </summary>
public static class QaSeeder
{
    /// <summary>Chemin exe « piscine » écrit dans le hook post-receive (jamais exécuté en QA — le
    /// hook n'est pas appelé sans push réel). Aligné sur le repli de <c>InitService</c>.</summary>
    private const string PiscineExe = "piscine";

    public static void Seed(QaProfile profile, PiscineLayout layout, CourseCatalog catalog)
    {
        Directory.CreateDirectory(layout.StateDir);
        Directory.CreateDirectory(layout.WorkspaceRoot);

        // Repartir d'un état propre à chaque démarrage (déterminisme).
        SafeDelete(layout.ProgressPath);
        SafeDelete(layout.LastPushResultPath);

        switch (profile)
        {
            case QaProfile.Fresh:
                EnsureUninitialized(layout);          // overlay onboarding
                break;

            case QaProfile.Mixed:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildMixedProgress(catalog, layout));
                break;

            case QaProfile.ExoPass:
                EnsureInitialized(layout);
                SaveSingle(layout, FirstExerciseId(catalog), ExerciseStatus.Reussi, attempts: 1);
                break;

            case QaProfile.ExoFail:
                EnsureInitialized(layout);
                SaveSingle(layout, FirstExerciseId(catalog), ExerciseStatus.ARevoir, attempts: 2);
                // Artefact riche « échec » pour que le diff structuré (CheckFeedback) ait de quoi rendre
                // sur /resultat à l'arrivée du delta (cf. SKILL : re-toucher progress.json après chargement).
                WriteRichResult(layout, catalog, success: false);
                break;

            case QaProfile.PushResult:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildMixedProgress(catalog, layout));
                WriteRichResult(layout, catalog, success: true); // toast + /resultat
                break;

            case QaProfile.Done:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildAllReussi(catalog));
                break;
        }
    }

    // ── Construction de progression ─────────────────────────────────────────────

    /// <summary>
    /// Progression variée sur les premiers exercices du catalogue : on alterne Reussi / « en cours » /
    /// ARevoir. Le statut persisté ne connaît que NonCommence/ARevoir/Reussi ; « en cours » est un statut
    /// <i>dérivé</i> (cf. <c>ProgressService</c>) déclenché par la présence de fichiers dans le workspace —
    /// on dépose donc un fichier marqueur pour les exercices « en cours » afin que le tableau de bord
    /// affiche bien les quatre familles de pastilles (Fait / En cours / À revoir / Restant).
    /// </summary>
    private static Progress BuildMixedProgress(CourseCatalog catalog, PiscineLayout layout)
    {
        var progress = new Progress();
        var ids = EnumerateExercises(catalog).Take(18).ToList();
        for (var i = 0; i < ids.Count; i++)
        {
            var (moduleId, exerciseId) = ids[i];
            switch (i % 3)
            {
                case 0:
                    progress.Exercises[exerciseId] =
                        new ExerciseProgress { Status = ExerciseStatus.Reussi, Attempts = 1 };
                    break;
                case 1:
                    // « En cours » dérivé : pas d'entrée progress.json, mais un fichier au workspace.
                    DropWorkInProgressFile(layout, moduleId, exerciseId);
                    break;
                default:
                    progress.Exercises[exerciseId] =
                        new ExerciseProgress { Status = ExerciseStatus.ARevoir, Attempts = i % 3 + 1 };
                    break;
            }
        }

        return progress;
    }

    /// <summary>Tout le catalogue en Reussi (profil <c>done</c> : rapport ~complet).</summary>
    private static Progress BuildAllReussi(CourseCatalog catalog)
    {
        var progress = new Progress();
        foreach (var (_, exerciseId) in EnumerateExercises(catalog))
        {
            progress.Exercises[exerciseId] = new ExerciseProgress { Status = ExerciseStatus.Reussi, Attempts = 1 };
        }

        return progress;
    }

    /// <summary>Persiste un statut pour un unique exercice (profils <c>exo-pass</c>/<c>exo-fail</c>).</summary>
    private static void SaveSingle(PiscineLayout layout, (string ModuleId, string ExerciseId) exo, ExerciseStatus status, int attempts)
    {
        var progress = new Progress
        {
            Exercises =
            {
                [exo.ExerciseId] = new ExerciseProgress
                {
                    Status = status,
                    Attempts = attempts,
                    LastAttempt = new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero), // déterministe
                },
            },
        };
        new ProgressStore(layout.ProgressPath).Save(progress);
    }

    // ── Résultat riche du dernier push (modèle réel #40, jamais codé en dur) ─────

    /// <summary>
    /// Écrit <c>last-push-result.json</c> via le modèle riche réel (<see cref="PushResultDocument"/> +
    /// <see cref="LastPushResultStore"/>) pour le 1ᵉʳ exercice du catalogue : succès → un cas vert ;
    /// échec → un cas rouge avec diff « Attendu/Obtenu » + indice + renvoi cours, tel que <c>/resultat</c>
    /// (et le diff <c>CheckFeedback</c>) sait le rendre.
    /// </summary>
    private static void WriteRichResult(PiscineLayout layout, CourseCatalog catalog, bool success)
    {
        var (moduleId, exerciseId) = FirstExerciseId(catalog);

        var cases = success
            ? new[] { new PushCaseResult("io", true, new[] { "Sortie conforme." }) }
            : new[]
            {
                new PushCaseResult(
                    "io",
                    false,
                    new[]
                    {
                        "La sortie ne correspond pas.",
                        "Attendu : Hello, World!",
                        "Obtenu  : Hello, world",
                    }),
            };

        var exercise = new PushExerciseResult(
            ExerciseId: exerciseId,
            ModuleId: moduleId,
            Status: success ? "Reussi" : "ARevoir",
            Cases: cases,
            Hint: success ? null : "Vérifie la casse et la ponctuation exacte attendue.",
            CourseRef: success ? null : "cours.md#hello");

        var document = new PushResultDocument(
            new[] { exercise },
            new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero)); // déterministe

        new LastPushResultStore(layout.LastPushResultPath).Save(document);
    }

    // ── Énumération du catalogue (API réelle CourseCatalog) ─────────────────────

    /// <summary>Énumère (moduleId, exerciseId) dans l'ordre du catalogue (modules triés, groupes, exos).</summary>
    private static IEnumerable<(string ModuleId, string ExerciseId)> EnumerateExercises(CourseCatalog catalog) =>
        catalog.Modules
            .SelectMany(m => m.Groups.SelectMany(g => g.Exercises))
            .Select(e => (e.ModuleId, e.Id));

    private static (string ModuleId, string ExerciseId) FirstExerciseId(CourseCatalog catalog) =>
        EnumerateExercises(catalog).FirstOrDefault();

    // ── Signal d'initialisation (état git réel, via GitWorkspace) ───────────────

    /// <summary>
    /// Pose le signal d'« initialisé » réel lu par <c>InitService.Status</c> /
    /// <c>OnboardingState.ShouldShow</c> : un dépôt workspace valide, un dépôt bare « origin » et le hook
    /// post-receive. On réutilise <see cref="GitWorkspace.Initialize"/> (idempotent) — aucune duplication.
    /// </summary>
    private static void EnsureInitialized(PiscineLayout layout) =>
        GitWorkspace.Initialize(layout, PiscineExe);

    /// <summary>
    /// Retire le signal d'init (profil <c>fresh</c> → overlay onboarding) : on efface les dépôts git du
    /// workspace et du bare « origin » pour que <c>Repository.IsValid</c> renvoie faux des deux côtés.
    /// </summary>
    private static void EnsureUninitialized(PiscineLayout layout)
    {
        SafeDeleteDirectory(Path.Combine(layout.WorkspaceRoot, ".git"));
        SafeDeleteDirectory(layout.RemoteRepoPath);
    }

    // ── Helpers fichiers ─────────────────────────────────────────────────────────

    /// <summary>Dépose un fichier marqueur dans le dossier d'un exercice → statut dérivé « en cours ».</summary>
    private static void DropWorkInProgressFile(PiscineLayout layout, string moduleId, string exerciseId)
    {
        var dir = layout.WorkspaceExerciseDir(moduleId, exerciseId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Program.cs"), "// QA: travail en cours\n");
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort : un état résiduel ne doit jamais empêcher le démarrage du harnais.
        }
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                ClearReadOnly(path);
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best-effort.
        }
    }

    /// <summary>Les objets git packés sont parfois en lecture seule (Windows) : à dégeler avant suppression.</summary>
    private static void ClearReadOnly(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try { File.SetAttributes(file, FileAttributes.Normal); }
            catch { /* best-effort */ }
        }
    }
}
