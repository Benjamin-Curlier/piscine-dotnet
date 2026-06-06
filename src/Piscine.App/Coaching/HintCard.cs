namespace Piscine.App.Coaching;

/// <summary>Severite d'une carte d'indice (jamais une note : registre purement educatif).</summary>
public enum HintSeverity
{
    /// <summary>Information / suggestion (ex. rappel de pousser, typo probable).</summary>
    Info,

    /// <summary>Avertissement : une action est probablement requise (ex. mauvaise branche).</summary>
    Warn,

    /// <summary>Bloquant : la suite ne peut pas aboutir sans corriger (ex. conflit, pas de depot).</summary>
    Block,
}

/// <summary>
/// Carte d'indice educative affichee a cote du terminal. Modele pur (immuable) : pas de logique,
/// pas de note. L'<see cref="Id"/> est stable (utilise comme <c>data-hint-id</c> par l'UI et les E2E).
/// </summary>
public sealed record HintCard(string Id, string Title, string Message, HintSeverity Severity);
