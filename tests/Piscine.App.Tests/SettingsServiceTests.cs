using System.IO;
using Piscine.App.Settings;
using Piscine.Core;
using Xunit;

namespace Piscine.App.Tests;

public sealed class SettingsServiceTests
{
    private static SettingsService Create(TempDir tmp) =>
        new(new PiscineLayout(tmp.Path, Path.Combine(tmp.Path, "ws"), Path.Combine(tmp.Path, "state")));

    [Fact]
    public void Load_returns_default_when_file_absent()
    {
        using var tmp = new TempDir();
        Assert.Null(Create(tmp).Load().EditorCommand);
    }

    [Fact]
    public void Save_then_Load_roundtrips_editor_command()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);
        svc.Save(new AppSettings { EditorCommand = "code" });
        Assert.Equal("code", svc.Load().EditorCommand);
    }

    [Fact]
    public void Load_returns_default_when_file_corrupt()
    {
        using var tmp = new TempDir();
        var state = Path.Combine(tmp.Path, "state");
        Directory.CreateDirectory(state);
        File.WriteAllText(Path.Combine(state, "settings.json"), "{ pas du json");

        Assert.Null(Create(tmp).Load().EditorCommand); // pas d'exception
    }

    // ---------- S6 : thème, échelle de police, cible terminal ----------

    [Fact]
    public void Defaults_are_system_theme_unit_scale_embedded_terminal()
    {
        using var tmp = new TempDir();
        var settings = Create(tmp).Load();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(AppSettings.DefaultFontScale, settings.FontScale);
        Assert.Equal(TerminalTarget.Embedded, settings.DefaultTerminal);
    }

    [Fact]
    public void Save_then_Load_roundtrips_theme_scale_and_terminal()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);

        svc.Save(new AppSettings
        {
            Theme = AppTheme.Dark,
            FontScale = 1.25,
            DefaultTerminal = TerminalTarget.System,
            EditorCommand = "rider",
        });

        var loaded = svc.Load();
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(1.25, loaded.FontScale);
        Assert.Equal(TerminalTarget.System, loaded.DefaultTerminal);
        Assert.Equal("rider", loaded.EditorCommand);
    }

    [Fact]
    public void Save_clamps_font_scale_above_max()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);

        svc.Save(new AppSettings { FontScale = 99.0 });

        Assert.Equal(AppSettings.MaxFontScale, svc.Load().FontScale);
    }

    [Fact]
    public void Save_clamps_font_scale_below_min()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);

        svc.Save(new AppSettings { FontScale = 0.1 });

        Assert.Equal(AppSettings.MinFontScale, svc.Load().FontScale);
    }

    [Fact]
    public void Load_clamps_out_of_range_font_scale_edited_by_hand()
    {
        using var tmp = new TempDir();
        var state = Path.Combine(tmp.Path, "state");
        Directory.CreateDirectory(state);
        File.WriteAllText(Path.Combine(state, "settings.json"), """{ "fontScale": 5.0, "theme": "dark" }""");

        var loaded = Create(tmp).Load();
        Assert.Equal(AppSettings.MaxFontScale, loaded.FontScale);
        Assert.Equal(AppTheme.Dark, loaded.Theme); // le reste est respecté
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ClampFontScale_falls_back_to_default_for_non_finite(double value)
    {
        Assert.Equal(AppSettings.DefaultFontScale, AppSettings.ClampFontScale(value));
    }

    [Fact]
    public void Theme_persists_as_camelcase_string_in_json()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);
        svc.Save(new AppSettings { Theme = AppTheme.Dark, DefaultTerminal = TerminalTarget.System });

        var json = File.ReadAllText(Path.Combine(tmp.Path, "state", "settings.json"));
        Assert.Contains("\"dark\"", json, System.StringComparison.Ordinal);
        Assert.Contains("\"system\"", json, System.StringComparison.Ordinal);
    }
}
