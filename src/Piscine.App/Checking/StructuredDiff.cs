using System.Collections.Generic;

namespace Piscine.App.Checking;

/// <summary>Nature d'une ligne d'un <see cref="StructuredDiff"/> (rendu coloré côté UI).</summary>
public enum DiffLineKind
{
    /// <summary>Ligne identique entre attendu et obtenu (contexte, neutre).</summary>
    Unchanged,

    /// <summary>Ligne présente dans l'attendu mais absente/différente dans l'obtenu (manquante).</summary>
    Expected,

    /// <summary>Ligne présente dans l'obtenu mais absente/différente dans l'attendu (en trop).</summary>
    Actual,
}

/// <summary>Une ligne du diff structuré : son texte (déjà déséchappé) et sa nature.</summary>
public sealed record DiffLine(DiffLineKind Kind, string Text);

/// <summary>
/// Diff attendu/obtenu <b>structuré</b>, dérivé dans la couche App à partir des messages verbatim
/// du grader (« Attendu : "…" » / « Obtenu  : "…" »), <b>sans</b> toucher au moteur. Le texte est
/// déséchappé (les <c>\n</c> redeviennent des sauts de ligne, découpés en lignes) pour un rendu
/// coloré ligne à ligne. Reste <c>null</c> côté <see cref="CheckCaseResult"/> si le cas n'expose pas
/// d'attendu/obtenu (compilation, exception, code de sortie, git, mutation…).
/// </summary>
public sealed record StructuredDiff(IReadOnlyList<DiffLine> Lines);
