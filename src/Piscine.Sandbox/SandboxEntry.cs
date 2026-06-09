using System;
using System.IO;
using System.Text.Json;

namespace Piscine.Sandbox;

/// <summary>Point d'entrée logique : lit le dossier de travail, exécute, écrit result.json.</summary>
public static class SandboxEntry
{
    public static int Run(string workDir)
    {
        var resultPath = Path.Combine(workDir, "result.json");
        var request = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(workDir, "request.json")),
            SandboxJsonContext.Default.SandboxRequest)!;
        var bytes = File.ReadAllBytes(Path.Combine(workDir, "asm.dll"));

        var result = new SandboxResult();
        var written = false;
        void Flush()
        {
            if (written)
            {
                return;
            }

            written = true;
            File.WriteAllText(resultPath, JsonSerializer.Serialize(result, SandboxJsonContext.Default.SandboxResult));
        }

        // Si la recrue appelle Environment.Exit(n), l'exécution ne revient pas : on tente d'écrire
        // un résultat partiel marqué ExitedEarly afin que le parent ne le prenne pas pour un crash.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            result.ExitedEarly = true;
            Flush();
        };

        try
        {
            result = SandboxExecutor.Execute(request, bytes);
        }
        catch (Exception ex)
        {
            result = new SandboxResult { ErrorType = ex.GetType().Name, ErrorMessage = ex.Message };
        }

        Flush();
        return 0;
    }
}
