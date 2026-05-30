using System;
using System.IO;
using LibGit2Sharp;
using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class CommitExtractorTests
{
    [Fact]
    public void Extract_MaterializesNestedTreeToDestination()
    {
        using var dir = new TempDir();
        var repoPath = dir.Combine("repo");
        Repository.Init(repoPath);

        dir.WriteFile(Path.Combine("repo", "00-setup", "ex00", "Hello.cs"), "// code");
        dir.WriteFile(Path.Combine("repo", "README.md"), "racine");

        string sha;
        using (var repo = new Repository(repoPath))
        {
            Commands.Stage(repo, "*");
            var sig = new Signature("t", "t@t", DateTimeOffset.Now);
            sha = repo.Commit("c", sig, sig).Sha;
        }

        var outDir = dir.Combine("out");
        CommitExtractor.Extract(repoPath, sha, outDir);

        Assert.Equal("// code", File.ReadAllText(Path.Combine(outDir, "00-setup", "ex00", "Hello.cs")));
        Assert.Equal("racine", File.ReadAllText(Path.Combine(outDir, "README.md")));
    }
}
