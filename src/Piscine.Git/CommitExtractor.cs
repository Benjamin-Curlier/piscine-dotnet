using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace Piscine.Git;

/// <summary>Matérialise l'arbre d'un commit (reçu par push) dans un dossier de travail.</summary>
public static class CommitExtractor
{
    /// <summary>
    /// Renvoie les chemins dont l'arbre a changé entre deux commits. Le hook s'en sert pour ne
    /// recorriger que les exercices présents dans le rendu courant au lieu de recompter tout
    /// l'instantané du dépôt à chaque push.
    /// </summary>
    public static IReadOnlySet<string> ChangedPaths(string repoPath, string oldSha, string newSha)
    {
        using var repo = new Repository(repoPath);
        var oldCommit = repo.Lookup<Commit>(oldSha)
            ?? throw new ArgumentException($"Commit introuvable : {oldSha}", nameof(oldSha));
        var newCommit = repo.Lookup<Commit>(newSha)
            ?? throw new ArgumentException($"Commit introuvable : {newSha}", nameof(newSha));

        using var changes = repo.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree);
        return changes
            .SelectMany(change => new[] { change.Path, change.OldPath })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static void Extract(string repoPath, string sha, string destinationDir)
    {
        using var repo = new Repository(repoPath);
        if (repo.Lookup<Commit>(sha) is not { } commit)
        {
            throw new ArgumentException($"Commit introuvable : {sha}", nameof(sha));
        }

        Directory.CreateDirectory(destinationDir);
        var root = Path.GetFullPath(destinationDir);
        WriteTree(commit.Tree, root, root);
    }

    private static void WriteTree(Tree tree, string dir, string root)
    {
        Directory.CreateDirectory(dir);
        foreach (var entry in tree)
        {
            // La recrue contrôle entièrement le dépôt bare lu ici : un nom d'entrée malveillant
            // (séparateur, « .. », chemin absolu) ferait écrire File.Create HORS du dossier snapshot.
            // On valide chaque nom et on confirme que la cible reste sous la racine.
            var target = ResolveSafeChild(root, dir, entry.Name);
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
                    WriteTree((Tree)entry.Target, target, root);
                    break;
            }
        }
    }

    /// <summary>
    /// Résout un enfant (nom d'entrée d'arbre) sous <paramref name="parentDir"/> en garantissant qu'il
    /// reste à l'intérieur de <paramref name="root"/>. Rejette les noms non atomiques (séparateur,
    /// « . »/« .. », chemin enraciné) et toute cible qui échapperait au snapshot.
    /// </summary>
    internal static string ResolveSafeChild(string root, string parentDir, string name)
    {
        if (string.IsNullOrEmpty(name) || name is "." or ".."
            || name.IndexOfAny(new[] { '/', '\\' }) >= 0
            || Path.IsPathRooted(name))
        {
            throw new InvalidOperationException($"Nom d'entrée d'arbre git invalide (traversal) : « {name} ».");
        }

        var rootFull = Path.GetFullPath(root);
        var target = Path.GetFullPath(Path.Combine(parentDir, name));
        var relative = Path.GetRelativePath(rootFull, target);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"Entrée d'arbre git hors du dossier cible : « {name} ».");
        }

        return target;
    }
}
