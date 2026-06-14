using System;
using System.Collections.Generic;

namespace Piscine.Components.Navigation;

/// <summary>
/// Catalogue des destinations primaires (en données) + règle d'activation par premier segment d'URL.
/// Pur et sans état → testable sans rendu. Rapport (/rapport) et Réglages (/reglages) s'ajouteront
/// à leurs sprints respectifs (S5/S6) ; Vérifier/Initialiser/Résultat seront absorbés par S2/S4/S7.
/// </summary>
public static class NavDestinations
{
    public static IReadOnlyList<NavDestination> Primary { get; } =
    [
        new NavDestination("Tableau de bord", "/", "nav-dashboard"),
        new NavDestination("Cours", "/cours", "nav-cours"),
        new NavDestination("Progression", "/progress", "nav-progress"),
        new NavDestination("Rapport", "/rapport", "nav-rapport"),
        new NavDestination("Vérifier", "/check", "nav-check"),
        new NavDestination("Initialiser", "/init", "nav-init"),
        new NavDestination("Résultat", "/resultat", "nav-resultat"),
        new NavDestination("Terminal", "/terminal", "nav-terminal"),
    ];

    /// <summary>
    /// Vrai si <paramref name="currentRelativePath"/> (chemin relatif à la base, sans slash de tête —
    /// tel que renvoyé par <c>NavigationManager.ToBaseRelativePath</c>) appartient à la destination.
    /// La racine "/" n'est active que pour le chemin vide ; sinon comparaison du premier segment.
    /// </summary>
    public static bool IsActive(NavDestination destination, string currentRelativePath)
    {
        var path = currentRelativePath.Split('?', '#')[0].Trim('/');
        var route = destination.Route.Trim('/');

        if (route.Length == 0)
        {
            return path.Length == 0;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var first = segments.Length > 0 ? segments[0] : string.Empty;
        return string.Equals(first, route, StringComparison.OrdinalIgnoreCase);
    }
}
