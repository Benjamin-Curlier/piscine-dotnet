using Microsoft.Extensions.Configuration;
using Piscine.App.Search;
using Piscine.Components.Services;

namespace Piscine.Components.Tests;

public sealed class SearchIndexBuilderTests
{
    [Fact]
    public void Build_indexes_rush_title_subject_tags_and_route()
    {
        var catalog = CreateCatalog();
        var index = SearchIndexBuilder.Build(catalog);
        var rush = Assert.Single(index, command => command.TestId == "cmd-rush-r7-poste-entreprise");

        Assert.Equal(SearchKind.Rush, rush.Kind);
        Assert.Equal("/rush/r7-poste-entreprise", rush.Route);
        Assert.Contains("grpc", rush.Keywords!, StringComparer.OrdinalIgnoreCase);

        var results = new SearchService(index).Search("poste déconnecté");
        Assert.Contains(results, result => result.Command.TestId == rush.TestId);
    }

    private static CourseCatalog CreateCatalog()
    {
        var root = FindRepoRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PISCINE_CONTENT"] = Path.Combine(root, "content")
            })
            .Build();
        return new CourseCatalog(configuration);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Piscine.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Racine du dépôt introuvable.");
    }
}
