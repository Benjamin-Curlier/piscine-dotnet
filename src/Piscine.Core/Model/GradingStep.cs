using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Une étape de notation déclarée dans le manifest (type io / unit / norme / mutation).</summary>
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

    /// <summary>Pour le grader <c>git</c> : état attendu du dépôt rendu par la recrue.</summary>
    public GitAssertions? Git { get; set; }

    /// <summary>Pour le grader <c>projet</c> : assertions d'architecture sur la solution multi-fichiers.</summary>
    public ProjectAssertions? Project { get; set; }
}

/// <summary>Assertions d'architecture d'une solution multi-fichiers (grader <c>projet</c>).</summary>
public sealed class ProjectAssertions
{
    /// <summary>
    /// Types qui doivent exister, au **nom de metadata** : <c>Namespace.Type</c> (ex. <c>Domain.Compte</c>),
    /// génériques suffixés par l'arité (<c>Domain.Repository`1</c>), types imbriqués avec <c>+</c>
    /// (<c>Domain.Outer+Inner</c>).
    /// </summary>
    public List<string> RequiresTypes { get; set; } = new();

    /// <summary>Dépendances de couches interdites (un namespace ne doit pas en référencer un autre).</summary>
    public List<LayerRule> ForbiddenDependencies { get; set; } = new();
}

/// <summary>Règle de couche : aucun type de <see cref="From"/> ne doit référencer un type de <see cref="To"/> (grader <c>projet</c>).</summary>
public sealed class LayerRule
{
    /// <summary>Namespace (ou préfixe) de la couche source.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Namespace (ou préfixe) de la couche cible interdite.</summary>
    public string To { get; set; } = string.Empty;
}

/// <summary>État attendu d'un dépôt git rendu (grader <c>git</c>).</summary>
public sealed class GitAssertions
{
    /// <summary>Branches qui doivent exister dans le dépôt.</summary>
    public List<string> Branches { get; set; } = new();

    /// <summary>Nombre minimum de commits atteignables depuis <c>HEAD</c> (0 = non vérifié).</summary>
    public int MinCommits { get; set; }

    /// <summary>Si vrai, aucun marqueur de conflit (<![CDATA[<<<<<<<]]> / ======= / <![CDATA[>>>>>>>]]>) ne doit subsister dans l'arbre de <c>HEAD</c>.</summary>
    public bool NoConflictMarkers { get; set; }

    /// <summary>Assertions de contenu de fichiers.</summary>
    public List<GitFileAssertion> Files { get; set; } = new();

    /// <summary>Fusions attendues : la pointe de <c>branch</c> doit être un ancêtre de <c>into</c>.</summary>
    public List<GitMerge> Merged { get; set; } = new();
}

/// <summary>
/// Assertion de contenu d'un fichier dans une ref donnée (grader <c>git</c>).
/// Si ni <see cref="Contains"/> ni <see cref="Content"/> ne sont renseignés, l'assertion vérifie
/// uniquement que le fichier existe.
/// </summary>
public sealed class GitFileAssertion
{
    /// <summary>Chemin du fichier relatif à la racine du dépôt.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Branche ou ref où lire le fichier (défaut <c>HEAD</c>).</summary>
    public string Ref { get; set; } = "HEAD";

    /// <summary>Sous-chaîne qui doit être présente dans le fichier (optionnel).</summary>
    public string Contains { get; set; } = string.Empty;

    /// <summary>Contenu exact attendu du fichier (optionnel ; le grader normalise les fins de ligne).</summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>Une fusion attendue : la pointe de <see cref="Branch"/> est un ancêtre de <see cref="Into"/> (grader <c>git</c>).</summary>
public sealed class GitMerge
{
    /// <summary>Branche cible (qui a reçu la fusion).</summary>
    public string Into { get; set; } = string.Empty;

    /// <summary>Branche dont la pointe doit être atteignable depuis <see cref="Into"/>.</summary>
    public string Branch { get; set; } = string.Empty;
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
