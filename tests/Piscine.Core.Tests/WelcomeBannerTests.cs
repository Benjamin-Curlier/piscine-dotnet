using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class WelcomeBannerTests
{
    [Fact]
    public void Render_ContainsTitle()
    {
        var banner = WelcomeBanner.Render("1.2.3");

        Assert.Contains("Piscine .NET", banner);
    }

    [Fact]
    public void Render_ContainsVersion()
    {
        var banner = WelcomeBanner.Render("1.2.3");

        Assert.Contains("1.2.3", banner);
    }
}
