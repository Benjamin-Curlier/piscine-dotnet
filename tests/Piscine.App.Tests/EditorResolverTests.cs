using Piscine.App.Launch;
using Xunit;

namespace Piscine.App.Tests;

public sealed class EditorResolverTests
{
    [Fact]
    public void Override_wins_over_detection() =>
        Assert.Equal("micro", EditorResolver.Resolve("micro", _ => true)!.FileName);

    [Fact]
    public void Picks_first_candidate_on_path() =>
        Assert.Equal("code", EditorResolver.Resolve(null, c => c == "code")!.FileName);

    [Fact]
    public void Picks_rider_when_only_rider_present() =>
        Assert.Equal("Rider", EditorResolver.Resolve(null, c => c == "rider")!.Label);

    [Fact]
    public void Null_when_nothing_found() =>
        Assert.Null(EditorResolver.Resolve(null, _ => false));

    [Fact]
    public void Blank_override_is_ignored() =>
        Assert.Equal("code", EditorResolver.Resolve("   ", c => c == "code")!.FileName);
}
