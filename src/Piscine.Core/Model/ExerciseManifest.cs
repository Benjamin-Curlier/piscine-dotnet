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

    /// <summary>Temps de travail actif conseillé, hors lecture du cours.</summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>Points d'expérience accordés à la première réussite.</summary>
    public int Xp { get; set; }

    /// <summary>Compétences utilisées pour les badges et la recherche pédagogique.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Rôle de l'exercice dans le fil rouge, affiché sans modifier l'énoncé technique.</summary>
    public string StoryBeat { get; set; } = string.Empty;

    /// <summary>
    /// Module après lequel une mission de synthèse est conseillée. Optionnel pour les exercices
    /// ordinaires ; les Rushes l'utilisent pour construire la frise et la recommandation UI sans
    /// verrouiller le parcours.
    /// </summary>
    public string RecommendedAfter { get; set; } = string.Empty;

    /// <summary>
    /// Indique qu'une revue humaine de preuves d'exploitation complète les contrôles automatiques
    /// (runbook, reprise après panne, compatibilité…).
    /// </summary>
    public bool ManualValidation { get; set; }

    /// <summary>
    /// Exercice bonus : son échec ne bloque pas la correction séquentielle du groupe
    /// (les exercices suivants restent corrigés).
    /// </summary>
    public bool Bonus { get; set; }
}
