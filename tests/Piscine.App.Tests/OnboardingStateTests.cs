using System.IO;
using Piscine.App.Init;
using Piscine.App.Onboarding;
using Piscine.Core;
using Piscine.Git;

namespace Piscine.App.Tests;

/// <summary>
/// Tests de <see cref="OnboardingState"/> (S7) : l'onboarding du 1ᵉʳ lancement s'affiche tant que le
/// workspace n'est PAS initialisé, et disparaît dès qu'il l'est. La décision dérive de
/// <see cref="InitService.Status"/> (aucune persistance dérivée) → on l'exerce sur un layout temporaire réel.
/// </summary>
public sealed class OnboardingStateTests : IDisposable
{
    private const string Exe = "/usr/local/bin/piscine";

    private readonly TempDir _temp = new();
    private readonly PiscineLayout _layout;
    private readonly OnboardingState _sut;
    private readonly InitService _init;

    public OnboardingStateTests()
    {
        var contentRoot = _temp.Combine("content");
        Directory.CreateDirectory(contentRoot);
        _layout = new PiscineLayout(contentRoot, _temp.Combine("workspace"), _temp.Combine(".state"));
        _init = new InitService(_layout, Exe);
        _sut = new OnboardingState(_init);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ShouldShow_OnBlankLayout_IsTrue()
    {
        // Arrange — layout vierge (non initialisé)

        // Act / Assert
        Assert.True(_sut.ShouldShow());
    }

    [Fact]
    public void ShouldShow_AfterInitialize_IsFalse()
    {
        // Arrange — workspace réellement initialisé
        _init.Initialize();

        // Act / Assert — plus de harcèlement une fois prêt
        Assert.False(_sut.ShouldShow());
    }

    [Fact]
    public void ShouldShow_AfterHookRemoved_IsTrueAgain()
    {
        // Arrange — initialisé puis hook supprimé → redevient « non initialisé »
        _init.Initialize();
        File.Delete(Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive"));

        // Act / Assert
        Assert.True(_sut.ShouldShow());
    }
}
