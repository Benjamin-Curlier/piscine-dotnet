using LibGit2Sharp;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Évalue le signal « exercice tenté » d'un exo <c>git</c> contre le dépôt rendu (le **bare** côté
/// <c>grade-received</c>). Sert à ne noter en **live** QUE les exos commencés : sans ce filtre, noter
/// tous les exos git à chaque push produirait des « à revoir » parasites pour les non commencés.
/// </summary>
public static class GitAttemptEvaluator
{
    /// <summary>
    /// Vrai si l'exo est « tenté » : <paramref name="attempt"/> est déclaré ET au moins un de ses
    /// prédicats (branche présente, fichier présent) est satisfait dans le dépôt
    /// <paramref name="repositoryPath"/>. <c>null</c> (ou dépôt invalide) ⇒ <c>false</c> : pas de
    /// notation live sans signal explicite.
    /// </summary>
    public static bool IsAttempted(GitAttempt? attempt, string? repositoryPath, string? headRef = null)
    {
        if (attempt is null
            || string.IsNullOrEmpty(repositoryPath)
            || !Repository.IsValid(repositoryPath))
        {
            return false;
        }

        using var repo = new Repository(repositoryPath);

        if (!string.IsNullOrEmpty(attempt.Branch) && repo.Branches[attempt.Branch] is not null)
        {
            return true;
        }

        return attempt.File is { } file && FileExists(repo, file, headRef);
    }

    private static bool FileExists(Repository repo, GitFileAssertion file, string? headRef)
    {
        var commit = ResolveCommit(repo, file.Ref, headRef);
        return commit?[file.Path]?.Target is Blob;
    }

    /// <summary>
    /// Résout une ref (branche, <c>HEAD</c>, ou sha) vers son commit, ou <c>null</c>.
    /// </summary>
    private static Commit? ResolveCommit(Repository repo, string refName, string? headRef)
    {
        if (string.IsNullOrEmpty(refName) || refName == "HEAD")
        {
            return string.IsNullOrEmpty(headRef) ? repo.Head?.Tip : repo.Branches[headRef]?.Tip;
        }

        var branch = repo.Branches[refName];
        return branch?.Tip ?? repo.Lookup<Commit>(refName);
    }
}
