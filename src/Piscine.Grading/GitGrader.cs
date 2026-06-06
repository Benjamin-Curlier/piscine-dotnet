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
            CheckBranches(repo, step.Git, failures);
            CheckCommitCount(repo, step.Git, failures);
            CheckMerges(repo, step.Git, failures);
            CheckFiles(repo, step.Git, failures);
            CheckConflictMarkers(repo, step.Git, failures);
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

    private static void CheckCommitCount(Repository repo, GitAssertions git, List<string> failures)
    {
        if (git.MinCommits <= 0)
        {
            return;
        }

        if (repo.Head?.Tip is null)
        {
            failures.Add($"au moins {git.MinCommits} commit(s) attendu(s), mais le dépôt n'a aucun commit.");
            return;
        }

        var count = repo.Commits.Count();
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

            var content = ReadBlobText(commit, file.Path);
            if (content is null)
            {
                failures.Add($"le fichier « {file.Path} » est absent dans « {file.Ref} ».");
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

    private static void CheckConflictMarkers(Repository repo, GitAssertions git, List<string> failures)
    {
        if (!git.NoConflictMarkers)
        {
            return;
        }

        var head = repo.Head?.Tip;
        if (head is null)
        {
            return;
        }

        foreach (var (path, text) in EnumerateBlobs(head))
        {
            if (text.Contains("<<<<<<<", StringComparison.Ordinal)
                && text.Contains(">>>>>>>", StringComparison.Ordinal))
            {
                failures.Add($"le fichier « {path} » contient des marqueurs de conflit non résolus.");
            }
        }
    }

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

    /// <summary>Lit le texte d'un blob à un chemin donné dans un commit, ou <c>null</c> s'il est absent.</summary>
    private static string? ReadBlobText(Commit commit, string path)
    {
        var entry = commit[path];
        if (entry?.Target is not Blob blob)
        {
            return null;
        }

        try
        {
            return blob.GetContentText();
        }
        catch (Exception)
        {
            return null;
        }
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
