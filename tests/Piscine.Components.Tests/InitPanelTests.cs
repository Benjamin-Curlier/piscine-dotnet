using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Init;
using Piscine.Components.Components.Init;
using Piscine.Core;
using Piscine.Git;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="InitPanel"/> : statut initial (layout vierge et
/// déjà initialisé) + clic sur le bouton d'initialisation. <see cref="InitService"/> est
/// injecté sur un <see cref="PiscineLayout"/> pointant un dossier temporaire réel (pas de
/// mock) pour vérifier l'intégration complète.
/// </summary>
public sealed class InitPanelTests : BunitContext, IDisposable
{
    // ── Helper TempDir local ─────────────────────────────────────────────────

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "piscine-bunit", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public string Combine(string rel) => System.IO.Path.Combine(Path, rel);

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    foreach (var f in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                        File.SetAttributes(f, FileAttributes.Normal);
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    private readonly TempDir _temp = new();

    new public void Dispose()
    {
        base.Dispose();
        _temp.Dispose();
    }

    private PiscineLayout CreateLayout()
    {
        var content = _temp.Combine("content");
        Directory.CreateDirectory(content);
        var workspace = _temp.Combine("workspace");
        var state = _temp.Combine(".state");
        return new PiscineLayout(content, workspace, state);
    }

    // ── Layout vierge → data-initialized="False" ─────────────────────────────

    [Fact]
    public void Render_BlankLayout_ShowsNotInitializedAndEnabledButton()
    {
        // Arrange
        var layout = CreateLayout();
        Services.AddSingleton(new InitService(layout, "/x/piscine"));

        // Act
        var cut = Render<InitPanel>();

        // Assert — statut "False"
        var status = cut.Find("[data-testid='init-status']");
        Assert.Equal("False", status.GetAttribute("data-initialized"));

        // Assert — bouton présent et pas dans un état disabled=true
        var btn = cut.Find("[data-testid='run-init']");
        Assert.NotNull(btn);
        // Blazor n'émet pas l'attribut disabled quand _running=false
        Assert.Null(btn.GetAttribute("disabled"));
    }

    // ── Layout déjà initialisé → data-initialized="True" ─────────────────────

    [Fact]
    public void Render_AlreadyInitialized_ShowsInitializedTrue()
    {
        // Arrange — initialiser avant le rendu
        var layout = CreateLayout();
        GitWorkspace.Initialize(layout, "/x/piscine");
        Services.AddSingleton(new InitService(layout, "/x/piscine"));

        // Act
        var cut = Render<InitPanel>();

        // Assert
        var status = cut.Find("[data-testid='init-status']");
        Assert.Equal("True", status.GetAttribute("data-initialized"));
    }

    // ── Clic bouton sur layout vierge → résultat succès + statut True ─────────

    [Fact]
    public void Click_RunInit_OnBlankLayout_ShowsSuccessResultAndUpdatesStatus()
    {
        // Arrange
        var layout = CreateLayout();
        Services.AddSingleton(new InitService(layout, "/x/piscine"));
        var cut = Render<InitPanel>();

        // Act
        cut.Find("[data-testid='run-init']").Click();

        // bUnit lance RunInit sur le pool via Task.Run ; WaitForAssertion attend la re-render.
        cut.WaitForAssertion(() =>
        {
            // Assert — résultat présent avec succès
            var result = cut.Find("[data-testid='init-result']");
            Assert.Equal("True", result.GetAttribute("data-success"));
            Assert.Contains("Environnement initialisé", result.TextContent, StringComparison.Ordinal);
        });

        // Assert — statut repasse à True
        var status = cut.Find("[data-testid='init-status']");
        Assert.Equal("True", status.GetAttribute("data-initialized"));

        // Assert — pas d'erreur affichée
        Assert.Empty(cut.FindAll("[data-testid='init-error']"));
    }
}
