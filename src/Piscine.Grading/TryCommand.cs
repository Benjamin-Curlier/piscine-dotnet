using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Piscine.Core;
using Piscine.Core.Content;

namespace Piscine.Grading;

/// <summary>
/// Outillage auteur : compile le corrigé de référence d'un exercice et l'exécute sur le
/// <c>stdin</c> de chaque cas <c>io</c> du manifest, en imprimant le <b>stdout réel</b> au
/// format YAML prêt à coller. Évite à l'auteur de *deviner* <c>expect_stdout</c> — il le génère.
/// Boucle interne ciblée (un exo) au lieu du gate global <c>validate-content</c>.
/// </summary>
public sealed class TryCommand
{
    private readonly PiscineLayout _layout;

    public TryCommand(PiscineLayout layout) => _layout = layout;

    public CommandResult Run(string exerciseId) => Run(exerciseId, write: false);

    public CommandResult Run(string exerciseId, bool write)
    {
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        if (location is null)
        {
            return new CommandResult(2, $"Exercice introuvable : {exerciseId}");
        }

        var solutionDir = Path.Combine(location.ContentDir, ContentValidator.SolutionDirName);
        if (!Directory.Exists(solutionDir))
        {
            return new CommandResult(2, $"[{exerciseId}] dossier solution/ manquant (corrigé de référence requis).");
        }

        var submission = SubmissionLoader.Load(location.ContentDir, solutionDir);
        if (submission.IsEmpty)
        {
            return new CommandResult(2, $"[{exerciseId}] aucun livrable trouvé dans solution/.");
        }

        var compilation = CompilationService.Compile(submission.Context.Sources, OutputKind.ConsoleApplication);
        if (!compilation.Success)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{exerciseId}] le corrigé ne compile pas :");
            foreach (var error in compilation.Errors)
            {
                sb.AppendLine($"  {error}");
            }

            return new CommandResult(1, sb.ToString().TrimEnd());
        }

        var ioCases = new List<IoCaseRef>();
        foreach (var step in submission.Manifest.Grading)
        {
            // `io` et `projet` exécutent tous deux des cas stdin→stdout : l'outil auteur génère leurs expects.
            if (step.Type == "io" || step.Type == "projet")
            {
                foreach (var ioCase in step.Cases)
                {
                    ioCases.Add(new IoCaseRef(ioCase.Args, ioCase.Stdin, ioCase.ExpectStdout, ioCase.ExpectExit));
                }
            }
        }

        if (ioCases.Count == 0)
        {
            return new CommandResult(0, $"[{exerciseId}] aucun cas io dans le manifest (module de lecture ?).");
        }

        var output = new StringBuilder();
        var allMatch = true;
        var allRunnable = true;
        var computed = new List<(string Stdout, int Exit)>();
        for (var i = 0; i < ioCases.Count; i++)
        {
            var c = ioCases[i];
            var run = ProgramRunner.Run(compilation.AssemblyBytes, c.Args.ToArray(), c.Stdin);

            output.AppendLine($"[{exerciseId}] cas {i + 1}/{ioCases.Count}");
            output.AppendLine($"  stdin         : {Quote(c.Stdin)}");

            if (run.TimedOut)
            {
                output.AppendLine("  ✗ timeout (boucle infinie ?)");
                allMatch = false;
                allRunnable = false;
                continue;
            }

            if (run.Error is not null)
            {
                output.AppendLine($"  ✗ exception : {run.Error.GetType().Name} — {run.Error.Message}");
                allMatch = false;
                allRunnable = false;
                continue;
            }

            computed.Add((Normalize(run.Stdout), run.ExitCode));

            var stdoutMatch = Normalize(run.Stdout) == Normalize(c.ExpectStdout);
            var exitMatch = run.ExitCode == c.ExpectExit;
            var ok = stdoutMatch && exitMatch;
            allMatch &= ok;

            output.AppendLine($"  expect_stdout : {Quote(run.Stdout)}   {(stdoutMatch ? "✓" : "✗ (diffère du manifest)")}");
            output.AppendLine($"  expect_exit   : {run.ExitCode}{(exitMatch ? "   ✓" : $"   ✗ (manifest = {c.ExpectExit})")}");
        }

        if (write)
        {
            if (!allRunnable)
            {
                output.AppendLine("→ Écriture annulée : au moins un cas n'est pas exécutable (timeout/exception).");
                return new CommandResult(1, output.ToString().TrimEnd());
            }

            var manifestPath = Path.Combine(location.ContentDir, ExerciseManifestLoader.FileName);
            var writeResult = WriteExpectations(manifestPath, computed);
            output.AppendLine(writeResult);
            return new CommandResult(0, output.ToString().TrimEnd());
        }

        output.AppendLine(allMatch
            ? "→ Tous les cas correspondent au manifest. ✓"
            : "→ Des cas diffèrent : relance avec --write pour réécrire le manifest, ou copie les lignes expect_* ci-dessus.");

        return new CommandResult(allMatch ? 0 : 1, output.ToString().TrimEnd());
    }

    /// <summary>
    /// Réécrit en place les valeurs <c>expect_stdout</c>/<c>expect_exit</c> (une par cas io, dans l'ordre),
    /// en préservant le reste du fichier (commentaires, mise en forme). Le <c>stdin</c>/<c>args</c> fournis
    /// par l'auteur ne sont jamais modifiés. Suppose le style maison : une valeur par ligne (HANDOFF règle 3).
    /// </summary>
    private static string WriteExpectations(string manifestPath, List<(string Stdout, int Exit)> computed)
    {
        var text = File.ReadAllText(manifestPath);
        var newline = text.Contains("\r\n") ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n").Split('\n');

        var stdoutLineCount = 0;
        var exitLineCount = 0;
        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^\s*expect_stdout:\s")) stdoutLineCount++;
            else if (Regex.IsMatch(line, @"^\s*expect_exit:\s")) exitLineCount++;
        }

        if (stdoutLineCount != computed.Count)
        {
            return $"→ Écriture annulée : {stdoutLineCount} ligne(s) expect_stdout dans le manifest ≠ {computed.Count} cas exécutés (style multi-lignes non supporté).";
        }

        var stdoutIdx = 0;
        var exitIdx = 0;
        var rewriteExit = exitLineCount == computed.Count;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var stdoutMatch = Regex.Match(line, @"^(\s*expect_stdout:\s*).*$");
            if (stdoutMatch.Success)
            {
                lines[i] = stdoutMatch.Groups[1].Value + Quote(computed[stdoutIdx++].Stdout);
                continue;
            }

            if (rewriteExit)
            {
                var exitMatch = Regex.Match(line, @"^(\s*expect_exit:\s*).*$");
                if (exitMatch.Success)
                {
                    lines[i] = exitMatch.Groups[1].Value + computed[exitIdx++].Exit.ToString();
                }
            }
        }

        File.WriteAllText(manifestPath, string.Join(newline, lines));
        var exitNote = rewriteExit ? "" : " (expect_exit laissés tels quels : nombre ≠ cas)";
        return $"→ Manifest mis à jour : {computed.Count} cas écrits{exitNote}.";
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\r\n", "\n").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

    private sealed record IoCaseRef(IReadOnlyList<string> Args, string Stdin, string ExpectStdout, int ExpectExit);
}
