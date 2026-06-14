using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace Piscine.App.Git;

/// <summary>
/// Derive l'<see cref="RepoState"/> d'un dossier de travail via LibGit2Sharp, en
/// <b>lecture seule</b> (aucune ecriture, aucun cache). Sans etat partage : enregistrable
/// en singleton et appelable apres chaque commande git pour rafraichir le panneau de statut.
/// </summary>
public sealed class GitStatusService
{
    private const string OriginName = "origin";

    /// <summary>Lit l'etat courant du depot situe dans <paramref name="workingDirectory"/>.</summary>
    public RepoState Read(string workingDirectory)
    {
        if (string.IsNullOrEmpty(workingDirectory) || !Repository.IsValid(workingDirectory))
        {
            return new RepoState { IsRepository = false };
        }

        using var repo = new Repository(workingDirectory);

        // Un seul RetrieveStatus par lecture : il est réutilisé pour les compteurs ET le scan de
        // marqueurs de conflit (appelé à chaque événement git — éviter de le calculer deux fois).
        var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
        var stagedCount = status.Staged.Count() + status.Added.Count() + status.Removed.Count();
        var unstagedCount = status.Modified.Count() + status.Missing.Count();
        var untrackedCount = status.Untracked.Count();

        var isDetached = repo.Info.IsHeadDetached;
        var head = repo.Head;
        var hasAnyCommit = head.Tip is not null;
        // Branche connue uniquement si HEAD est sur une branche avec au moins un commit.
        var currentBranch = isDetached || !hasAnyCommit ? null : head.FriendlyName;

        var hasOrigin = repo.Network.Remotes[OriginName] is not null;
        var (aheadOfOrigin, aheadPaths) = ComputeAhead(repo, currentBranch, head);

        // Identite git effective (lecture seule) : sert d'en-tete a la page de rapport.
        var userName = repo.Config.Get<string>("user.name")?.Value;
        var userEmail = repo.Config.Get<string>("user.email")?.Value;

        var conflicted = CollectConflictedFiles(repo, status);

        // Attribution par exercice : chemins (relatifs, séparateur /) ayant du travail non committé.
        var uncommittedPaths = status
            .Where(e => !e.State.HasFlag(FileStatus.Ignored))
            .Select(e => e.FilePath)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new RepoState
        {
            IsRepository = true,
            CurrentBranch = currentBranch,
            UserName = userName,
            UserEmail = userEmail,
            IsDetachedHead = isDetached,
            HasAnyCommit = hasAnyCommit,
            StagedCount = stagedCount,
            UnstagedCount = unstagedCount,
            UntrackedCount = untrackedCount,
            HasOrigin = hasOrigin,
            AheadOfOrigin = aheadOfOrigin,
            ConflictedFiles = conflicted,
            UncommittedPaths = uncommittedPaths,
            AheadPaths = aheadPaths,
        };
    }

    /// <summary>
    /// Commits atteignables depuis HEAD non atteignables depuis <c>origin/&lt;branche&gt;</c>.
    /// 0 si pas de branche courante, pas de commit, ou pas de pendant distant.
    /// </summary>
    private static (int Count, IReadOnlyList<string> Paths) ComputeAhead(
        Repository repo, string? currentBranch, Branch head)
    {
        if (currentBranch is null || head.Tip is null)
        {
            return (0, []);
        }

        var tracked = repo.Branches[$"{OriginName}/{currentBranch}"];
        if (tracked?.Tip is null)
        {
            return (0, []);
        }

        var filter = new CommitFilter
        {
            IncludeReachableFrom = head.Tip,
            ExcludeReachableFrom = tracked.Tip,
        };
        var count = repo.Commits.QueryBy(filter).Count();
        if (count == 0)
        {
            return (0, []);
        }

        // Chemins nets modifiés entre origin et HEAD : attribue « commité-non-poussé » par exercice.
        var changes = repo.Diff.Compare<TreeChanges>(tracked.Tip.Tree, head.Tip.Tree);
        var paths = changes.Select(c => c.Path).Distinct(StringComparer.Ordinal).ToList();
        return (count, paths);
    }

    /// <summary>
    /// Fichiers en conflit : d'abord l'index (<see cref="Index.Conflicts"/>, signal net) ; en filet,
    /// un scan textuel des fichiers suivis cherchant les trois marqueurs <b>en debut de ligne</b>
    /// (meme detection que <c>GitGrader</c>, pour eviter les faux positifs : art ASCII, doc, diff).
    /// </summary>
    private static IReadOnlyList<string> CollectConflictedFiles(Repository repo, RepositoryStatus status)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var conflict in repo.Index.Conflicts)
        {
            // Ours/Theirs/Ancestor pointent tous le meme chemin ; on prend le premier non nul.
            var path = conflict.Ours?.Path ?? conflict.Theirs?.Path ?? conflict.Ancestor?.Path;
            if (path is not null && seen.Add(path))
            {
                result.Add(path);
            }
        }

        var workdir = repo.Info.WorkingDirectory;
        if (string.IsNullOrEmpty(workdir))
        {
            return result;
        }

        foreach (var entry in status)
        {
            var path = entry.FilePath;
            if (seen.Contains(path))
            {
                continue;
            }

            var full = Path.Combine(workdir, path);
            if (!File.Exists(full))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(full);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (HasMarkerAtLineStart(text, "<<<<<<<")
                && HasMarkerAtLineStart(text, "=======")
                && HasMarkerAtLineStart(text, ">>>>>>>"))
            {
                if (seen.Add(path))
                {
                    result.Add(path);
                }
            }
        }

        return result;
    }

    /// <summary>Vrai si <paramref name="marker"/> apparait en debut de ligne (debut du texte ou apres un <c>\n</c>).</summary>
    private static bool HasMarkerAtLineStart(string text, string marker)
    {
        if (text.StartsWith(marker, StringComparison.Ordinal))
        {
            return true;
        }

        return text.Contains("\n" + marker, StringComparison.Ordinal);
    }
}
