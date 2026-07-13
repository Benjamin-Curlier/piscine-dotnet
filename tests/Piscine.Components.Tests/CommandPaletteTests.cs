using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Search;
using Piscine.Components.Components;
using Piscine.Components.Services;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit de la palette de commande <see cref="CommandPalette"/> : ouverture (via le point
/// d'entrée JS-invokable), filtrage des résultats, état vide et activation d'un résultat (navigation).
/// L'interop JS (hotkey global + focus) est mise en mode « loose » : les imports/appels de module sont
/// des no-op, ce qui isole la logique C# de filtrage/navigation.
/// </summary>
public sealed class CommandPaletteTests : BunitContext
{
    private static readonly IReadOnlyList<SearchCommand> Index =
    [
        new SearchCommand(SearchKind.Destination, "Tableau de bord", "Navigation", "/", "cmd-nav-dashboard"),
        new SearchCommand(SearchKind.Destination, "Progression", "Navigation", "/progress", "cmd-nav-progress"),
        new SearchCommand(SearchKind.Action, "Vérifier l'exercice", "Lancer la correction", "/check", "cmd-action-check", Keywords: ["check"]),
        new SearchCommand(SearchKind.Exercise, "Bonjour le monde", "01 · ex00-hello", "/module/01-bases/ex00-hello",
            "cmd-exo-01-bases-ex00-hello", Keywords: ["ex00-hello"]),
    ];

    private void ConfigureServices()
    {
        JSInterop.Mode = JSRuntimeMode.Loose; // import + appels de module = no-op
        Services.AddSingleton(new SearchService(Index));
        // CourseCatalog (utilisé par le raccourci exo suivant/précédent) : résolu depuis le content/ du dépôt.
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<CourseCatalog>();
    }

    [Fact]
    public void Closed_by_default_renders_nothing()
    {
        ConfigureServices();

        var cut = Render<CommandPalette>();

        Assert.Empty(cut.FindAll("[data-testid='command-palette']"));
    }

    [Fact]
    public async Task Opens_via_js_entrypoint_and_lists_all_commands()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();

        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        Assert.NotNull(cut.Find("[data-testid='command-palette']"));
        // Requête vide → tout l'index est listé.
        Assert.Equal(Index.Count, cut.FindAll("[data-testid='command-palette-results'] .cmdk-item").Count);
    }

    [Fact]
    public async Task Typing_filters_results()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        cut.Find("[data-testid='command-palette-input']").Input("prog");

        var items = cut.FindAll("[data-testid='command-palette-results'] .cmdk-item");
        Assert.Single(items);
        Assert.NotNull(cut.Find("[data-testid='cmd-nav-progress']"));
    }

    [Fact]
    public async Task No_match_shows_empty_state()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        cut.Find("[data-testid='command-palette-input']").Input("zzzznotfound");

        Assert.NotNull(cut.Find("[data-testid='command-palette-empty']"));
        Assert.Empty(cut.FindAll("[data-testid='command-palette-results']"));
    }

    [Fact]
    public async Task Filter_is_accent_insensitive()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        cut.Find("[data-testid='command-palette-input']").Input("verifier");

        Assert.NotNull(cut.Find("[data-testid='cmd-action-check']"));
    }

    [Fact]
    public async Task Activating_a_result_navigates_and_closes()
    {
        ConfigureServices();
        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        cut.Find("[data-testid='cmd-nav-progress']").Click();

        Assert.EndsWith("/progress", nav.Uri, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='command-palette']")); // refermée après activation
    }

    // ── Accessibilité : sémantique combobox/listbox/option (WAI-ARIA) ─────────

    [Fact]
    public async Task Input_and_list_expose_combobox_listbox_semantics()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        var input = cut.Find("[data-testid='command-palette-input']");
        Assert.Equal("combobox", input.GetAttribute("role"));
        Assert.Equal("true", input.GetAttribute("aria-expanded"));
        Assert.Equal("cmdk-listbox", input.GetAttribute("aria-controls"));
        // Le 1ᵉʳ résultat est actif par défaut → aria-activedescendant pointe son id.
        Assert.Equal("cmdk-option-0", input.GetAttribute("aria-activedescendant"));

        var list = cut.Find("[data-testid='command-palette-results']");
        Assert.Equal("listbox", list.GetAttribute("role"));
        Assert.Equal("cmdk-listbox", list.GetAttribute("id"));

        var options = cut.FindAll("[data-testid='command-palette-results'] .cmdk-item");
        Assert.All(options, o => Assert.Equal("option", o.GetAttribute("role")));
        Assert.All(options, o => Assert.Equal("-1", o.GetAttribute("tabindex")));
        // Seule l'option sélectionnée (la 1ʳᵉ) porte aria-selected="true".
        Assert.Equal("cmdk-option-0", options[0].GetAttribute("id"));
        Assert.Equal("true", options[0].GetAttribute("aria-selected"));
        Assert.All(options.Skip(1), o => Assert.Equal("false", o.GetAttribute("aria-selected")));
    }

    [Fact]
    public async Task Empty_state_collapses_combobox()
    {
        ConfigureServices();
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenFromJs());

        cut.Find("[data-testid='command-palette-input']").Input("zzzznotfound");

        var input = cut.Find("[data-testid='command-palette-input']");
        Assert.Equal("false", input.GetAttribute("aria-expanded"));
        // Aucune liste rendue → aria-controls / aria-activedescendant omis (pas de cible fantôme).
        Assert.Null(input.GetAttribute("aria-controls"));
        Assert.Null(input.GetAttribute("aria-activedescendant"));
    }
}
