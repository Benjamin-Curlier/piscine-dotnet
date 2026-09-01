using System;
using System.Collections.Generic;
using Piscine.App.Progress;

namespace Piscine.App.Report;

/// <summary>
/// Ligne « par module » du rapport : compteurs dérivés des statuts d'exercices d'un module
/// (faits / en cours / à revoir / restants) + complétion des bonus. Pur, sans UI.
/// </summary>
public sealed record ReportModuleRow(
    string Number,
    string Title,
    int Fait,
    int EnCours,
    int ARevoir,
    int Restant,
    int BonusFaits,
    int BonusTotal,
    int Total)
{
    /// <summary>Pourcentage d'exercices « faits » (poussés/notés) sur le total du module.</summary>
    public int PercentFait => Total == 0 ? 0 : (int)Math.Round(100.0 * Fait / Total);
}

/// <summary>Verdict d'un exercice dans l'historique des push récents du rapport.</summary>
public sealed record ReportPushEntry(string ExerciseId, string Verdict, int Attempts);

/// <summary>État d'une mission de synthèse dans le rapport, séparé des exercices de modules.</summary>
public sealed record ReportRushRow(string Id, string Title, ExerciseProgressStatus Status);

/// <summary>
/// Modèle <b>pur</b> et autonome de la page de rapport : identité git, date de génération,
/// avancement global, lignes par module et historique de push. Aucune dépendance UI →
/// consommé tel quel par <c>Report.razor</c> ET par <see cref="ReportMarkdown"/> (testable xUnit).
/// </summary>
public sealed record ReportModel(
    string? UserName,
    string? UserEmail,
    string? Branch,
    DateTimeOffset GeneratedAt,
    int PercentFait,
    int Fait,
    int EnCours,
    int ARevoir,
    int Restant,
    int Total,
    IReadOnlyList<ReportModuleRow> Modules,
    IReadOnlyList<ReportPushEntry> RecentPushes,
    IReadOnlyList<ReportRushRow> Rushes);
