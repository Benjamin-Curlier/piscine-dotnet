using Piscine.App.Search;
using Xunit;

namespace Piscine.App.Tests;

public sealed class SearchServiceTests
{
    private static SearchService Build() => new(
    [
        new SearchCommand(SearchKind.Destination, "Tableau de bord", "Navigation", "/", "cmd-nav-dashboard", Keywords: ["board"]),
        new SearchCommand(SearchKind.Destination, "Progression", "Navigation", "/progress", "cmd-nav-progress"),
        new SearchCommand(SearchKind.Action, "Vérifier l'exercice", "Lancer la correction", "/check", "cmd-action-check", Keywords: ["check"]),
        new SearchCommand(SearchKind.Module, "01 — Les bases", "Module", "/module/01-bases", "cmd-module-01-bases",
            Body: "Les variables et les types en C#.", Keywords: ["01-bases", "01"]),
        new SearchCommand(SearchKind.Exercise, "Bonjour le monde", "01 · ex00-hello", "/module/01-bases/ex00-hello",
            "cmd-exo-01-bases-ex00-hello", Body: "Affiche bonjour le monde avec Console.WriteLine.", Keywords: ["ex00-hello"]),
    ]);

    [Fact]
    public void Empty_query_returns_all_in_natural_order_destinations_first()
    {
        var results = Build().Search("");

        Assert.Equal(5, results.Count);
        // Les destinations (poids le plus élevé) priment, puis actions, modules, exercices.
        Assert.Equal(SearchKind.Destination, results[0].Command.Kind);
        Assert.Equal(SearchKind.Exercise, results[^1].Command.Kind);
    }

    [Fact]
    public void Prefix_match_on_title_ranks_first()
    {
        var results = Build().Search("Prog");

        Assert.NotEmpty(results);
        Assert.Equal("Progression", results[0].Command.Title);
    }

    [Fact]
    public void Search_is_accent_insensitive()
    {
        // « verifier » sans accent doit retrouver « Vérifier l'exercice ».
        var results = Build().Search("verifier");

        Assert.Contains(results, r => r.Command.Route == "/check");
    }

    [Fact]
    public void Search_is_case_insensitive()
    {
        var lower = Build().Search("bonjour");
        var upper = Build().Search("BONJOUR");

        Assert.Equal(
            lower[0].Command.Route,
            upper[0].Command.Route);
        Assert.Equal("/module/01-bases/ex00-hello", lower[0].Command.Route);
    }

    [Fact]
    public void Keyword_match_finds_command_when_title_does_not()
    {
        // "ex00-hello" n'est pas dans le titre « Bonjour le monde », mais dans les mots-clés.
        var results = Build().Search("ex00-hello");

        Assert.Contains(results, r => r.Command.Route == "/module/01-bases/ex00-hello");
    }

    [Fact]
    public void Fulltext_match_on_body_returns_result_with_snippet()
    {
        // "Console.WriteLine" n'apparaît que dans le corps (sujet) de l'exercice.
        var results = Build().Search("WriteLine");

        var hit = Assert.Single(results, r => r.Command.Route == "/module/01-bases/ex00-hello");
        Assert.NotNull(hit.Snippet);
        Assert.Contains("WriteLine", hit.Snippet!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Title_match_outranks_body_only_match()
    {
        // « monde » est dans le titre de l'exercice « Bonjour le monde » ET dans le corps du module
        // (texte du cours hypothétique). On vérifie que le match de titre prime sur un match de corps.
        var service = new SearchService(
        [
            new SearchCommand(SearchKind.Module, "Premiers pas", "Module", "/module/00-intro", "cmd-module-00",
                Body: "Bienvenue dans le monde du C#."),
            new SearchCommand(SearchKind.Exercise, "Le monde", "00 · ex01", "/module/00-intro/ex01", "cmd-exo-00-ex01",
                Body: "Aucun mot pertinent ici."),
        ]);

        var results = service.Search("monde");

        // L'exercice (match de titre, score ~700+) doit précéder le module (match de corps, ~150).
        Assert.Equal("/module/00-intro/ex01", results[0].Command.Route);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Fulltext_snippet_is_aligned_even_with_accents_before_match()
    {
        // Le corps contient des accents AVANT le terme cherché : si l'extrait était découpé avec un
        // index calculé sur une chaîne accent-supprimée (longueur différente), il serait décalé.
        // On vérifie que l'extrait contient bien le terme recherché, intact.
        var service = new SearchService(
        [
            new SearchCommand(SearchKind.Module, "Module accentué", "Module", "/module/x", "cmd-module-x",
                Body: "Préambule très accentué éàèùçâê puis le mot CIBLE apparaît ici clairement."),
        ]);

        var results = service.Search("cible");

        var hit = Assert.Single(results);
        Assert.NotNull(hit.Snippet);
        Assert.Contains("CIBLE", hit.Snippet!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void No_match_returns_empty()
    {
        var results = Build().Search("zzzzznotfound");

        Assert.Empty(results);
    }

    [Fact]
    public void Subsequence_fuzzy_match_on_title()
    {
        // "prgn" est une sous-séquence de "Progression".
        var results = Build().Search("prgn");

        Assert.Contains(results, r => r.Command.Title == "Progression");
    }

    [Fact]
    public void Limit_caps_the_number_of_results()
    {
        var results = Build().Search("", limit: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Limit_zero_returns_empty()
    {
        Assert.Empty(Build().Search("a", limit: 0));
    }

    [Fact]
    public void Whitespace_query_is_treated_as_empty()
    {
        var results = Build().Search("   ");

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void Null_index_does_not_throw()
    {
        var service = new SearchService(null!);

        Assert.Empty(service.Search("anything"));
        Assert.Empty(service.Index);
    }
}
