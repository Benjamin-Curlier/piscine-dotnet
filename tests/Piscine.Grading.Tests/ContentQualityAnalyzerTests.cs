using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public sealed class ContentQualityAnalyzerTests
{
    [Fact]
    public void Analyze_ExactImplementationLineInSubject_IsAdvisory()
    {
        using var dir = new TempDir();
        var module = Path.Combine("content", "modules", "15-regex");
        var exercise = Path.Combine(module, "exercises", "ex00");
        dir.WriteFile(Path.Combine(module, "module.yaml"), """
            id: 15-regex
            title: Regex
            order: 15
            groups:
              - id: regex
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine(module, "cours.md"), "cours");
        dir.WriteFile(Path.Combine(exercise, "manifest.yaml"), """
            id: ex00
            title: Test
            objective: Filtrer une collection de valeurs avec un prédicat fourni.
            deliverables: [Program.cs]
            grading: []
            """);
        const string leaked = "return valeurs.Where(valeur => valeur > 10).ToArray();";
        dir.WriteFile(Path.Combine(exercise, "subject.md"), $"Indice : `{leaked}`");
        dir.WriteFile(Path.Combine(exercise, "solution", "Program.cs"), leaked);

        var issues = ContentQualityAnalyzer.Analyze(
            new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state")));

        Assert.Contains(issues, issue => issue.Scope == "ex00" && issue.Rule == "solution-leak");
    }

    [Fact]
    public void Analyze_LeakInEarlyStarterComment_IsAdvisory()
    {
        using var dir = new TempDir();
        var module = Path.Combine("content", "modules", "01-bases");
        var exercise = Path.Combine(module, "exercises", "ex00");
        dir.WriteFile(Path.Combine(module, "module.yaml"), """
            id: 01-bases
            title: Bases
            order: 1
            groups:
              - id: bases
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine(module, "cours.md"), "cours");
        dir.WriteFile(Path.Combine(exercise, "manifest.yaml"), """
            id: ex00
            title: Test
            objective: Transformer plusieurs valeurs sans dévoiler le calcul final.
            deliverables: [Program.cs]
            starter: [Program.cs]
            grading: []
            """);
        const string leaked = "return valeurs.Where(valeur => valeur > 10).ToArray();";
        dir.WriteFile(Path.Combine(exercise, "subject.md"), "Filtre les valeurs.");
        dir.WriteFile(Path.Combine(exercise, "starter", "Program.cs"), $"// Solution possible : {leaked}");
        dir.WriteFile(Path.Combine(exercise, "solution", "Program.cs"), leaked);

        var issues = ContentQualityAnalyzer.Analyze(
            new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state")));

        Assert.Contains(issues, issue => issue.Scope == "ex00" && issue.Rule == "starter-leak");
    }

    [Fact]
    public void Analyze_EmptyModuleAndDifficultSingleHint_AreAdvisory()
    {
        using var dir = new TempDir();
        var empty = Path.Combine("content", "modules", "01-empty");
        dir.WriteFile(Path.Combine(empty, "module.yaml"), """
            id: 01-empty
            title: Empty
            order: 1
            groups:
              - id: none
                exercises: []
            """);

        var module = Path.Combine("content", "modules", "02-hard");
        var exercise = Path.Combine(module, "exercises", "ex00");
        dir.WriteFile(Path.Combine(module, "module.yaml"), """
            id: 02-hard
            title: Hard
            order: 2
            groups:
              - id: hard
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine(exercise, "manifest.yaml"), """
            id: ex00
            title: Hard
            objective: Résoudre un problème avancé avec plusieurs contraintes importantes.
            difficulty: difficile
            estimated_minutes: 90
            deliverables: [Program.cs]
            grading: []
            feedback:
              hints:
                - { when: io_mismatch, message: "Commence." }
            """);
        dir.WriteFile(Path.Combine(exercise, "solution", "Program.cs"), """
            var values = Enumerable.Range(1, 20).ToArray();
            var filtered = values.Where(value => value > 4).ToArray();
            var sorted = filtered.OrderBy(value => value).ToArray();
            foreach (var value in sorted)
            {
                Console.WriteLine(value);
            }
            """);

        var issues = ContentQualityAnalyzer.Analyze(
            new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state")));

        Assert.Contains(issues, issue => issue.Scope == "01-empty" && issue.Rule == "empty-module");
        Assert.Contains(issues, issue => issue.Scope == "ex00" && issue.Rule == "hint-depth");
    }
}
