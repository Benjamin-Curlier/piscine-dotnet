using Bunit;
using Piscine.App.Checking;
using Piscine.Components.Components.Check;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="CheckFeedback"/> : rendu du verdict, du diff
/// attendu/obtenu, de l'indice et du lien course_ref. Les <see cref="CheckOutcome"/> sont
/// construits à la main (le composant est pur, sans appel moteur).
/// </summary>
public sealed class CheckFeedbackTests : BunitContext
{
    // --- Cas PASS : verdict Réussi, pas d'indice, pas de diff ---------------------------------

    [Fact]
    public void Render_PassOutcome_ShowsVerdictOnly()
    {
        // Arrange
        var outcome = new CheckOutcome(
            ExerciseId: "ex00-hello",
            ModuleId: "00-setup-git",
            Verdict: CheckVerdict.Reussi,
            Cases: [new CheckCaseResult("io", true, [])],
            Hint: null,
            CourseRef: null);

        // Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, outcome));

        // Assert — verdict présent
        var verdict = cut.Find("[data-testid='check-verdict']");
        Assert.Contains("Réussi", verdict.TextContent, System.StringComparison.Ordinal);

        // Assert — aucun indice
        Assert.Empty(cut.FindAll("[data-testid='check-hint']"));

        // Assert — aucun diff
        Assert.Empty(cut.FindAll("[data-testid='diff-expected']"));
        Assert.Empty(cut.FindAll("[data-testid='diff-actual']"));

        // Assert — pas d'encart « pas suivant » sur un verdict Réussi
        Assert.Empty(cut.FindAll("[data-testid='check-next-step']"));
    }

    // --- Cas AucunFichier : encart actionnable « pas suivant » --------------------------------

    [Fact]
    public void Render_AucunFichier_ShowsActionableNextStep()
    {
        // Arrange — verdict sans cas (workspace vide), tel que produit par CheckService.
        var outcome = new CheckOutcome(
            ExerciseId: "ex00-hello",
            ModuleId: "00-setup-git",
            Verdict: CheckVerdict.AucunFichier,
            Cases: [],
            Hint: null,
            CourseRef: null);

        // Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, outcome));

        // Assert — encart actionnable présent, orientant vers « Ouvrir le dossier »
        var nextStep = cut.Find("[data-testid='check-next-step']");
        Assert.Contains("Ouvrir le dossier", nextStep.TextContent, System.StringComparison.Ordinal);
    }

    // --- Cas Introuvable : encart actionnable « pas suivant » ---------------------------------

    [Fact]
    public void Render_Introuvable_ShowsActionableNextStep()
    {
        // Arrange
        var outcome = new CheckOutcome(
            ExerciseId: "ex-inconnu",
            ModuleId: string.Empty,
            Verdict: CheckVerdict.Introuvable,
            Cases: [],
            Hint: null,
            CourseRef: null);

        // Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, outcome));

        // Assert — encart actionnable présent, mentionnant l'exercice introuvable
        var nextStep = cut.Find("[data-testid='check-next-step']");
        Assert.Contains("introuvable", nextStep.TextContent, System.StringComparison.OrdinalIgnoreCase);
    }

    // --- Cas FAIL : diff + indice + lien course_ref -------------------------------------------

    [Fact]
    public void Render_FailOutcome_ShowsDiffHintAndCourseRef()
    {
        // Arrange — messages tels que produits par IoGrader
        var messages = new[]
        {
            "La sortie ne correspond pas.",
            "Attendu : \"Hello, Piscine!\\n\"",
            "Obtenu  : \"Bonjour\\n\"",
        };

        var outcome = new CheckOutcome(
            ExerciseId: "ex00-hello",
            ModuleId: "00-setup-git",
            Verdict: CheckVerdict.ARevoir,
            Cases: [new CheckCaseResult("io", false, messages)],
            Hint: "Vérifie la casse, la virgule, le '!' et le retour à la ligne final.",
            CourseRef: "cours.md#hello-world");

        // Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, outcome));

        // Assert — verdict présent
        var verdict = cut.Find("[data-testid='check-verdict']");
        Assert.Contains("revoir", verdict.TextContent, System.StringComparison.OrdinalIgnoreCase);

        // Assert — diff-expected et diff-actual présents
        var expected = cut.Find("[data-testid='diff-expected']");
        Assert.Contains("Attendu", expected.TextContent, System.StringComparison.Ordinal);

        var actual = cut.Find("[data-testid='diff-actual']");
        Assert.Contains("Obtenu", actual.TextContent, System.StringComparison.Ordinal);

        // Assert — indice présent
        cut.Find("[data-testid='check-hint']");

        // Assert — lien course_ref : href contient /module/00-setup-git et l'ancre hello-world
        var link = cut.Find("[data-testid='check-course-ref']");
        var href = link.GetAttribute("href") ?? string.Empty;
        Assert.Contains("/module/00-setup-git", href, System.StringComparison.Ordinal);
        Assert.Contains("hello-world", href, System.StringComparison.Ordinal);
    }

    // --- Cas FAIL avec diff STRUCTURÉ (S4) : rendu coloré ligne à ligne -----------------------

    [Fact]
    public void Render_FailOutcome_WithStructuredDiff_RendersColoredRows()
    {
        // Arrange — diff structuré dérivé côté App (ligne contexte + ligne attendue + ligne obtenue).
        var diff = new StructuredDiff(
        [
            new DiffLine(DiffLineKind.Unchanged, "ligne commune"),
            new DiffLine(DiffLineKind.Expected, "Hello, Piscine!"),
            new DiffLine(DiffLineKind.Actual, "Bonjour"),
        ]);

        var outcome = new CheckOutcome(
            ExerciseId: "ex00-hello",
            ModuleId: "00-setup-git",
            Verdict: CheckVerdict.ARevoir,
            Cases: [new CheckCaseResult("io", false, ["La sortie ne correspond pas."], diff)],
            Hint: null,
            CourseRef: null);

        // Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, outcome));

        // Assert — bloc diff structuré présent
        cut.Find("[data-testid='diff-block']");

        // Assert — lignes attendue (rouge/vert) et obtenue rendues avec leurs testid stables
        var expected = cut.Find("[data-testid='diff-expected']");
        Assert.Contains("Hello, Piscine!", expected.TextContent, System.StringComparison.Ordinal);

        var actual = cut.Find("[data-testid='diff-actual']");
        Assert.Contains("Bonjour", actual.TextContent, System.StringComparison.Ordinal);

        // Assert — ligne de contexte présente
        var context = cut.Find("[data-testid='diff-context']");
        Assert.Contains("ligne commune", context.TextContent, System.StringComparison.Ordinal);

        // Assert — le message non-diff reste affiché
        Assert.Contains("La sortie ne correspond pas.", cut.Markup, System.StringComparison.Ordinal);
    }

    // --- Outcome null : placeholder affiché ---------------------------------------------------

    [Fact]
    public void Render_NullOutcome_ShowsPlaceholder()
    {
        // Arrange + Act
        var cut = Render<CheckFeedback>(p => p.Add(c => c.Outcome, (CheckOutcome?)null));

        // Assert — aucun verdict, juste un placeholder
        Assert.Empty(cut.FindAll("[data-testid='check-verdict']"));
        Assert.NotEmpty(cut.Markup); // quelque chose est rendu (le placeholder)
    }
}
