using System;
using System.Globalization;
using System.Text;

namespace Piscine.App.Report;

/// <summary>
/// Générateur <b>pur</b> : transforme un <see cref="ReportModel"/> en un rapport Markdown
/// git-friendly (déterministe, sans dépendance UI ni horloge interne). Testable unitairement
/// (modèle → chaîne attendue). Sert de cible au bouton « Copier / Enregistrer en Markdown ».
/// </summary>
public static class ReportMarkdown
{
    /// <summary>Rend le rapport en Markdown. Le retour à la ligne est toujours <c>\n</c> (git-friendly).</summary>
    public static string Render(ReportModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sb = new StringBuilder();

        sb.Append("# Rapport de progression — Piscine .NET\n\n");

        // Identité git + date de génération.
        var identity = FormatIdentity(model.UserName, model.UserEmail);
        sb.Append("- **Recrue :** ").Append(identity).Append('\n');
        if (!string.IsNullOrWhiteSpace(model.Branch))
        {
            sb.Append("- **Branche :** ").Append(model.Branch).Append('\n');
        }
        sb.Append("- **Généré le :** ")
          .Append(model.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
          .Append('\n');

        // Avancement global.
        sb.Append("- **Avancement :** ")
          .Append(model.PercentFait.ToString(CultureInfo.InvariantCulture))
          .Append(" % (")
          .Append(model.Fait.ToString(CultureInfo.InvariantCulture))
          .Append('/')
          .Append(model.Total.ToString(CultureInfo.InvariantCulture))
          .Append(" faits)\n\n");

        // Compteurs globaux.
        sb.Append("## Vue d'ensemble\n\n");
        sb.Append("| Fait | En cours | À revoir | Restant | Total |\n");
        sb.Append("|---:|---:|---:|---:|---:|\n");
        sb.Append("| ").Append(model.Fait).Append(" | ")
          .Append(model.EnCours).Append(" | ")
          .Append(model.ARevoir).Append(" | ")
          .Append(model.Restant).Append(" | ")
          .Append(model.Total).Append(" |\n\n");

        // Détail par module.
        sb.Append("## Par module\n\n");
        if (model.Modules.Count == 0)
        {
            sb.Append("_Aucun module avec exercices._\n");
        }
        else
        {
            sb.Append("| Module | Fait | En cours | À revoir | Restant | Bonus | Avancement |\n");
            sb.Append("|:---|---:|---:|---:|---:|:--:|---:|\n");
            foreach (var m in model.Modules)
            {
                sb.Append("| ").Append(Escape($"{m.Number} · {m.Title}")).Append(" | ")
                  .Append(m.Fait).Append(" | ")
                  .Append(m.EnCours).Append(" | ")
                  .Append(m.ARevoir).Append(" | ")
                  .Append(m.Restant).Append(" | ")
                  .Append(m.BonusFaits).Append('/').Append(m.BonusTotal).Append(" | ")
                  .Append(m.PercentFait).Append(" % |\n");
            }
        }

        // Historique des push récents.
        sb.Append("\n## Push récents\n\n");
        if (model.RecentPushes.Count == 0)
        {
            sb.Append("_Aucun push récent._\n");
        }
        else
        {
            sb.Append("| Exercice | Verdict | Tentatives |\n");
            sb.Append("|:---|:---|---:|\n");
            foreach (var p in model.RecentPushes)
            {
                sb.Append("| ").Append(Escape(p.ExerciseId)).Append(" | ")
                  .Append(Escape(p.Verdict)).Append(" | ")
                  .Append(p.Attempts).Append(" |\n");
            }
        }

        return sb.ToString();
    }

    private static string FormatIdentity(string? name, string? email)
    {
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasEmail = !string.IsNullOrWhiteSpace(email);

        if (hasName && hasEmail)
        {
            return $"{name} <{email}>";
        }
        if (hasName)
        {
            return name!;
        }
        if (hasEmail)
        {
            return email!;
        }
        return "Identité git non configurée";
    }

    /// <summary>Échappe les barres verticales pour ne pas casser les cellules de tableau Markdown.</summary>
    private static string Escape(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);
}
