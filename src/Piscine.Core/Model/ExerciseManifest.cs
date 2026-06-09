using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>
/// Un exercice désérialisé depuis <c>manifest.yaml</c>.
/// La section <c>constraints</c> sera ajoutée quand elle sera appliquée.
/// </summary>
public sealed class ExerciseManifest
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public List<string> Deliverables { get; set; } = new();

    public List<string> Starter { get; set; } = new();

    /// <summary>
    /// Fichiers du corrigé (informatif : reflète le contenu de <c>solution/</c>). Non consommé au
    /// runtime — le corrigé est lu depuis le dossier <c>solution/</c> — mais déclaré dans le contenu,
    /// donc reconnu ici pour que la passe stricte de <c>validate-content</c> ne le signale pas.
    /// </summary>
    public List<string> Solution { get; set; } = new();

    public List<GradingStep> Grading { get; set; } = new();

    public FeedbackConfig Feedback { get; set; } = new();

    /// <summary>Niveau de difficulté indicatif : <c>facile</c>, <c>moyen</c> (défaut) ou <c>difficile</c>.</summary>
    public string Difficulty { get; set; } = "moyen";

    /// <summary>
    /// Exercice bonus : son échec ne bloque pas la correction séquentielle du groupe
    /// (les exercices suivants restent corrigés).
    /// </summary>
    public bool Bonus { get; set; }
}
