// Interop de l'onboarding 1ᵉʳ lancement (S7) : un simple marqueur de disponibilité interactive.
// L'E2E attend window.__onboardingReady avant d'interagir (le circuit Blazor est alors monté →
// les @onclick sont câblés). Aucun état durable ici — la logique d'affichage vit côté C#.

export function markReady() {
    window.__onboardingReady = true;
}
