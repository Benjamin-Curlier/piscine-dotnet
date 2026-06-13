using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Piscine.Components.Components.Layout;
using Piscine.Components.Navigation;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class NavTabsTests : BunitContext
{
    [Fact]
    public void Renders_one_link_per_primary_destination()
    {
        var cut = Render<NavTabs>();

        foreach (var d in NavDestinations.Primary)
        {
            var link = cut.Find($"[data-testid='{d.TestId}']");
            Assert.Equal(d.Route, link.GetAttribute("href"));
            Assert.Contains(d.Label, link.TextContent, System.StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Marks_active_destination_from_current_uri()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/cours");

        var cut = Render<NavTabs>();

        Assert.Contains("active", cut.Find("[data-testid='nav-cours']").GetAttribute("class"));
        Assert.DoesNotContain("active", cut.Find("[data-testid='nav-dashboard']").GetAttribute("class") ?? "");
    }
}
