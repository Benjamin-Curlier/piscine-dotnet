using System;
using Piscine.App.Report;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>
/// Le générateur Markdown est pur (modèle → chaîne). On vérifie l'en-tête (identité/branche/date/
/// avancement), les tableaux par module et l'historique de push, plus les replis (identité absente,
/// listes vides) et l'échappement des barres verticales.
/// </summary>
public sealed class ReportMarkdownTests
{
    private static readonly DateTimeOffset At =
        new(2026, 6, 14, 9, 30, 0, TimeSpan.Zero);

    private static ReportModel Sample() => new(
        UserName: "Ada Lovelace",
        UserEmail: "ada@piscine.dev",
        Branch: "main",
        GeneratedAt: At,
        PercentFait: 50,
        Fait: 2,
        EnCours: 1,
        ARevoir: 0,
        Restant: 1,
        Total: 4,
        Modules:
        [
            new ReportModuleRow("01", "Bases C#", 2, 1, 0, 1, 1, 2, 4),
        ],
        RecentPushes:
        [
            new ReportPushEntry("ex00-hello", "Réussi", 1),
        ]);

    [Fact]
    public void Renders_identity_branch_date_and_progress_header()
    {
        var md = ReportMarkdown.Render(Sample());

        Assert.Contains("# Rapport de progression — Piscine .NET", md);
        Assert.Contains("**Recrue :** Ada Lovelace <ada@piscine.dev>", md);
        Assert.Contains("**Branche :** main", md);
        Assert.Contains("**Généré le :** 2026-06-14 09:30", md);
        Assert.Contains("**Avancement :** 50 % (2/4 faits)", md);
    }

    [Fact]
    public void Renders_module_table_row_with_bonus_and_percent()
    {
        var md = ReportMarkdown.Render(Sample());

        Assert.Contains("## Par module", md);
        Assert.Contains("| 01 · Bases C# | 2 | 1 | 0 | 1 | 1/2 | 50 % |", md);
    }

    [Fact]
    public void Renders_recent_pushes()
    {
        var md = ReportMarkdown.Render(Sample());

        Assert.Contains("## Push récents", md);
        Assert.Contains("| ex00-hello | Réussi | 1 |", md);
    }

    [Fact]
    public void Missing_identity_falls_back_to_placeholder()
    {
        var model = Sample() with { UserName = null, UserEmail = null };

        var md = ReportMarkdown.Render(model);

        Assert.Contains("**Recrue :** Identité git non configurée", md);
    }

    [Fact]
    public void Empty_modules_and_pushes_render_placeholders()
    {
        var model = Sample() with { Modules = [], RecentPushes = [] };

        var md = ReportMarkdown.Render(model);

        Assert.Contains("_Aucun module avec exercices._", md);
        Assert.Contains("_Aucun push récent._", md);
    }

    [Fact]
    public void Pipe_in_text_is_escaped_to_not_break_table()
    {
        var model = Sample() with
        {
            Modules = [new ReportModuleRow("01", "A|B", 0, 0, 0, 1, 0, 0, 1)],
        };

        var md = ReportMarkdown.Render(model);

        Assert.Contains("01 · A\\|B", md);
    }

    [Fact]
    public void Output_uses_lf_only_newlines()
    {
        var md = ReportMarkdown.Render(Sample());

        Assert.DoesNotContain("\r", md);
    }
}
