using Piscine.App.Terminal;

namespace Piscine.App.Tests;

public sealed class GitPathResolverTests
{
    private static string GitFileName =>
        OperatingSystem.IsWindows() ? "git.exe" : "git";

    [Fact]
    public void Resolve_skips_the_shim_dir_and_returns_the_system_git()
    {
        using var temp = new TempDir();

        // Dossier « shim » EN TETE du PATH, avec un faux git (a exclure).
        var shimDir = Path.GetDirectoryName(temp.WriteFile(Path.Combine("shim", GitFileName), "shim"))!;
        // Dossier « systeme » plus loin, avec le vrai git attendu.
        var systemGit = temp.WriteFile(Path.Combine("system", GitFileName), "system");
        var systemDir = Path.GetDirectoryName(systemGit)!;

        var path = string.Join(Path.PathSeparator, shimDir, systemDir);

        var resolved = GitPathResolver.Resolve(path, shimDir);

        Assert.NotNull(resolved);
        Assert.Equal(
            Path.GetFullPath(systemGit),
            Path.GetFullPath(resolved!));
    }

    [Fact]
    public void Resolve_returns_null_when_no_git_on_path()
    {
        using var temp = new TempDir();

        var emptyA = Path.GetDirectoryName(temp.WriteFile(Path.Combine("a", "notgit.txt"), "x"))!;
        var emptyB = Path.GetDirectoryName(temp.WriteFile(Path.Combine("b", "other.txt"), "x"))!;

        var path = string.Join(Path.PathSeparator, emptyA, emptyB);

        Assert.Null(GitPathResolver.Resolve(path, excludeDir: null));
    }

    [Fact]
    public void Resolve_returns_null_for_empty_path()
    {
        Assert.Null(GitPathResolver.Resolve(string.Empty, excludeDir: null));
    }
}
