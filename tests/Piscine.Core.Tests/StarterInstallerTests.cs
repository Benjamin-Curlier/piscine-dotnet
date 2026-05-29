using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class StarterInstallerTests
{
    [Fact]
    public void Install_CopiesStarterFiles_WithoutOverwritingLearnerWork()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "starter", "README.md"), "consigne");
        dir.WriteFile(Path.Combine("content", "starter", "Hello.cs"), "// squelette");
        // La recrue a déjà commencé Hello.cs : ne pas l'écraser.
        dir.WriteFile(Path.Combine("ws", "Hello.cs"), "mon travail");

        StarterInstaller.Install(dir.Combine("content"), dir.Combine("ws"));

        Assert.Equal("consigne", File.ReadAllText(dir.Combine(Path.Combine("ws", "README.md"))));
        Assert.Equal("mon travail", File.ReadAllText(dir.Combine(Path.Combine("ws", "Hello.cs"))));
    }

    [Fact]
    public void Install_NoStarterDir_DoesNothing()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Combine("content"));

        StarterInstaller.Install(dir.Combine("content"), dir.Combine("ws"));

        Assert.True(Directory.Exists(dir.Combine("ws")));
    }
}
