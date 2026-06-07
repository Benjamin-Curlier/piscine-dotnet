using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Progress;
using Piscine.App.Push;
using Piscine.Components.Components.Push;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="PushResultPanel"/> :
/// état vide, entrée « À revoir » avec lien /check, entrée « Réussi » sans lien,
/// et auto-rafraîchissement sur <c>ResultReceived</c>.
/// </summary>
public sealed class PushResultPanelTests : BunitContext
{
    // ── Faux IPushResultWatcher ───────────────────────────────────────────────

    private sealed class FakeWatcher : IPushResultWatcher
    {
        private PushResult? _latest;
        private readonly PushResultDocument? _rich;

        public FakeWatcher(PushResult? latest = null, PushResultDocument? rich = null)
        {
            _latest = latest;
            _rich = rich;
        }

        public event Action<PushResult>? ResultReceived;

        public PushResult? LatestResult() => _latest;

        public PushResultDocument? LatestRichResult() => _rich;

        public void Start() { /* no-op */ }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>Déclenche <see cref="ResultReceived"/> depuis le test pour simuler un rendu entrant.</summary>
        public void RaiseResult(PushResult r)
        {
            _latest = r;
            ResultReceived?.Invoke(r);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PushResult MakeResult(params (string Id, PushVerdict Verdict, int Attempts)[] entries)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = entries
            .Select(e => new PushResultEntry(e.Id, e.Verdict, e.Attempts, now))
            .ToList();
        return new PushResult(changed, now);
    }

    // ── Test 1 : état vide ────────────────────────────────────────────────────

    [Fact]
    public void Render_WhenLatestResultIsNull_ShowsPushEmptyAndNoEntry()
    {
        // Arrange
        var fake = new FakeWatcher(latest: null);
        Services.AddSingleton<IPushResultWatcher>(fake);

        // Act
        var cut = Render<PushResultPanel>();

        // Assert
        cut.Find("[data-testid='push-empty']");
        Assert.Empty(cut.FindAll("[data-testid='push-entry']"));
    }

    // ── Test 2 : entrée « À revoir » ──────────────────────────────────────────

    [Fact]
    public void Render_WithARevoir_ShowsEntryWithBadgeAndCheckLink()
    {
        // Arrange
        var result = MakeResult(("ex00-hello", PushVerdict.ARevoir, 1));
        var fake = new FakeWatcher(latest: result);
        Services.AddSingleton<IPushResultWatcher>(fake);

        // Act
        var cut = Render<PushResultPanel>();

        // Assert — au moins une entrée
        var entry = cut.Find("[data-testid='push-entry']");
        Assert.NotNull(entry);

        // Assert — badge ARevoir
        var badge = cut.Find("[data-testid='status-badge']");
        Assert.Equal("ARevoir", badge.GetAttribute("data-status"));

        // Assert — lien /check présent
        var link = cut.Find("[data-testid='push-check-link']");
        Assert.Equal("/check", link.GetAttribute("href"));

        // Assert — pas de placeholder vide
        Assert.Empty(cut.FindAll("[data-testid='push-empty']"));
    }

    // ── Test 3 : entrée « Réussi » ────────────────────────────────────────────

    [Fact]
    public void Render_WithReussi_ShowsBadgePousseNoteAndNoCheckLink()
    {
        // Arrange
        var result = MakeResult(("ex00-hello", PushVerdict.Reussi, 2));
        var fake = new FakeWatcher(latest: result);
        Services.AddSingleton<IPushResultWatcher>(fake);

        // Act
        var cut = Render<PushResultPanel>();

        // Assert — badge PousseNote (mapping Reussi → PousseNote)
        var badge = cut.Find("[data-testid='status-badge']");
        Assert.Equal("PousseNote", badge.GetAttribute("data-status"));

        // Assert — pas de lien /check pour Réussi
        Assert.Empty(cut.FindAll("[data-testid='push-check-link']"));
    }

    // ── Test 4 : auto-rafraîchissement ───────────────────────────────────────

    [Fact]
    public void Render_WhenResultReceivedRaised_UpdatesDisplayWithoutInteraction()
    {
        // Arrange — démarrer vide
        var fake = new FakeWatcher(latest: null);
        Services.AddSingleton<IPushResultWatcher>(fake);
        var cut = Render<PushResultPanel>();

        // Vérifier état initial
        cut.Find("[data-testid='push-empty']");
        Assert.Empty(cut.FindAll("[data-testid='push-entry']"));

        // Act — déclencher ResultReceived sans interaction utilisateur
        var result = MakeResult(("ex01-foo", PushVerdict.ARevoir, 1));
        fake.RaiseResult(result);

        // Assert — attendre que le composant se re-rende avec la nouvelle entrée
        cut.WaitForState(() => cut.FindAll("[data-testid='push-entry']").Count > 0,
            timeout: TimeSpan.FromSeconds(3));

        var entry = cut.Find("[data-testid='push-entry']");
        Assert.NotNull(entry);
        Assert.Empty(cut.FindAll("[data-testid='push-empty']"));
    }

    // ── Test 5 : résultat riche → diff inline (CheckFeedback), pas de lien /check ──

    [Fact]
    public void Render_WithRichResult_ShowsDiffInline_NoCheckLink()
    {
        // Arrange — statut + artefact riche (#40) pour le même exercice.
        var result = MakeResult(("ex00-hello", PushVerdict.ARevoir, 1));
        var rich = new PushResultDocument(
            new[]
            {
                new PushExerciseResult(
                    "ex00-hello", "00-setup", "ARevoir",
                    new[] { new PushCaseResult("io", false, new[] { "Attendu : ok", "Obtenu  : non" }) },
                    Hint: "Relis l'énoncé.",
                    CourseRef: "cours.md#hello"),
            },
            DateTimeOffset.UtcNow);
        Services.AddSingleton<IPushResultWatcher>(new FakeWatcher(latest: result, rich: rich));

        // Act
        var cut = Render<PushResultPanel>();

        // Assert — le diff riche est rendu inline (CheckFeedback) au lieu du lien /check.
        cut.Find("[data-testid='diff-expected']");
        cut.Find("[data-testid='diff-actual']");
        cut.Find("[data-testid='check-verdict']");
        cut.Find("[data-testid='check-hint']");
        cut.Find("[data-testid='check-course-ref']");
        Assert.Empty(cut.FindAll("[data-testid='push-check-link']"));
    }
}
