namespace Piscine.App.Search;

/// <summary>
/// Nature d'une entrée de la palette de commande. Sert au tri (les destinations/actions priment sur le
/// plein-texte à score égal) et au pictogramme affiché par l'UI.
/// </summary>
public enum SearchKind
{
    /// <summary>Destination de navigation primaire (Tableau de bord, Cours, Progression…).</summary>
    Destination,

    /// <summary>Action exécutable (Vérifier, Ouvrir, Initialiser…).</summary>
    Action,

    /// <summary>Un module pédagogique.</summary>
    Module,

    /// <summary>Un exercice.</summary>
    Exercise,

    /// <summary>Un résultat de recherche plein-texte (cours ou sujet).</summary>
    Content,
}

/// <summary>
/// Une entrée indexable de la palette, en données (sans UI). <paramref name="Title"/> est la cible
/// principale du filtrage flou ; <paramref name="Subtitle"/> est un complément affiché et faiblement
/// pondéré ; <paramref name="Body"/> alimente la recherche plein-texte (markdown cours/sujet) et n'est
/// pas affiché tel quel. <paramref name="Route"/> est la destination de navigation ; <paramref name="Keywords"/>
/// regroupe des synonymes/identifiants supplémentaires pris en compte au filtrage.
/// </summary>
public sealed record SearchCommand(
    SearchKind Kind,
    string Title,
    string? Subtitle,
    string Route,
    string TestId,
    string? Body = null,
    IReadOnlyList<string>? Keywords = null);

/// <summary>
/// Une entrée classée par <see cref="SearchService"/> : la commande, son score (plus haut = plus
/// pertinent) et, le cas échéant, un extrait de contexte plein-texte autour de la 1ʳᵉ occurrence.
/// </summary>
public sealed record SearchResult(SearchCommand Command, int Score, string? Snippet);
