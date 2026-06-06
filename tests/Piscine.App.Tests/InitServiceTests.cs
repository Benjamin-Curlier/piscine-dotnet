using System.IO;
using LibGit2Sharp;
using Piscine.App.Init;
using Piscine.Core;
using Piscine.Git;

namespace Piscine.App.Tests;

/// <summary>
/// Tests unitaires de <see cref="InitService"/> : vierge → initialisé, idempotence,
/// détection de statut, contenu du hook. Chaque test utilise un <see cref="TempDir"/>
/// isolé pour ne jamais toucher ~/piscine réel.
/// </summary>
public sealed class InitServiceTests : IDisposable
{
    private const string Exe = "/usr/local/bin/piscine";

    private readonly TempDir _temp = new();
    private readonly PiscineLayout _layout;
    private readonly InitService _sut;

    public InitServiceTests()
    {
        // contentRoot doit exister mais n'est pas utilisé par InitService.
        var contentRoot = _temp.Combine("content");
        Directory.CreateDirectory(contentRoot);

        var workspaceRoot = _temp.Combine("workspace");
        var stateDir = _temp.Combine(".state");

        _layout = new PiscineLayout(contentRoot, workspaceRoot, stateDir);
        _sut = new InitService(_layout, Exe);
    }

    public void Dispose() => _temp.Dispose();

    // ── Vierge → initialisé ──────────────────────────────────────────────────

    [Fact]
    public void Status_OnBlankLayout_IsInitializedFalse()
    {
        // Arrange — layout vierge, rien n'a été créé

        // Act
        var status = _sut.Status();

        // Assert
        Assert.False(status.IsInitialized);
        Assert.False(status.WorkspaceReady);
        Assert.False(status.BareRepoReady);
        Assert.False(status.HookInstalled);
        Assert.False(status.OriginConfigured);
    }

    [Fact]
    public void Initialize_OnBlankLayout_ReturnsSuccessWithExpectedOutcome()
    {
        // Arrange — layout vierge

        // Act
        var result = _sut.Initialize();

        // Assert — succès
        Assert.True(result.Success);
        Assert.False(result.Before.IsInitialized);
        Assert.True(result.After.IsInitialized);
        Assert.Equal("Environnement initialisé.", result.Message);
        Assert.Null(result.Error);

        // Assert — dépôts réellement créés
        Assert.True(Repository.IsValid(_layout.WorkspaceRoot));
        Assert.True(Repository.IsValid(_layout.RemoteRepoPath));

        // Assert — hook présent
        Assert.True(File.Exists(Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive")));
    }

    // ── Contenu du hook = chemin fourni (jamais Environment.ProcessPath) ─────

    [Fact]
    public void Initialize_HookContent_ContainsGradeReceivedAndExePath()
    {
        // Arrange — layout vierge

        // Act
        _sut.Initialize();

        // Assert
        var hookPath = Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive");
        var content = File.ReadAllText(hookPath);

        Assert.Contains("grade-received", content, StringComparison.Ordinal);
        Assert.Contains(Exe, content, StringComparison.Ordinal);
    }

    // ── Détection de statut après init ───────────────────────────────────────

    [Fact]
    public void Status_AfterInitialize_AllFlagsTrue()
    {
        // Arrange
        _sut.Initialize();

        // Act
        var status = _sut.Status();

        // Assert
        Assert.True(status.WorkspaceReady);
        Assert.True(status.BareRepoReady);
        Assert.True(status.HookInstalled);
        Assert.True(status.OriginConfigured);
        Assert.True(status.IsInitialized);
    }

    // ── Re-run idempotent ─────────────────────────────────────────────────────

    [Fact]
    public void Initialize_CalledTwice_IsIdempotent()
    {
        // Arrange
        _sut.Initialize();

        // Act — deuxième appel
        var result2 = _sut.Initialize();

        // Assert
        Assert.True(result2.Success);
        Assert.True(result2.Before.IsInitialized);
        Assert.Equal("Déjà initialisé.", result2.Message);
        Assert.Null(result2.Error);
    }

    // ── Suppression du hook → IsInitialized repasse à false ──────────────────

    [Fact]
    public void Status_AfterHookDeleted_IsInitializedFalse()
    {
        // Arrange
        _sut.Initialize();
        var hookPath = Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive");
        File.Delete(hookPath);

        // Act
        var status = _sut.Status();

        // Assert — workspace et bare toujours valides, mais hook absent → not initialized
        Assert.True(status.WorkspaceReady);
        Assert.True(status.BareRepoReady);
        Assert.False(status.HookInstalled);
        Assert.False(status.IsInitialized);
    }
}
