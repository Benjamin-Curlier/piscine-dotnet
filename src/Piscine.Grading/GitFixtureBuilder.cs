using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Construit un dépôt git de **fixture** à partir d'un scénario déclaratif (commits, branches,
/// fusions), via LibGit2Sharp. Sert au gate <c>validate-content</c> pour matérialiser le « corrigé »
/// d'un exercice <c>git</c> (qui n'a pas de dossier <c>solution/</c>) et le confronter aux assertions.
/// </summary>
public static class GitFixtureBuilder
{
    private static readonly Signature Author = new("Fixture", "fixture@piscine.dev", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    /// <summary>Construit le dépôt décrit par <paramref name="steps"/> dans <paramref name="directory"/> (déjà créé).</summary>
    public static void Build(IReadOnlyList<GitFixtureStep> steps, string directory)
    {
        Repository.Init(directory);
        using var repo = new Repository(directory);

        foreach (var step in steps)
        {
            if (!string.IsNullOrEmpty(step.MergeInto) || !string.IsNullOrEmpty(step.MergeFrom))
            {
                Merge(repo, step);
            }
            else if (!string.IsNullOrEmpty(step.Message))
            {
                Commit(repo, step);
            }
            else if (!string.IsNullOrEmpty(step.Branch) && !string.IsNullOrEmpty(step.Base))
            {
                repo.CreateBranch(step.Branch, Tip(repo, step.Base));
            }
            else
            {
                throw new InvalidOperationException("étape de fixture git incomplète (ni commit, ni branche, ni merge).");
            }
        }
    }

    private static void Commit(Repository repo, GitFixtureStep step)
    {
        EnsureOnBranch(repo, step.Branch, step.Base);

        foreach (var (relativePath, content) in step.Files)
        {
            var full = Path.Combine(repo.Info.WorkingDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        Commands.Stage(repo, "*");
        repo.Commit(step.Message, Author, Author);
    }

    private static void Merge(Repository repo, GitFixtureStep step)
    {
        Commands.Checkout(repo, repo.Branches[step.MergeInto]);
        repo.Merge(repo.Branches[step.MergeFrom], Author);
    }

    /// <summary>Place HEAD sur <paramref name="branch"/> : la crée si besoin (branche orpheline initiale ou depuis <paramref name="baseBranch"/>).</summary>
    private static void EnsureOnBranch(Repository repo, string branch, string baseBranch)
    {
        if (repo.Branches[branch] is { } existing)
        {
            Commands.Checkout(repo, existing);
            return;
        }

        if (repo.Head.Tip is null)
        {
            // Dépôt sans commit : pointer HEAD vers la branche voulue avant le 1er commit.
            repo.Refs.UpdateTarget("HEAD", "refs/heads/" + branch);
            return;
        }

        // Nouvelle branche à partir d'une base existante.
        var created = repo.CreateBranch(branch, Tip(repo, baseBranch));
        Commands.Checkout(repo, created);
    }

    private static Commit Tip(Repository repo, string branch)
    {
        var b = repo.Branches[branch]
            ?? throw new InvalidOperationException($"fixture git : branche de base « {branch} » introuvable.");
        return b.Tip;
    }
}
