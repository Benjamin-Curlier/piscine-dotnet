using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>git</c> : la recrue rend un dépôt git ; le manifest décrit l'état attendu
/// (branches présentes, nombre de commits, fusions, contenu de fichiers, absence de marqueurs de
/// conflit). Le grader inspecte le dépôt via LibGit2Sharp et rend un verdict éducatif unique.
/// </summary>
public sealed class GitGrader : IGrader
{
    public string Type => "git";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        if (step.Git is null)
        {
            return GraderResult.Failure(Type, "contenu : étape git sans bloc « git » d'assertions.");
        }

        if (string.IsNullOrEmpty(context.RepositoryPath) || !Repository.IsValid(context.RepositoryPath))
        {
            return GraderResult.Failure(Type, "Aucun dépôt git valide n'a été rendu.")
                .WithTrigger(FeedbackTriggers.GitState);
        }

        var failures = new List<string>();
        using (var repo = new Repository(context.RepositoryPath))
        {
            // HEAD effectif : la branche de rendu côté dépôt bare (HeadRef), sinon le HEAD réel du
            // dépôt (check local + fixture). Permet de noter un bare dont le HEAD est orphelin.
            var head = ResolveHead(repo, context.HeadRef);
            CheckBranches(repo, step.Git, failures);
            CheckCommitCount(repo, step.Git, head, failures);
            CheckMerges(repo, step.Git, failures);
            CheckFiles(repo, step.Git, failures);
            CheckConflictMarkers(repo, step.Git, head, failures);
        }

        if (failures.Count == 0)
        {
            return GraderResult.Success(Type);
        }

        var messages = new List<string> { "L'état de ton dépôt git ne correspond pas à l'attendu :" };
        messages.AddRange(failures.Select(f => $"- {f}"));
        return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.GitState);
    }

    private static void CheckBranches(Repository repo, GitAssertions git, List<string> failures)
    {
        foreach (var name in git.Branches)
        {
            if (repo.Branches[name] is null)
            {
                failures.Add($"la branche « {name} » est absente.");
            }
        }
    }

    private static void CheckCommitCount(Repository repo, GitAssertions git, Commit? head, List<string> failures)
    {
        if (git.MinCommits <= 0)
        {
            return;
        }

        if (head is null)
        {
            failures.Add($"au moins {git.MinCommits} commit(s) attendu(s), mais le dépôt n'a aucun commit.");
            return;
        }

        // On ne walk l'historique que jusqu'au seuil : inutile de parcourir un long passé.
        var filter = new CommitFilter { IncludeReachableFrom = head };
        var count = repo.Commits.QueryBy(filter).Take(git.MinCommits).Count();
        if (count < git.MinCommits)
        {
            failures.Add($"au moins {git.MinCommits} commit(s) attendu(s) sur HEAD, trouvé {count}.");
        }
    }

    private static void CheckMerges(Repository repo, GitAssertions git, List<string> failures)
    {
        foreach (var merge in git.Merged)
        {
            var into = ResolveCommit(repo, merge.Into);
            var branch = ResolveCommit(repo, merge.Branch);
            if (into is null)
            {
                failures.Add($"fusion : la branche cible « {merge.Into} » est introuvable.");
                continue;
            }

            if (branch is null)
            {
                failures.Add($"fusion : la branche « {merge.Branch} » est introuvable.");
                continue;
            }

            // « branch » est fusionnée dans « into » si la pointe de branch est un ancêtre de into.
            var mergeBase = repo.ObjectDatabase.FindMergeBase(into, branch);
            if (mergeBase is null || mergeBase.Sha != branch.Sha)
            {
                failures.Add($"fusion : « {merge.Branch} » n'est pas fusionnée dans « {merge.Into} ».");
            }
        }
    }

    private static void CheckFiles(Repository repo, GitAssertions git, List<string> failures)
    {
        foreach (var file in git.Files)
        {
            var commit = ResolveCommit(repo, file.Ref);
            if (commit is null)
            {
                failures.Add($"fichier « {file.Path} » : la ref « {file.Ref} » est introuvable.");
                continue;
            }

            var entry = commit[file.Path];
            if (entry?.Target is not Blob blob)
            {
                failures.Add($"le fichier « {file.Path} » est absent dans « {file.Ref} ».");
                continue;
            }

            string content;
            try
            {
                content = blob.GetContentText();
            }
            catch (Exception)
            {
                failures.Add($"le fichier « {file.Path} » n'est pas un fichier texte lisible.");
                continue;
            }

            if (!string.IsNullOrEmpty(file.Contains) && !content.Contains(file.Contains, StringComparison.Ordinal))
            {
                failures.Add($"le fichier « {file.Path} » ne contient pas « {file.Contains} ».");
            }

            if (!string.IsNullOrEmpty(file.Content)
                && Normalize(content) != Normalize(file.Content))
            {
                failures.Add($"le contenu du fichier « {file.Path} » ne correspond pas à l'attendu.");
            }
        }
    }

    private static void CheckConflictMarkers(Repository repo, GitAssertions git, Commit? head, List<string> failures)
    {
        if (!git.NoConflictMarkers)
        {
            return;
        }

        if (head is null)
        {
            return;
        }

        foreach (var (path, text) in EnumerateBlobs(head))
        {
            // Un vrai conflit git laisse les trois marqueurs en début de ligne. Exiger les trois
            // (en début de ligne) évite les faux positifs (art ASCII, doc parlant des conflits, diff).
            if (HasMarkerAtLineStart(text, "<<<<<<<")
                && HasMarkerAtLineStart(text, "=======")
                && HasMarkerAtLineStart(text, ">>>>>>>"))
            {
                failures.Add($"le fichier « {path} » contient des marqueurs de conflit non résolus.");
            }
        }
    }

    /// <summary>Vrai si <paramref name="marker"/> apparaît en début de ligne (début du texte ou après un <c>\n</c>).</summary>
    private static bool HasMarkerAtLineStart(string text, string marker)
    {
        if (text.StartsWith(marker, StringComparison.Ordinal))
        {
            return true;
        }

        return text.Contains("\n" + marker, StringComparison.Ordinal);
    }

    /// <summary>
    /// HEAD effectif du dépôt : la pointe de <paramref name="headRef"/> quand il est fourni (dépôt bare
    /// côté <c>grade-received</c>), sinon le HEAD réel (<c>repo.Head</c>, check local + fixture).
    /// </summary>
    private static Commit? ResolveHead(Repository repo, string? headRef)
        => string.IsNullOrEmpty(headRef) ? repo.Head?.Tip : repo.Branches[headRef]?.Tip;

    /// <summary>Résout une ref (branche, <c>HEAD</c>, ou sha) vers son commit, ou <c>null</c>.</summary>
    private static Commit? ResolveCommit(Repository repo, string refName)
    {
        if (string.IsNullOrEmpty(refName) || refName == "HEAD")
        {
            return repo.Head?.Tip;
        }

        var branch = repo.Branches[refName];
        if (branch?.Tip is not null)
        {
            return branch.Tip;
        }

        return repo.Lookup<Commit>(refName);
    }

    /// <summary>Parcourt tous les blobs texte de l'arbre d'un commit (chemin, contenu).</summary>
    private static IEnumerable<(string Path, string Text)> EnumerateBlobs(Commit commit)
    {
        var stack = new Stack<Tree>();
        stack.Push(commit.Tree);
        while (stack.Count > 0)
        {
            foreach (var entry in stack.Pop())
            {
                switch (entry.TargetType)
                {
                    case TreeEntryTargetType.Tree:
                        stack.Push((Tree)entry.Target);
                        break;
                    case TreeEntryTargetType.Blob:
                        var blob = (Blob)entry.Target;
                        if (blob.IsBinary)
                        {
                            break;
                        }

                        string text;
                        try
                        {
                            text = blob.GetContentText();
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        yield return (entry.Path, text);
                        break;
                }
            }
        }
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n");
}
