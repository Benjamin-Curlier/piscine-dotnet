namespace Piscine.Components.Navigation;

/// <summary>
/// Une destination primaire de navigation, en données : la coquille la rend en onglet (Approche A)
/// et, plus tard, en rail d'icônes (B) sans changer les pages.
/// </summary>
public sealed record NavDestination(string Label, string Route, string TestId);
