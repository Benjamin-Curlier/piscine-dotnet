using System.Collections.Generic;
using Bunit;
using Piscine.App.Board;
using Piscine.Components.Components.Board;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class BoardOverviewTests : BunitContext
{
    [Fact]
    public void Renders_counts_percent_and_module_bars()
    {
        var counts = new BoardCounts(Fait: 2, EnCours: 1, ARevoir: 1, Restant: 2, Total: 6);
        var modules = new List<ModuleProgress> { new("01", "Bases", 2, 4), new("02", "Boucles", 0, 3) };

        var cut = Render<BoardOverview>(p => p
            .Add(c => c.Counts, counts)
            .Add(c => c.Modules, modules));

        Assert.Contains("2", cut.Find("[data-testid='board-count-fait']").TextContent);
        Assert.Contains("33", cut.Find("[data-testid='board-percent']").TextContent); // 2/6
        Assert.Equal(2, cut.FindAll("[data-testid='board-module']").Count);
    }
}
