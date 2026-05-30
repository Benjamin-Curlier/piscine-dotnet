using System;
using System.IO;
using LibGit2Sharp;

namespace Piscine.Git;

/// <summary>Matérialise l'arbre d'un commit (reçu par push) dans un dossier de travail.</summary>
public static class CommitExtractor
{
    public static void Extract(string repoPath, string sha, string destinationDir)
    {
        using var repo = new Repository(repoPath);
        if (repo.Lookup<Commit>(sha) is not { } commit)
        {
            throw new ArgumentException($"Commit introuvable : {sha}", nameof(sha));
        }

        Directory.CreateDirectory(destinationDir);
        WriteTree(commit.Tree, destinationDir);
    }

    private static void WriteTree(Tree tree, string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var entry in tree)
        {
            var target = Path.Combine(dir, entry.Name);
            switch (entry.TargetType)
            {
                case TreeEntryTargetType.Blob:
                    using (var content = ((Blob)entry.Target).GetContentStream())
                    using (var file = File.Create(target))
                    {
                        content.CopyTo(file);
                    }
                    break;
                case TreeEntryTargetType.Tree:
                    WriteTree((Tree)entry.Target, target);
                    break;
            }
        }
    }
}
