using System.Linq;
using Piscine.App.Checking;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>
/// Tests purs de <see cref="StructuredDiffBuilder"/> : dérivation d'un diff structuré ligne à ligne
/// depuis les messages verbatim du grader io (« Attendu : "…" » / « Obtenu  : "…" »), sans toucher
/// au moteur. Format des messages = <c>IoGrader.Quote</c> (contenu entre guillemets, <c>\n</c> échappés).
/// </summary>
public sealed class StructuredDiffBuilderTests
{
    [Fact]
    public void TryBuild_WithoutExpectedOrActual_ReturnsNull()
    {
        // Cas non-io : compilation, exception, code de sortie… pas de ligne Attendu/Obtenu.
        var diff = StructuredDiffBuilder.TryBuild(["Le programme ne compile pas :", "erreur CS1002"]);
        Assert.Null(diff);
    }

    [Fact]
    public void TryBuild_WithOnlyExpected_ReturnsNull()
    {
        var diff = StructuredDiffBuilder.TryBuild(["Attendu : \"Hello\\n\""]);
        Assert.Null(diff);
    }

    [Fact]
    public void TryBuild_SingleLineMismatch_MarksExpectedThenActual()
    {
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "La sortie ne correspond pas.",
            "Attendu : \"Hello, Piscine!\\n\"",
            "Obtenu  : \"Bonjour\\n\"",
        ]);

        Assert.NotNull(diff);

        // "Hello, Piscine!" ≠ "Bonjour" → une ligne expected, une ligne actual ; la dernière ligne
        // vide (issue du \n final) est commune aux deux → contexte unchanged.
        var expected = diff.Lines.Where(l => l.Kind == DiffLineKind.Expected).ToList();
        var actual = diff.Lines.Where(l => l.Kind == DiffLineKind.Actual).ToList();

        Assert.Contains(expected, l => l.Text == "Hello, Piscine!");
        Assert.Contains(actual, l => l.Text == "Bonjour");
    }

    [Fact]
    public void TryBuild_UnescapesNewlinesIntoSeparateLines()
    {
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "Attendu : \"ligne1\\nligne2\\n\"",
            "Obtenu  : \"ligne1\\nDIFFERENT\\n\"",
        ]);

        Assert.NotNull(diff);

        // "ligne1" est commune (contexte), "ligne2"/"DIFFERENT" diffèrent, "" final commun.
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Unchanged && l.Text == "ligne1");
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Expected && l.Text == "ligne2");
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Actual && l.Text == "DIFFERENT");
    }

    [Fact]
    public void TryBuild_IdenticalContent_AllUnchanged()
    {
        // Défensif : même si le grader n'émet un diff que sur mismatch, la dérivation reste cohérente.
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "Attendu : \"a\\nb\\n\"",
            "Obtenu  : \"a\\nb\\n\"",
        ]);

        Assert.NotNull(diff);
        Assert.All(diff.Lines, l => Assert.Equal(DiffLineKind.Unchanged, l.Kind));
    }

    [Fact]
    public void TryBuild_ExtraActualLine_MarkedActual()
    {
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "Attendu : \"a\\n\"",
            "Obtenu  : \"a\\nbonus\\n\"",
        ]);

        Assert.NotNull(diff);
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Actual && l.Text == "bonus");
        // "a" reste du contexte commun.
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Unchanged && l.Text == "a");
    }

    [Fact]
    public void TryBuild_MissingExpectedLine_MarkedExpected()
    {
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "Attendu : \"a\\nb\\nc\\n\"",
            "Obtenu  : \"a\\nc\\n\"",
        ]);

        Assert.NotNull(diff);
        // "b" manque dans l'obtenu → marqué expected.
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Expected && l.Text == "b");
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Unchanged && l.Text == "a");
        Assert.Contains(diff.Lines, l => l.Kind == DiffLineKind.Unchanged && l.Text == "c");
    }

    [Fact]
    public void TryBuild_PreservesAllExpectedAndActualContent()
    {
        // Invariant : les lignes Expected+Unchanged reconstituent l'attendu ; Actual+Unchanged l'obtenu.
        var diff = StructuredDiffBuilder.TryBuild(
        [
            "Attendu : \"x\\ny\\nz\\n\"",
            "Obtenu  : \"x\\nW\\nz\\n\"",
        ]);

        Assert.NotNull(diff);

        var rebuiltExpected = diff.Lines
            .Where(l => l.Kind is DiffLineKind.Unchanged or DiffLineKind.Expected)
            .Select(l => l.Text)
            .ToList();
        var rebuiltActual = diff.Lines
            .Where(l => l.Kind is DiffLineKind.Unchanged or DiffLineKind.Actual)
            .Select(l => l.Text)
            .ToList();

        Assert.Equal(new[] { "x", "y", "z", "" }, rebuiltExpected);
        Assert.Equal(new[] { "x", "W", "z", "" }, rebuiltActual);
    }
}
