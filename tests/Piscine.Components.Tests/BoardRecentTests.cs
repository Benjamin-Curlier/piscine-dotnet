using System;
using Bunit;
using Piscine.App.Push;
using Piscine.Components.Components.Board;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class BoardRecentTests : BunitContext
{
    [Fact]
    public void Null_shows_empty_state()
    {
        var cut = Render<BoardRecent>(p => p.Add(c => c.Recent, (PushResult?)null));
        Assert.NotNull(cut.Find("[data-testid='board-recent-empty']"));
    }

    [Fact]
    public void With_entries_lists_them()
    {
        var recent = new PushResult(
            new[] { new PushResultEntry("ex00-hello", PushVerdict.Reussi, 1, DateTimeOffset.UtcNow) },
            DateTimeOffset.UtcNow);
        var cut = Render<BoardRecent>(p => p.Add(c => c.Recent, recent));
        Assert.Single(cut.FindAll("[data-testid='board-recent-item']"));
    }
}
