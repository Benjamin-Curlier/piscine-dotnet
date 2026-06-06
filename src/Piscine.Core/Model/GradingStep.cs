using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Une étape de notation déclarée dans le manifest (type io / unit / norme).</summary>
public sealed class GradingStep
{
    public string Type { get; set; } = string.Empty;

    /// <summary>Cas d'exécution pour le grader <c>io</c>.</summary>
    public List<IoCase> Cases { get; set; } = new();

    /// <summary>Fichiers de tests cachés pour le grader <c>unit</c> (consommé à l'It.3).</summary>
    public List<string> TestFiles { get; set; } = new();

    /// <summary>Pour le grader <c>norme</c> : si vrai, un écart de norme fait échouer l'exercice.</summary>
    public bool Blocking { get; set; }

    /// <summary>Pour le grader <c>mutation</c> : impl de référence cachée (chemin relatif au dossier content de l'exo).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Pour le grader <c>mutation</c> : mutations à dériver de la référence par find/replace.</summary>
    public List<Mutant> Mutants { get; set; } = new();
}

/// <summary>Une mutation : un remplacement textuel nommé appliqué à l'impl de référence (grader <c>mutation</c>).</summary>
public sealed class Mutant
{
    /// <summary>Identifiant court de la mutation (diagnostics auteur).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Phrase pédagogique montrée à la recrue si le mutant survit (le cas manquant).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Chaîne à trouver dans la référence (doit matcher exactement une fois).</summary>
    public string Find { get; set; } = string.Empty;

    /// <summary>Chaîne de remplacement (introduit le bug).</summary>
    public string Replace { get; set; } = string.Empty;
}

/// <summary>Un cas d'exécution pour le grader <c>io</c>.</summary>
public sealed class IoCase
{
    public List<string> Args { get; set; } = new();

    public string Stdin { get; set; } = string.Empty;

    public string ExpectStdout { get; set; } = string.Empty;

    public int ExpectExit { get; set; }
}
