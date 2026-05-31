using System.Collections.Generic;
using System.IO;
using System.Text;
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

    public CommandResult Run(string exerciseId)
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
            if (step.Type == "io")
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
                continue;
            }

            if (run.Error is not null)
            {
                output.AppendLine($"  ✗ exception : {run.Error.GetType().Name} — {run.Error.Message}");
                allMatch = false;
                continue;
            }

            var stdoutMatch = Normalize(run.Stdout) == Normalize(c.ExpectStdout);
            var exitMatch = run.ExitCode == c.ExpectExit;
            var ok = stdoutMatch && exitMatch;
            allMatch &= ok;

            output.AppendLine($"  expect_stdout : {Quote(run.Stdout)}   {(stdoutMatch ? "✓" : "✗ (diffère du manifest)")}");
            output.AppendLine($"  expect_exit   : {run.ExitCode}{(exitMatch ? "   ✓" : $"   ✗ (manifest = {c.ExpectExit})")}");
        }

        output.AppendLine(allMatch
            ? "→ Tous les cas correspondent au manifest. ✓"
            : "→ Des cas diffèrent : copie les lignes expect_* ci-dessus dans le manifest.");

        return new CommandResult(allMatch ? 0 : 1, output.ToString().TrimEnd());
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\r\n", "\n").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

    private sealed record IoCaseRef(IReadOnlyList<string> Args, string Stdin, string ExpectStdout, int ExpectExit);
}
