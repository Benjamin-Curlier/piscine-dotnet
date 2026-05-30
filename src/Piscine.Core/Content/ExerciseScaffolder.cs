using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Piscine.Core.Content;

/// <summary>
/// Génère le squelette d'un nouvel exercice <c>io</c> (manifest, sujet, starter, solution)
/// dans un module existant, pour accélérer la création de contenu.
/// </summary>
public static class ExerciseScaffolder
{
    private static readonly Regex NumericPrefix = new(@"^ex\d+-", RegexOptions.IgnoreCase);

    /// <summary>
    /// Déduit le nom de fichier livrable PascalCase à partir de l'id de l'exercice :
    /// <c>ex02-somme-n</c> → <c>SommeN.cs</c>. Le préfixe <c>exNN-</c> est retiré.
    /// </summary>
    public static string DeliverableFileName(string exerciseId)
    {
        var stripped = NumericPrefix.Replace(exerciseId, string.Empty);
        if (stripped.Length == 0)
        {
            stripped = exerciseId;
        }

        var segments = stripped.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var pascal = string.Concat(segments.Select(Capitalize));
        return $"{pascal}.cs";
    }

    /// <summary>
    /// Crée <c>&lt;modulesRoot&gt;/&lt;moduleId&gt;/exercises/&lt;exerciseId&gt;/</c> avec un squelette d'exercice io.
    /// Lève si le module est absent ou si l'exercice existe déjà.
    /// </summary>
    /// <returns>Le chemin du dossier d'exercice créé.</returns>
    public static string Create(string modulesRoot, string moduleId, string exerciseId)
    {
        var moduleDir = Path.Combine(modulesRoot, moduleId);
        if (!Directory.Exists(moduleDir))
        {
            throw new DirectoryNotFoundException(
                $"Module introuvable : {moduleId}. Crée d'abord son dossier sous {modulesRoot}.");
        }

        var exerciseDir = Path.Combine(moduleDir, "exercises", exerciseId);
        if (Directory.Exists(exerciseDir))
        {
            throw new IOException($"L'exercice existe déjà : {exerciseId}");
        }

        var deliverable = DeliverableFileName(exerciseId);
        var title = TitleFromId(exerciseId);

        Directory.CreateDirectory(Path.Combine(exerciseDir, "starter"));
        Directory.CreateDirectory(Path.Combine(exerciseDir, "solution"));

        File.WriteAllText(Path.Combine(exerciseDir, "manifest.yaml"), ManifestTemplate(exerciseId, title, deliverable));
        File.WriteAllText(Path.Combine(exerciseDir, "subject.md"), SubjectTemplate(exerciseId, title, deliverable));
        File.WriteAllText(Path.Combine(exerciseDir, "starter", deliverable), StarterTemplate());
        File.WriteAllText(Path.Combine(exerciseDir, "solution", deliverable), SolutionTemplate());

        return exerciseDir;
    }

    private static string Capitalize(string segment) =>
        segment.Length == 0 ? segment : char.ToUpperInvariant(segment[0]) + segment[1..];

    private static string TitleFromId(string exerciseId)
    {
        var stripped = NumericPrefix.Replace(exerciseId, string.Empty);
        if (stripped.Length == 0)
        {
            stripped = exerciseId;
        }

        var words = stripped.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(Capitalize));
    }

    private static string ManifestTemplate(string exerciseId, string title, string deliverable)
    {
        var sb = new StringBuilder();
        sb.Append("id: ").Append(exerciseId).Append('\n');
        sb.Append("title: \"").Append(title).Append("\"\n");
        sb.Append("objective: \"TODO : décris en une phrase ce que l'exercice attend.\"\n");
        sb.Append("deliverables: [").Append(deliverable).Append("]\n");
        sb.Append("starter: [").Append(deliverable).Append("]\n");
        sb.Append("grading:\n");
        sb.Append("  - type: io\n");
        sb.Append("    cases:\n");
        sb.Append("      - stdin: \"TODO\\n\"\n");
        sb.Append("        expect_stdout: \"TODO\\n\"\n");
        sb.Append("        expect_exit: 0\n");
        sb.Append("feedback:\n");
        sb.Append("  hints:\n");
        sb.Append("    - when: io_mismatch\n");
        sb.Append("      message: \"TODO : indice pédagogique.\"\n");
        sb.Append("  course_ref: \"cours.md\"\n");
        sb.Append("solution: [").Append(deliverable).Append("]\n");
        return sb.ToString();
    }

    private static string SubjectTemplate(string exerciseId, string title, string deliverable)
    {
        var sb = new StringBuilder();
        sb.Append("# ").Append(exerciseId).Append(" — ").Append(title).Append("\n\n");
        sb.Append("## Objectif\n\n");
        sb.Append("TODO : décris ce que la recrue doit lire sur l'entrée et afficher sur la sortie.\n\n");
        sb.Append("## Livrable\n\n");
        sb.Append("- `").Append(deliverable).Append("`\n\n");
        sb.Append("## Indices\n\n");
        sb.Append("- TODO\n");
        return sb.ToString();
    }

    private static string StarterTemplate() =>
        "// TODO : écris ta solution ici.\n";

    private static string SolutionTemplate() =>
        "// TODO : solution de référence (sert de filet à validate-content).\n" +
        "System.Console.WriteLine(\"TODO\");\n";
}
