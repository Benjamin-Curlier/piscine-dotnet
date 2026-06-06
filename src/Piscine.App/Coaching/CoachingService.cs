using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Git;

namespace Piscine.App.Coaching;

/// <summary>
/// Moteur de coaching <b>pur et agnostique au shell</b> : a partir d'un <see cref="RepoState"/>, du
/// dernier <see cref="GitCommandEvent"/> (ou <c>null</c> = « lecture d'etat seule ») et des
/// <see cref="ExerciseExpectation"/>, il produit une liste ordonnee de <see cref="HintCard"/>
/// (le plus bloquant d'abord). Aucune dependance au terminal : ne lit que argv / exit code / etat.
/// Registre purement educatif (jamais de note). Implemente le tableau de la spec section 5.
/// </summary>
public sealed class CoachingService
{
    /// <summary>Sous-commandes git connues servant a detecter les typos (distance Levenshtein 1).</summary>
    private static readonly string[] KnownSubcommands =
    [
        "add", "branch", "checkout", "clone", "commit", "config", "diff", "fetch", "init",
        "log", "merge", "pull", "push", "rebase", "remote", "reset", "restore", "rm",
        "stash", "status", "switch", "tag",
    ];

    /// <summary>Evalue l'etat et le dernier evenement git en cartes d'indices ordonnees (plus bloquant d'abord).</summary>
    public IReadOnlyList<HintCard> Evaluate(RepoState state, GitCommandEvent? lastCommand, ExerciseExpectation expectation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(expectation);

        var cards = new List<HintCard>();

        // 1. init / origin manquant (Block).
        if (!state.IsRepository)
        {
            cards.Add(new HintCard(
                "init_missing",
                "Pas encore de depot",
                "Ce dossier n'est pas un depot git. Lance d'abord l'initialisation (bouton Init).",
                HintSeverity.Block));
        }
        else if (!state.HasOrigin)
        {
            cards.Add(new HintCard(
                "origin_missing",
                "Aucun origin",
                "Aucun remote « origin » n'est configure : le rendu officiel passe par lui. Lance l'initialisation.",
                HintSeverity.Block));
        }

        // 2. Marqueurs de conflit (Block).
        if (state.ConflictedFiles.Count > 0)
        {
            var files = string.Join(", ", state.ConflictedFiles);
            cards.Add(new HintCard(
                "conflict_markers",
                "Conflit non resolu",
                $"Des marqueurs de conflit (`<<<<<<<`) sont presents dans : {files}. Edite ces fichiers puis « git add ».",
                HintSeverity.Block));
        }

        // 3. HEAD detache (Warn).
        if (state.IsDetachedHead)
        {
            cards.Add(new HintCard(
                "detached_head",
                "HEAD detache",
                "Tu n'es sur aucune branche (HEAD detache). Reviens sur une branche avec « git switch <branche> ».",
                HintSeverity.Warn));
        }

        // 4. Mauvaise branche (Warn).
        if (!string.IsNullOrEmpty(expectation.ExpectedBranch)
            && state.CurrentBranch is not null
            && !string.Equals(state.CurrentBranch, expectation.ExpectedBranch, StringComparison.Ordinal))
        {
            cards.Add(new HintCard(
                "wrong_branch",
                "Mauvaise branche",
                $"Tu es sur « {state.CurrentBranch} », mais l'exo attend « {expectation.ExpectedBranch} ». Bascule avec « git switch {expectation.ExpectedBranch} ».",
                HintSeverity.Warn));
        }

        // 5. « commit » sans rien stage (Warn).
        if (string.Equals(lastCommand?.Subcommand, "commit", StringComparison.Ordinal) && state.StagedCount == 0)
        {
            cards.Add(new HintCard(
                "commit_nothing_staged",
                "Rien a committer",
                "Rien n'est indexe : ajoute tes fichiers avec « git add <fichiers> » avant de committer.",
                HintSeverity.Warn));
        }

        // 6. Typo de sous-commande (Info).
        if (lastCommand is { ExitCode: not 0 } && lastCommand.Subcommand is { } sub
            && !KnownSubcommands.Contains(sub, StringComparer.Ordinal))
        {
            var suggestion = KnownSubcommands.FirstOrDefault(k => Levenshtein(k, sub) == 1);
            if (suggestion is not null)
            {
                cards.Add(new HintCard(
                    "typo_subcommand",
                    "Commande inconnue",
                    $"« git {sub} » ? Voulais-tu dire « git {suggestion} » ?",
                    HintSeverity.Info));
            }
        }

        // 7. Commite mais pas pousse (Info).
        var committed = string.Equals(lastCommand?.Subcommand, "commit", StringComparison.Ordinal)
            && lastCommand!.ExitCode == 0;
        if (committed || state.AheadOfOrigin > 0)
        {
            var branch = state.CurrentBranch ?? "main";
            cards.Add(new HintCard(
                "committed_not_pushed",
                "Pas encore pousse",
                $"Ton travail est committe localement mais non officiel tant que tu n'as pas fait « git push origin {branch} ».",
                HintSeverity.Info));
        }

        // 8. Pousse mais correction KO (Warn).
        if (expectation.GradeReceivedFailed == true)
        {
            cards.Add(new HintCard(
                "grade_received_failed",
                "Correction en echec",
                "Ton rendu est bien arrive, mais la correction signale un souci. Consulte le feedback educatif rendu par l'app.",
                HintSeverity.Warn));
        }

        return cards;
    }

    /// <summary>Distance d'edition de Levenshtein (pure, locale), pour suggerer une sous-commande proche.</summary>
    private static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
