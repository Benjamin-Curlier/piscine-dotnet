using System.Linq;
using Piscine.Components.Navigation;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class NavDestinationsTests
{
    [Fact]
    public void Primary_lists_destinations_in_expected_order()
    {
        var routes = NavDestinations.Primary.Select(d => d.Route).ToArray();
        Assert.Equal(
            new[] { "/", "/cours", "/progress", "/rapport", "/check", "/init", "/resultat", "/terminal" },
            routes);
    }

    [Fact]
    public void Every_destination_has_label_and_testid()
    {
        Assert.All(NavDestinations.Primary, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Label));
            Assert.False(string.IsNullOrWhiteSpace(d.TestId));
        });
    }

    [Fact]
    public void IsActive_root_matches_only_empty_path()
    {
        var dashboard = NavDestinations.Primary.First(d => d.Route == "/");
        Assert.True(NavDestinations.IsActive(dashboard, ""));
        Assert.False(NavDestinations.IsActive(dashboard, "cours"));
    }

    [Fact]
    public void IsActive_matches_first_segment_only()
    {
        var cours = NavDestinations.Primary.First(d => d.Route == "/cours");
        Assert.True(NavDestinations.IsActive(cours, "cours"));
        var terminal = NavDestinations.Primary.First(d => d.Route == "/terminal");
        Assert.True(NavDestinations.IsActive(terminal, "terminal?cwd=foo"));
    }

    [Fact]
    public void IsActive_exercise_page_activates_no_primary_tab()
    {
        Assert.All(NavDestinations.Primary, d =>
            Assert.False(NavDestinations.IsActive(d, "module/05-git/ex00-branche-merge")));
    }
}
