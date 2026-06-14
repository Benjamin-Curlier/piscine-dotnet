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
}
