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

    [Fact]
    public void Groups_modules_by_act_and_opens_first_incomplete_act_only()
    {
        var counts = new BoardCounts(1, 0, 0, 2, 3);
        var modules = new List<ModuleProgress>
        {
            new("00", "Setup", 1, 1, "Acte I — Départ", "00-setup"),
            new("01", "Bases", 0, 1, "Acte I — Départ", "01-bases"),
            new("06", "Collections", 0, 1, "Acte II — Métier", "06-collections"),
        };

        var cut = Render<BoardOverview>(parameters => parameters
            .Add(component => component.Counts, counts)
            .Add(component => component.Modules, modules));

        var acts = cut.FindAll(".board-act");
        Assert.Equal(2, acts.Count);
        Assert.NotNull(acts[0].GetAttribute("open"));
        Assert.Null(acts[1].GetAttribute("open"));
    }
}
