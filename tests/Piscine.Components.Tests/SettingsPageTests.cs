using System;
using System.IO;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Settings;
using Piscine.Components.Components.Pages;
using Piscine.Core;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit de la page <see cref="Settings"/> (/reglages, S6) : elle se rend, reflète les réglages
/// courants (thème, échelle de police, éditeur, terminal) et expose les contrôles d'enregistrement.
/// JSInterop en mode loose (l'import du module d'interop n'est pas exercé au rendu initial).
/// </summary>
public sealed class SettingsPageTests : BunitContext
{
    private readonly string _tempHome;
    private readonly SettingsService _service;

    public SettingsPageTests()
    {
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-bunit-settings-{Guid.NewGuid():N}");
        var workspace = Path.Combine(_tempHome, "workspace");
        var state = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(state);

        var layout = new PiscineLayout(_tempHome, workspace, state);
        _service = new SettingsService(layout);

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(layout);
        Services.AddSingleton(_service);
    }

    [Fact]
    public void Renders_settings_form_with_all_sections()
    {
        var cut = Render<Settings>();

        cut.Find("[data-testid='settings']");
        cut.Find("[data-testid='settings-theme']");
        cut.Find("[data-testid='settings-fontscale']");
        cut.Find("[data-testid='settings-editor']");
        cut.Find("[data-testid='settings-terminal']");
        cut.Find("[data-testid='settings-save']");
    }

    [Fact]
    public void Defaults_select_system_theme_and_100_percent_scale()
    {
        var cut = Render<Settings>();

        var systemRadio = cut.Find("[data-testid='settings-theme-system'] input");
        Assert.True(systemRadio.HasAttribute("checked"));

        Assert.Contains("100 %", cut.Find("[data-testid='settings-fontscale-value']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Reflects_persisted_settings()
    {
        _service.Save(new AppSettings
        {
            Theme = AppTheme.Dark,
            FontScale = 1.3,
            EditorCommand = "rider",
            DefaultTerminal = TerminalTarget.System,
        });

        var cut = Render<Settings>();

        // Thème sombre coché.
        Assert.True(cut.Find("[data-testid='settings-theme-dark'] input").HasAttribute("checked"));
        // Terminal système coché.
        Assert.True(cut.Find("[data-testid='settings-terminal-system'] input").HasAttribute("checked"));
        // Échelle reflétée (130 %).
        Assert.Contains("130 %", cut.Find("[data-testid='settings-fontscale-value']").TextContent, StringComparison.Ordinal);
        // Commande éditeur reflétée.
        Assert.Equal("rider", cut.Find("[data-testid='settings-editor']").GetAttribute("value"));
    }

    [Fact]
    public void Save_persists_changes_via_service()
    {
        var cut = Render<Settings>();

        // Choisir le thème sombre puis enregistrer.
        cut.Find("[data-testid='settings-theme-dark'] input").Change("Dark");
        cut.Find("[data-testid='settings-save']").Click();

        var loaded = _service.Load();
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        // Le message de confirmation s'affiche.
        cut.Find("[data-testid='settings-saved']");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* nettoyage best-effort */ }
        }
        base.Dispose(disposing);
    }
}
