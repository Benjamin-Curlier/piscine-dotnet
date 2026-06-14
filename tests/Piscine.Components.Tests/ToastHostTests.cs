using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Push;
using Piscine.Components.Components.Layout;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit de <see cref="ToastHost"/> : aucun toast au repos, apparition à l'arrivée d'un
/// <see cref="IPushResultWatcher.ResultReceived"/> (sur n'importe quelle page, sans interaction),
/// et fermeture au clic.
/// </summary>
public sealed class ToastHostTests : BunitContext
{
    // ── Faux IPushResultWatcher (déclenchable depuis le test) ──────────────────

    private sealed class FakeWatcher : IPushResultWatcher
    {
        private PushResult? _latest;

        public event Action<PushResult>? ResultReceived;

        public PushResult? LatestResult() => _latest;

        public PushResultDocument? LatestRichResult() => null;

        public void Start() { /* no-op */ }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void RaiseResult(PushResult r)
        {
            _latest = r;
            ResultReceived?.Invoke(r);
        }
    }

    private static PushResult MakeResult(string id, PushVerdict verdict) =>
        new([new PushResultEntry(id, verdict, 1, DateTimeOffset.UtcNow)], DateTimeOffset.UtcNow);

    // ── Test 1 : pas de toast au repos ─────────────────────────────────────────

    [Fact]
    public void Render_NoResult_ShowsNoToast()
    {
        Services.AddSingleton<IPushResultWatcher>(new FakeWatcher());

        var cut = Render<ToastHost>();

        Assert.Empty(cut.FindAll("[data-testid='push-toast']"));
    }

    // ── Test 2 : toast apparaît sur ResultReceived (sans interaction) ──────────

    [Fact]
    public void Render_OnResultReceived_ShowsToastWithEntry()
    {
        var fake = new FakeWatcher();
        Services.AddSingleton<IPushResultWatcher>(fake);
        var cut = Render<ToastHost>();

        // État initial : pas de toast.
        Assert.Empty(cut.FindAll("[data-testid='push-toast']"));

        // Act — un verdict de push arrive (thread du watcher simulé).
        fake.RaiseResult(MakeResult("ex00-hello", PushVerdict.ARevoir));

        // Assert — le toast apparaît seul.
        cut.WaitForState(() => cut.FindAll("[data-testid='push-toast']").Count > 0,
            timeout: TimeSpan.FromSeconds(3));

        cut.Find("[data-testid='push-toast']");
        var entry = cut.Find("[data-testid='toast-entry']");
        Assert.Contains("ex00-hello", entry.TextContent, StringComparison.Ordinal);

        // Badge de statut ARevoir présent dans le toast.
        var badge = cut.Find("[data-testid='status-badge']");
        Assert.Equal("ARevoir", badge.GetAttribute("data-status"));

        // Lien vers /resultat.
        var link = cut.Find("[data-testid='toast-link']");
        Assert.Equal("/resultat", link.GetAttribute("href"));
    }

    // ── Test 3 : fermeture au clic ─────────────────────────────────────────────

    [Fact]
    public void Click_Close_DismissesToast()
    {
        var fake = new FakeWatcher();
        Services.AddSingleton<IPushResultWatcher>(fake);
        var cut = Render<ToastHost>();

        fake.RaiseResult(MakeResult("ex00-hello", PushVerdict.Reussi));
        cut.WaitForState(() => cut.FindAll("[data-testid='push-toast']").Count > 0,
            timeout: TimeSpan.FromSeconds(3));

        // Act — fermer le toast.
        cut.Find("[data-testid='toast-close']").Click();

        // Assert — le toast disparaît.
        Assert.Empty(cut.FindAll("[data-testid='push-toast']"));
    }
}
