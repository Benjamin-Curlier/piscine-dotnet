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
    public static bool IsAttempted(GitAttempt? attempt, string? repositoryPath)
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

        return attempt.File is { } file && FileExists(repo, file);
    }

    private static bool FileExists(Repository repo, GitFileAssertion file)
    {
        var commit = ResolveCommit(repo, file.Ref);
        return commit?[file.Path]?.Target is Blob;
    }

    /// <summary>
    /// Résout une ref (branche, <c>HEAD</c>, ou sha) vers son commit, ou <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Divergence assumée avec <c>GitGrader.ResolveHead</c> : ici le ref implicite <c>HEAD</c> se
    /// résout sur <c>repo.Head?.Tip</c>, alors que le grader prend le <c>headRef</c> fourni par
    /// l'appelant (la branche de rendu du dépôt **bare** côté <c>grade-received</c>). Sans impact sur
    /// le contenu actuel : le seul exo git déclare son <c>attempt</c> par <c>branch</c>, et les
    /// prédicats <c>File</c> du contenu portent toujours un <c>Ref</c> explicite (jamais le défaut
    /// <c>HEAD</c>). Le cas à risque — un <c>attempt.File</c> au ref par défaut évalué contre un bare
    /// au HEAD orphelin — donnerait un faux « non tenté ». L'alignement propre consisterait à propager
    /// un <c>headRef</c> optionnel depuis <c>GradeReceivedCommand</c> (<c>RenduBranch</c>) jusqu'ici,
    /// comme le fait déjà le grader. Non fait ici : l'appelant est hors du périmètre de ce lot
    /// (cf. incomplete).
    /// </remarks>
    private static Commit? ResolveCommit(Repository repo, string refName)
    {
        if (string.IsNullOrEmpty(refName) || refName == "HEAD")
        {
            return repo.Head?.Tip;
        }

        var branch = repo.Branches[refName];
        return branch?.Tip ?? repo.Lookup<Commit>(refName);
    }
}
