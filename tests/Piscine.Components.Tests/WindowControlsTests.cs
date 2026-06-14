using Bunit;
using Piscine.Components.Components.Layout;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class WindowControlsTests : BunitContext
{
    [Fact]
    public void Renders_three_window_buttons_with_french_aria_labels()
    {
        var cut = Render<WindowControls>();
        var buttons = cut.FindAll("button.win-btn");
        Assert.Equal(3, buttons.Count);
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Réduire la fenêtre");
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Agrandir ou restaurer la fenêtre");
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Fermer la fenêtre");
    }

    [Fact]
    public void Buttons_invoke_winControl_via_onclick_attribute()
    {
        var cut = Render<WindowControls>();
        var markup = cut.Markup;
        Assert.Contains("winControl('minimize')", markup);
        Assert.Contains("winControl('togglemax')", markup);
        Assert.Contains("winControl('close')", markup);
    }
}
