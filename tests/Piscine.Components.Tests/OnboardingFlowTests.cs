using System;
using System.Collections.Generic;
using System.IO;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Init;
using Piscine.App.Onboarding;
using Piscine.Components.Components.Onboarding;
using Piscine.Components.Services;
using Piscine.Core;
using Piscine.Git;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du parcours d'onboarding <see cref="OnboardingFlow"/> (S7) : affiché tant que le
/// workspace n'est PAS initialisé, masqué une fois initialisé, transitions d'étapes (Bienvenue → Init
/// → Fait) et CTA vers le 1ᵉʳ exercice. Services réels (InitService + OnboardingState) sur un layout
/// temporaire isolé ; catalogue de contenu du dépôt pour une route de 1ᵉʳ exercice déterministe.
/// </summary>
public sealed class OnboardingFlowTests : BunitContext, IDisposable
{
    private const string Exe = "/x/piscine";

    private readonly string _tempHome;
    private readonly PiscineLayout _layout;
    private readonly InitService _init;

    public OnboardingFlowTests()
    {
        var repoRoot = FindRepoRoot();
        var content = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-bunit-onboarding-{Guid.NewGuid():N}");
        var workspace = Path.Combine(_tempHome, "workspace");
        var state = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(state);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PISCINE_CONTENT"] = content })
            .Build();

        _layout = new PiscineLayout(content, workspace, state);
        _init = new InitService(_layout, Exe);

        // Interop en mode loose : l'import du module de marqueur (markReady) est un no-op au rendu.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(new CourseCatalog(config));
        Services.AddSingleton(_init);
        Services.AddSingleton(new OnboardingState(_init));
    }

    // ── Affiché quand non initialisé ─────────────────────────────────────────

    [Fact]
    public void Shows_welcome_when_workspace_uninitialized()
    {
        var cut = Render<OnboardingFlow>();

        cut.Find("[data-testid='onboarding']");
        cut.Find("[data-testid='onboarding-welcome']");
        // Première étape : pas encore d'écran d'init ni de fin.
        Assert.Empty(cut.FindAll("[data-testid='onboarding-init']"));
        Assert.Empty(cut.FindAll("[data-testid='onboarding-done']"));
    }

    // ── Masqué quand déjà initialisé (pas de harcèlement) ────────────────────

    [Fact]
    public void Hidden_when_workspace_already_initialized()
    {
        // Arrange — workspace réellement initialisé avant le rendu.
        GitWorkspace.Initialize(_layout, Exe);

        var cut = Render<OnboardingFlow>();

        Assert.Empty(cut.FindAll("[data-testid='onboarding']"));
    }

    // ── Transition Bienvenue → Init ──────────────────────────────────────────

    [Fact]
    public void Start_transitions_from_welcome_to_init_step()
    {
        var cut = Render<OnboardingFlow>();

        cut.Find("[data-testid='onboarding-start']").Click();

        cut.Find("[data-testid='onboarding-init']");
        cut.Find("[data-testid='onboarding-run-init']");
        Assert.Empty(cut.FindAll("[data-testid='onboarding-welcome']"));
    }

    // ── Init réussie → étape « Fait » + CTA 1ᵉʳ exercice ─────────────────────

    [Fact]
    public void Running_init_succeeds_and_reaches_done_with_first_exercise_link()
    {
        var cut = Render<OnboardingFlow>();
        cut.Find("[data-testid='onboarding-start']").Click();

        cut.Find("[data-testid='onboarding-run-init']").Click();

        // RunInit s'exécute via Task.Run → attendre la re-render de l'étape finale.
        cut.WaitForAssertion(() => cut.Find("[data-testid='onboarding-done']"));

        // Le workspace est désormais réellement initialisé.
        Assert.True(_init.Status().IsInitialized);

        // CTA vers un exercice réel du curriculum (/module/<mod>/<exo>) ou repli /cours.
        var cta = cut.Find("[data-testid='onboarding-first-exercise']");
        var href = cta.GetAttribute("href");
        Assert.False(string.IsNullOrWhiteSpace(href));
        Assert.StartsWith("/module/", href, StringComparison.Ordinal);
    }

    // ── « Plus tard » ferme l'overlay ────────────────────────────────────────

    [Fact]
    public void Skip_dismisses_overlay()
    {
        var cut = Render<OnboardingFlow>();

        cut.Find("[data-testid='onboarding-skip']").Click();

        Assert.Empty(cut.FindAll("[data-testid='onboarding']"));
    }

    // ── Accessibilité : Escape (relayé par le module JS) ferme la modale ──────

    [Fact]
    public async Task Escape_entrypoint_dismisses_overlay()
    {
        var cut = Render<OnboardingFlow>();
        cut.Find("[data-testid='onboarding']"); // visible au départ

        // Point d'entrée invoqué par le module JS quand la recrue presse Échap dans la modale.
        await cut.InvokeAsync(() => cut.Instance.DismissFromJs());

        Assert.Empty(cut.FindAll("[data-testid='onboarding']"));
    }

    public new void Dispose()
    {
        base.Dispose();
        try
        {
            if (Directory.Exists(_tempHome))
            {
                foreach (var f in Directory.EnumerateFiles(_tempHome, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(f, FileAttributes.Normal);
                }
                Directory.Delete(_tempHome, recursive: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Racine du dépôt introuvable (Piscine.slnx absent).");
    }
}
