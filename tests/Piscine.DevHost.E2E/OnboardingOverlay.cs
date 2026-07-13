using Microsoft.Playwright;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Aide E2E partagée : ferme l'overlay d'onboarding du 1ᵉʳ lancement (S7) s'il est présent, pour que
/// les tests qui pilotent une page derrière lui (boutons de <c>/init</c>, <c>/check</c>, <c>/reglages</c>…)
/// ne voient pas leurs clics interceptés par le modal (<c>data-testid="onboarding"</c>,
/// <c>position:fixed; inset:0; z-index:500</c> — il recouvre tout l'écran).
/// </summary>
/// <remarks>
/// L'overlay ne s'affiche que sur un workspace NON initialisé (<c>OnboardingState.ShouldShow</c>). Comme
/// il est prérendu côté serveur, il est déjà dans le DOM au retour de la navigation ; on ne fait donc
/// rien s'il est absent (workspace déjà initialisé) → l'aide est sûre à appeler inconditionnellement.
/// Sinon on attend le marqueur d'interactivité (<c>window.__onboardingReady</c>, posé par
/// <c>OnboardingFlow.razor.js</c> une fois le circuit Blazor Server monté et les <c>@onclick</c> câblés),
/// on clique « Plus tard » (<c>data-testid="onboarding-skip"</c> → <c>Dismiss()</c> met
/// <c>_visible=false</c>, sans initialiser quoi que ce soit ni naviguer), puis on attend le retrait
/// effectif du DOM avant de rendre la main à l'appelant.
/// </remarks>
internal static class OnboardingOverlay
{
    /// <summary>Ferme l'overlay d'onboarding s'il recouvre la page ; no-op s'il est absent.</summary>
    public static async Task DismissIfPresentAsync(IPage page)
    {
        // Prérendu : sur un workspace initialisé l'overlay n'est jamais rendu → rien à fermer.
        if (await page.QuerySelectorAsync("[data-testid='onboarding']") is null)
        {
            return;
        }

        // Attendre que les gestionnaires @onclick soient câblés (circuit Blazor Server monté).
        await page.WaitForFunctionAsync(
            "() => window.__onboardingReady === true",
            new PageWaitForFunctionOptions { Timeout = 30_000 });

        // « Plus tard » ferme l'overlay pour la session (Dismiss → _visible=false) sans effet de bord.
        await page.ClickAsync(
            "[data-testid='onboarding-skip']",
            new PageClickOptions { Timeout = 15_000 });

        // Attendre le retrait effectif du DOM avant que l'appelant n'interagisse derrière.
        await page.WaitForSelectorAsync(
            "[data-testid='onboarding']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 15_000 });
    }
}
