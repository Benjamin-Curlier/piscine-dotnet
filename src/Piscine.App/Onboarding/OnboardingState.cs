using Piscine.App.Init;

namespace Piscine.App.Onboarding;

/// <summary>
/// Décide si l'onboarding du 1ᵉʳ lancement doit s'afficher (S7). Règle pure et sans état : on s'appuie
/// sur <see cref="InitService.Status"/> (déjà testé, lecture seule) — l'onboarding est proposé tant que
/// le workspace n'est PAS initialisé, et disparaît dès qu'il l'est (pas de harcèlement une fois prêt).
/// On NE persiste PAS de drapeau « déjà vu » : l'état d'initialisation est la seule source de vérité,
/// ce qui rend la décision idempotente et sans nouvelle persistance dérivée (cf. spec §4).
/// </summary>
public sealed class OnboardingState(InitService init)
{
    private readonly InitService _init = init;

    /// <summary>
    /// Vrai si l'environnement n'est pas encore initialisé → on guide la recrue (accueil → init → 1ᵉʳ exo).
    /// Faux dès que le workspace, le dépôt bare et le hook sont en place.
    /// </summary>
    public bool ShouldShow() => !_init.Status().IsInitialized;
}
